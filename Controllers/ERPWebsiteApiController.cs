using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using CompuGear.Data;
using CompuGear.Models;
using CompuGear.Services;
using System.Text.Json;

namespace CompuGear.Controllers
{
    /// <summary>
    /// ERPWebsite API Controller - Handles subscription registration from the public website
    /// Creates Company, User (Company Admin), CompanySubscription, and CompanyModuleAccess records
    /// Integrates with PayMongo for payment processing
    /// </summary>
    [Route("api/erpwebsite")]
    [ApiController]
    public class ERPWebsiteApiController : ControllerBase
    {
        private readonly CompuGearDbContext _context;
        private readonly IPayMongoService _payMongoService;
        private readonly IMemoryCache _memoryCache;

        public ERPWebsiteApiController(CompuGearDbContext context, IPayMongoService payMongoService, IMemoryCache memoryCache)
        {
            _context = context;
            _payMongoService = payMongoService;
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// Process a new subscription from the public ERP website
        /// Creates: Company -> User (CompanyAdmin) -> CompanySubscription -> CompanyModuleAccess
        /// </summary>
        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromBody] ERPSubscribeRequest request)
        {
            try
            {
                // ===== VALIDATION =====
                if (string.IsNullOrWhiteSpace(request.CompanyName))
                    return Ok(new { success = false, message = "Company name is required." });

                if (string.IsNullOrWhiteSpace(request.CompanyEmail))
                    return Ok(new { success = false, message = "Company email is required." });

                if (string.IsNullOrWhiteSpace(request.CompanyPhone))
                    return Ok(new { success = false, message = "Company phone is required." });

                if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
                    return Ok(new { success = false, message = "First name and last name are required." });

                if (string.IsNullOrWhiteSpace(request.AdminEmail))
                    return Ok(new { success = false, message = "Admin email is required." });

                if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
                    return Ok(new { success = false, message = "Password must be at least 8 characters." });

                if (!request.ContractAgreed)
                    return Ok(new { success = false, message = "You must agree to the Subscription Service Contract before proceeding." });

                                // Normalize plan name - accept Starter, Pro, Enterprise (case-insensitive)
                var validPlans = new[] { "Starter", "Pro", "Enterprise" };
                var matchedPlan = validPlans.FirstOrDefault(p => string.Equals(p, request.PlanName?.Trim(), StringComparison.OrdinalIgnoreCase));
                request.PlanName = matchedPlan ?? "Starter";

                var selectedModuleCodes = ResolveSelectedModuleCodes(request);
                if (!selectedModuleCodes.Any())
                    return Ok(new { success = false, message = "Please select at least one module." });

                var erpModules = await _context.ERPModules
                    .Where(m => selectedModuleCodes.Contains(m.ModuleCode) && m.IsActive)
                    .ToListAsync();

                if (!erpModules.Any())
                    return Ok(new { success = false, message = "Selected modules are unavailable. Please contact support." });

                // Check if company email already exists
                var existingCompany = await _context.Companies
                    .FirstOrDefaultAsync(c => c.Email == request.CompanyEmail);
                if (existingCompany != null)
                    return Ok(new { success = false, message = "A company with this email already exists." });

                // Check if admin email already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.AdminEmail);
                if (existingUser != null)
                    return Ok(new { success = false, message = "An account with this email already exists." });

                // Use execution strategy to support retries with transactions
                var strategy = _context.Database.CreateExecutionStrategy();
                Company company = null!;
                User adminUser = null!;

                await strategy.ExecuteAsync(async () =>
                {
                    using var tx = await _context.Database.BeginTransactionAsync();

                    // ===== 1. CREATE COMPANY =====
                    var companyCode = $"CG-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
                    company = new Company
                    {
                        CompanyName = request.CompanyName,
                        CompanyCode = companyCode,
                        Email = request.CompanyEmail,
                        Phone = request.CompanyPhone,
                        Address = request.CompanyAddress,
                        City = request.CompanyCity,
                        Country = string.IsNullOrWhiteSpace(request.CompanyCountry) ? "Philippines" : request.CompanyCountry,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.Companies.Add(company);
                    await _context.SaveChangesAsync();

                    // ===== 2. CREATE ADMIN USER (RoleId = 2 = Company Admin) =====
                    var salt = Guid.NewGuid().ToString("N").Substring(0, 16);
                    var passwordHash = Convert.ToBase64String(
                        System.Security.Cryptography.SHA256.HashData(
                            System.Text.Encoding.UTF8.GetBytes(request.Password + salt)));

                    adminUser = new User
                    {
                        Username = request.AdminEmail.Split('@')[0] + DateTime.Now.Ticks.ToString().Substring(10),
                        Email = request.AdminEmail,
                        FirstName = request.FirstName,
                        LastName = request.LastName,
                        Phone = request.AdminPhone,
                        PasswordHash = passwordHash,
                        Salt = salt,
                        RoleId = 2, // Company Admin - gets redirected to Admin Portal
                        CompanyId = company.CompanyId,
                        IsActive = true,
                        IsEmailVerified = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.Users.Add(adminUser);
                    await _context.SaveChangesAsync();

                    // ===== 3. CREATE SUBSCRIPTION =====
                    var planConfig = GetPlanConfig(request.PlanName);
                    var contractTerm = request.PlanName == "Enterprise" ? 24 : request.PlanName == "Pro" ? 12 : 12;
                    var nextDue = DateTime.UtcNow.AddMonths(request.BillingCycle == "Annual" ? 12 : 1);
                    var subscription = new CompanySubscription
                    {
                        CompanyId = company.CompanyId,
                        PlanName = request.PlanName,
                        Status = "Active",
                        BillingCycle = request.BillingCycle ?? "Monthly",
                        StartDate = DateTime.UtcNow,
                        MonthlyFee = planConfig.MonthlyFee,
                        MaxUsers = planConfig.MaxUsers,
                        Notes = $"Subscribed via ERPWebsite on {DateTime.UtcNow:yyyy-MM-dd}",
                        ContractAgreed = true,
                        ContractAgreedAt = DateTime.UtcNow,
                        ContractType = request.PlanName == "Enterprise" ? "Enterprise" : request.PlanName == "Pro" ? "Annual" : "Standard",
                        ContractTermMonths = contractTerm,
                        LastPaymentDate = DateTime.UtcNow,
                        NextDueDate = nextDue,
                        PaymentStatus = "Current",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        CreatedBy = adminUser.UserId
                    };

                    _context.CompanySubscriptions.Add(subscription);
                    await _context.SaveChangesAsync();

                    // ===== 4. CREATE MODULE ACCESS =====
                    foreach (var module in erpModules)
                    {
                        var access = new CompanyModuleAccess
                        {
                            CompanyId = company.CompanyId,
                            ModuleId = module.ModuleId,
                            IsEnabled = true,
                            ActivatedAt = DateTime.UtcNow,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.CompanyModuleAccess.Add(access);
                    }

                    var roleAccessRows = BuildRoleAccessForSubscribedModules(company.CompanyId, selectedModuleCodes);
                    _context.RoleModuleAccess.AddRange(roleAccessRows);

                    await _context.SaveChangesAsync();

                    // ===== 5. LOG PLATFORM USAGE =====
                    var usageLog = new PlatformUsageLog
                    {
                        CompanyId = company.CompanyId,
                        UserId = adminUser.UserId,
                        ModuleCode = "SUBSCRIPTION",
                        Action = "NEW_SUBSCRIPTION",
                        Details = $"New {request.PlanName} subscription created via ERPWebsite. Plan: {request.PlanName}, Billing: {request.BillingCycle}",
                        IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.PlatformUsageLogs.Add(usageLog);
                    await _context.SaveChangesAsync();

                    // ===== 6. CREATE ACCOUNTING ENTRIES (Journal + GL) =====
                    await CreateSubscriptionAccountingEntries(subscription, company);

                    await tx.CommitAsync();
                });

                return Ok(new
                {
                    success = true,
                    message = "Subscription created successfully! You can now login with your admin email.",
                    planName = request.PlanName,
                    companyId = company.CompanyId,
                    companyName = company.CompanyName,
                    adminEmail = adminUser.Email
                });
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                return Ok(new { success = false, message = "An error occurred: " + msg });
            }
        }

        /// <summary>
        /// Create a PayMongo Checkout Session for subscription payment.
        /// Validates the form, stores pending registration in session, and returns a PayMongo checkout URL.
        /// </summary>
        [HttpPost("create-checkout")]
        public async Task<IActionResult> CreateCheckout([FromBody] ERPSubscribeRequest request)
        {
            try
            {
                // ===== VALIDATION =====
                if (string.IsNullOrWhiteSpace(request.CompanyName))
                    return Ok(new { success = false, message = "Company name is required." });

                if (string.IsNullOrWhiteSpace(request.CompanyEmail))
                    return Ok(new { success = false, message = "Company email is required." });

                if (string.IsNullOrWhiteSpace(request.CompanyPhone))
                    return Ok(new { success = false, message = "Company phone is required." });

                if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
                    return Ok(new { success = false, message = "First name and last name are required." });

                if (string.IsNullOrWhiteSpace(request.AdminEmail))
                    return Ok(new { success = false, message = "Admin email is required." });

                if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
                    return Ok(new { success = false, message = "Password must be at least 8 characters." });

                // Normalize plan name - accept Starter, Pro, Enterprise (case-insensitive)
                var validPlans2 = new[] { "Starter", "Pro", "Enterprise" };
                var matchedPlan2 = validPlans2.FirstOrDefault(p => string.Equals(p, request.PlanName?.Trim(), StringComparison.OrdinalIgnoreCase));
                request.PlanName = matchedPlan2 ?? "Starter";

                var selectedModuleCodes = ResolveSelectedModuleCodes(request);
                if (!selectedModuleCodes.Any())
                    return Ok(new { success = false, message = "Please select at least one module." });

                // Check if company email already exists
                var existingCompany = await _context.Companies
                    .FirstOrDefaultAsync(c => c.Email == request.CompanyEmail);
                if (existingCompany != null)
                    return Ok(new { success = false, message = "A company with this email already exists." });

                // Check if admin email already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.AdminEmail);
                if (existingUser != null)
                    return Ok(new { success = false, message = "An account with this email already exists." });

                // ===== CALCULATE AMOUNT =====
                var planConfig = GetPlanConfig(request.PlanName);
                var billingCycle = request.BillingCycle ?? "Monthly";
                decimal amount;
                string description;

                if (billingCycle == "Annual")
                {
                    amount = planConfig.MonthlyFee * 10; // Annual = 10 months (save 2)
                    description = $"CompuGear ERP {request.PlanName} Plan - Annual Subscription (â‚±{planConfig.MonthlyFee:N0}/mo Ã— 10 months)";
                }
                else
                {
                    amount = planConfig.MonthlyFee;
                    description = $"CompuGear ERP {request.PlanName} Plan - Monthly Subscription";
                }

                // ===== STORE PENDING REGISTRATION IN SESSION =====
                var registrationId = Guid.NewGuid().ToString("N");
                var pendingData = JsonSerializer.Serialize(request);
                HttpContext.Session.SetString($"PendingReg_{registrationId}", pendingData);
                _memoryCache.Set($"PendingReg_{registrationId}", pendingData, TimeSpan.FromHours(2));

                // ===== CREATE PAYMONGO CHECKOUT SESSION =====
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var checkoutRequest = new PayMongoCheckoutRequest
                {
                    PlanName = request.PlanName,
                    BillingCycle = billingCycle,
                    Amount = amount,
                    Description = description,
                    SuccessUrl = $"{baseUrl}/ERPWebsite/PaymentSuccess?reg_id={registrationId}",
                    CancelUrl = $"{baseUrl}/ERPWebsite/PaymentCancelled?plan={request.PlanName}",
                    RegistrationId = registrationId
                };

                var result = await _payMongoService.CreateCheckoutSessionAsync(checkoutRequest);

                if (!result.Success)
                {
                    return Ok(new { success = false, message = result.ErrorMessage ?? "Failed to create payment session." });
                }

                // Store checkout session ID in session for verification
                HttpContext.Session.SetString($"CheckoutSession_{registrationId}", result.CheckoutSessionId!);
                _memoryCache.Set($"CheckoutSession_{registrationId}", result.CheckoutSessionId!, TimeSpan.FromHours(2));

                return Ok(new
                {
                    success = true,
                    checkoutUrl = result.CheckoutUrl,
                    checkoutSessionId = result.CheckoutSessionId
                });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        /// <summary>
        /// Verify payment and complete registration after PayMongo checkout
        /// Called internally by the PaymentSuccess page controller action
        /// </summary>
        [HttpPost("verify-payment")]
        public async Task<IActionResult> VerifyPaymentAndRegister([FromBody] PaymentVerifyRequest verifyRequest)
        {
            try
            {
                var registrationId = verifyRequest.RegistrationId;
                var sessionId = verifyRequest.SessionId;

                if (string.IsNullOrWhiteSpace(registrationId))
                    return Ok(new { success = false, message = "Invalid payment verification request." });

                var storedSessionId = HttpContext.Session.GetString($"CheckoutSession_{registrationId}")
                    ?? _memoryCache.Get<string>($"CheckoutSession_{registrationId}");
                if (string.IsNullOrWhiteSpace(sessionId) || sessionId.Contains("{"))
                {
                    sessionId = storedSessionId;
                }

                if (string.IsNullOrWhiteSpace(sessionId))
                    return Ok(new { success = false, message = "Payment session not found. Please try again from the confirmation page." });

                // ===== VERIFY PAYMONGO PAYMENT =====
                var sessionStatus = await _payMongoService.GetCheckoutSessionAsync(sessionId);
                if (!sessionStatus.Success || !sessionStatus.IsPaid)
                {
                    return Ok(new { success = false, message = "Payment has not been completed. Please try again." });
                }

                // ===== RETRIEVE PENDING REGISTRATION DATA =====
                var pendingJson = HttpContext.Session.GetString($"PendingReg_{registrationId}")
                    ?? _memoryCache.Get<string>($"PendingReg_{registrationId}");
                if (string.IsNullOrWhiteSpace(pendingJson))
                {
                    return Ok(new { success = false, message = "Registration session expired. Please start over." });
                }

                var request = JsonSerializer.Deserialize<ERPSubscribeRequest>(pendingJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (request == null)
                    return Ok(new { success = false, message = "Invalid registration data." });

                var selectedModuleCodes = ResolveSelectedModuleCodes(request);
                if (!selectedModuleCodes.Any())
                    return Ok(new { success = false, message = "No subscribed modules found for this registration." });

                var erpModules = await _context.ERPModules
                    .Where(m => selectedModuleCodes.Contains(m.ModuleCode) && m.IsActive)
                    .ToListAsync();

                if (!erpModules.Any())
                    return Ok(new { success = false, message = "Selected modules are unavailable. Please contact support." });

                // ===== Double-check uniqueness =====
                var existingCompany = await _context.Companies
                    .FirstOrDefaultAsync(c => c.Email == request.CompanyEmail);

                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.AdminEmail);

                if (existingCompany != null && existingUser != null && existingUser.CompanyId == existingCompany.CompanyId)
                {
                    var existingSubscription = await _context.CompanySubscriptions
                        .Where(s => s.CompanyId == existingCompany.CompanyId)
                        .OrderByDescending(s => s.StartDate)
                        .FirstOrDefaultAsync();

                    if (existingSubscription != null)
                    {
                        HttpContext.Session.Remove($"PendingReg_{registrationId}");
                        HttpContext.Session.Remove($"CheckoutSession_{registrationId}");
                        _memoryCache.Remove($"PendingReg_{registrationId}");
                        _memoryCache.Remove($"CheckoutSession_{registrationId}");

                        return Ok(new
                        {
                            success = true,
                            message = "Payment verified and subscription already created.",
                            planName = existingSubscription.PlanName,
                            companyName = existingCompany.CompanyName,
                            adminEmail = existingUser.Email,
                            paymentId = sessionStatus.PaymentId
                        });
                    }
                }

                if (existingCompany != null)
                    return Ok(new { success = false, message = "A company with this email already exists." });

                if (existingUser != null)
                    return Ok(new { success = false, message = "An account with this email already exists." });

                // Use execution strategy to support retries with transactions
                var verifyStrategy = _context.Database.CreateExecutionStrategy();
                Company company = null!;
                User adminUser = null!;

                await verifyStrategy.ExecuteAsync(async () =>
                {
                    using var tx = await _context.Database.BeginTransactionAsync();

                    // ===== 1. CREATE COMPANY =====
                    var companyCode = $"CG-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
                    company = new Company
                    {
                        CompanyName = request.CompanyName,
                        CompanyCode = companyCode,
                        Email = request.CompanyEmail,
                        Phone = request.CompanyPhone,
                        Address = request.CompanyAddress,
                        City = request.CompanyCity,
                        Country = string.IsNullOrWhiteSpace(request.CompanyCountry) ? "Philippines" : request.CompanyCountry,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.Companies.Add(company);
                    await _context.SaveChangesAsync();

                    // ===== 2. CREATE ADMIN USER =====
                    var salt = Guid.NewGuid().ToString("N").Substring(0, 16);
                    var passwordHash = Convert.ToBase64String(
                        System.Security.Cryptography.SHA256.HashData(
                            System.Text.Encoding.UTF8.GetBytes(request.Password + salt)));

                    adminUser = new User
                    {
                        Username = request.AdminEmail.Split('@')[0] + DateTime.Now.Ticks.ToString().Substring(10),
                        Email = request.AdminEmail,
                        FirstName = request.FirstName,
                        LastName = request.LastName,
                        Phone = request.AdminPhone,
                        PasswordHash = passwordHash,
                        Salt = salt,
                        RoleId = 2, // Company Admin
                        CompanyId = company.CompanyId,
                        IsActive = true,
                        IsEmailVerified = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.Users.Add(adminUser);
                    await _context.SaveChangesAsync();

                    // ===== 3. CREATE SUBSCRIPTION =====
                    var planConfig = GetPlanConfig(request.PlanName);
                    var contractTerm = request.PlanName == "Enterprise" ? 24 : request.PlanName == "Pro" ? 12 : 12;
                    var nextDue = DateTime.UtcNow.AddMonths(request.BillingCycle == "Annual" ? 12 : 1);
                    var subscription = new CompanySubscription
                    {
                        CompanyId = company.CompanyId,
                        PlanName = request.PlanName,
                        Status = "Active",
                        BillingCycle = request.BillingCycle ?? "Monthly",
                        StartDate = DateTime.UtcNow,
                        MonthlyFee = planConfig.MonthlyFee,
                        MaxUsers = planConfig.MaxUsers,
                        Notes = $"Paid via PayMongo (Payment: {sessionStatus.PaymentId}) on {DateTime.UtcNow:yyyy-MM-dd}",
                        ContractAgreed = true,
                        ContractAgreedAt = DateTime.UtcNow,
                        ContractType = request.PlanName == "Enterprise" ? "Enterprise" : request.PlanName == "Pro" ? "Annual" : "Standard",
                        ContractTermMonths = contractTerm,
                        LastPaymentDate = DateTime.UtcNow,
                        NextDueDate = nextDue,
                        PaymentStatus = "Current",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        CreatedBy = adminUser.UserId
                    };

                    _context.CompanySubscriptions.Add(subscription);
                    await _context.SaveChangesAsync();

                    // ===== 4. CREATE MODULE ACCESS =====
                    foreach (var module in erpModules)
                    {
                        var access = new CompanyModuleAccess
                        {
                            CompanyId = company.CompanyId,
                            ModuleId = module.ModuleId,
                            IsEnabled = true,
                            ActivatedAt = DateTime.UtcNow,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.CompanyModuleAccess.Add(access);
                    }

                    var roleAccessRows = BuildRoleAccessForSubscribedModules(company.CompanyId, selectedModuleCodes);
                    _context.RoleModuleAccess.AddRange(roleAccessRows);

                    await _context.SaveChangesAsync();

                    // ===== 5. LOG =====
                    var usageLog = new PlatformUsageLog
                    {
                        CompanyId = company.CompanyId,
                        UserId = adminUser.UserId,
                        ModuleCode = "SUBSCRIPTION",
                        Action = "NEW_SUBSCRIPTION_PAID",
                        Details = $"Paid {request.PlanName} subscription via PayMongo. Payment ID: {sessionStatus.PaymentId}. Billing: {request.BillingCycle}",
                        IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.PlatformUsageLogs.Add(usageLog);
                    await _context.SaveChangesAsync();

                    // ===== 6. CREATE ACCOUNTING ENTRIES (Journal + GL) =====
                    await CreateSubscriptionAccountingEntries(subscription, company, sessionStatus.PaymentId ?? "");

                    await tx.CommitAsync();
                });

                // Clean up session data
                HttpContext.Session.Remove($"PendingReg_{registrationId}");
                HttpContext.Session.Remove($"CheckoutSession_{registrationId}");
                _memoryCache.Remove($"PendingReg_{registrationId}");
                _memoryCache.Remove($"CheckoutSession_{registrationId}");

                return Ok(new
                {
                    success = true,
                    message = "Payment verified and subscription created!",
                    planName = request.PlanName,
                    companyName = company.CompanyName,
                    adminEmail = adminUser.Email,
                    paymentId = sessionStatus.PaymentId
                });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        /// <summary>
        /// Get subscription contract terms and penalty schedule (public endpoint)
        /// </summary>
        [HttpGet("contract-terms")]
        public IActionResult GetContractTerms()
        {
            var terms = new
            {
                vatRate = 12.0m,
                vatName = "VAT",
                penaltyRate = 3.0m, // 3% monthly penalty on overdue
                gracePeriodDays = 15,
                contractTerms = new[]
                {
                    new { plan = "Starter", termMonths = 12, earlyTerminationFee = 2997m, description = "12-month minimum contract" },
                    new { plan = "Pro", termMonths = 12, earlyTerminationFee = 7497m, description = "12-month minimum contract" },
                    new { plan = "Enterprise", termMonths = 24, earlyTerminationFee = 24995m, description = "24-month minimum contract" }
                },
                legalReferences = new[]
                {
                    new { code = "RA 8792", title = "Electronic Commerce Act of 2000", description = "Recognizes the validity of electronic contracts and digital signatures in the Philippines." },
                    new { code = "RA 7394", title = "Consumer Act of the Philippines", description = "Protects consumer rights including transparency in pricing, penalties, and service agreements." },
                    new { code = "RA 386 Art. 1159", title = "Civil Code of the Philippines - Obligations & Contracts", description = "Obligations arising from contracts have the force of law between parties and must be complied with in good faith." },
                    new { code = "RA 386 Art. 1169", title = "Civil Code - Default/Delay", description = "Those obliged to deliver or do something incur delay from the time the obligee demands fulfillment. Demand is not necessary when the obligation expressly so provides." },
                    new { code = "RA 386 Art. 2209", title = "Civil Code - Interest on Delay", description = "If the obligation consists in the payment of a sum of money and the debtor incurs delay, the indemnity for damages shall be the payment of legal interest." },
                    new { code = "RA 3765", title = "Truth in Lending Act", description = "Requires full disclosure of all charges, fees, and penalties associated with any credit or service agreement." }
                },
                penaltySchedule = new[]
                {
                    new { months = "1", penalty = "3% of outstanding balance + service suspension warning", action = "Notice of Default" },
                    new { months = "2", penalty = "6% cumulative + account suspension", action = "Service Suspension" },
                    new { months = "3+", penalty = "9% cumulative + early termination fee + legal collection", action = "Account Termination & Legal Action" }
                }
            };
            return Ok(new { success = true, data = terms });
        }

        /// <summary>
        /// Get available plans and their pricing (public endpoint)
        /// </summary>
        [HttpGet("plans")]
        public IActionResult GetPlans()
        {
            var plans = new[]
            {
                new
                {
                    name = "Starter",
                    monthlyPrice = 999m,
                    annualPrice = 11988m,
                    maxUsers = 15,
                    maxAdmins = 1,
                    modules = new[] { "SALES", "INVENTORY" },
                    features = new[] { "Up to 15 Users", "1 Admin User", "Sales Module", "Inventory Module" }
                },
                new
                {
                    name = "Pro",
                    monthlyPrice = 2499m,
                    annualPrice = 29988m,
                    maxUsers = 50,
                    maxAdmins = 5,
                    modules = new[] { "SALES", "CUSTOMERS", "INVENTORY", "BILLING", "SUPPORT", "MARKETING" },
                    features = new[] { "Up to 50 Users", "Up to 5 Admin Users", "All ERP Modules", "Billing & Invoicing", "Advanced Analytics", "Priority Email Support" }
                },
                new
                {
                    name = "Enterprise",
                    monthlyPrice = 4999m,
                    annualPrice = 59988m,
                    maxUsers = 200,
                    maxAdmins = 10,
                    modules = new[] { "SALES", "CUSTOMERS", "INVENTORY", "BILLING", "SUPPORT", "MARKETING" },
                    features = new[] { "Up to 200 Users", "Up to 10 Admin Users", "All ERP Modules", "Dedicated Account Manager", "Advanced Analytics", "Priority Support" }
                }
            };

            return Ok(new { success = true, plans });
        }

        /// <summary>
        /// Get available ERP modules (public endpoint)
        /// </summary>
        [HttpGet("modules")]
        public async Task<IActionResult> GetModules()
        {
            var modules = await _context.ERPModules
                .Where(m => m.IsActive)
                .OrderBy(m => m.SortOrder)
                .Select(m => new
                {
                    m.ModuleId,
                    m.ModuleName,
                    m.ModuleCode,
                    m.Description,
                    m.Icon,
                    m.MonthlyPrice,
                    m.AnnualPrice
                })
                .ToListAsync();

            return Ok(new { success = true, modules });
        }

        // ===== HELPER METHODS =====

        private static PlanConfig GetPlanConfig(string planName)
        {
            return planName switch
            {
                "Starter" => new PlanConfig
                {
                    MonthlyFee = 999m,
                    MaxUsers = 15,
                    ModuleCodes = new[] { "SALES", "INVENTORY" }
                },
                "Pro" => new PlanConfig
                {
                    MonthlyFee = 2499m,
                    MaxUsers = 50,
                    ModuleCodes = new[] { "SALES", "CUSTOMERS", "INVENTORY", "BILLING", "SUPPORT", "MARKETING" }
                },
                "Enterprise" => new PlanConfig
                {
                    MonthlyFee = 4999m,
                    MaxUsers = 200,
                    ModuleCodes = new[] { "SALES", "CUSTOMERS", "INVENTORY", "BILLING", "SUPPORT", "MARKETING" }
                },
                _ => new PlanConfig
                {
                    MonthlyFee = 999m,
                    MaxUsers = 15,
                    ModuleCodes = new[] { "SALES", "INVENTORY" }
                }
            };
        }

        private static string[] ResolveSelectedModuleCodes(ERPSubscribeRequest request)
        {
            var allowed = GetPlanConfig(request.PlanName)
                .ModuleCodes
                .Select(m => m.ToUpperInvariant())
                .ToHashSet();

            var selected = (request.SelectedModuleCodes ?? new List<string>())
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Select(m => m.Trim().ToUpperInvariant())
                .Where(allowed.Contains)
                .Distinct()
                .ToList();

            if (!selected.Any())
            {
                selected = allowed.ToList();
            }

            return selected.ToArray();
        }

        private static List<RoleModuleAccess> BuildRoleAccessForSubscribedModules(int companyId, IEnumerable<string> subscribedModuleCodes)
        {
            var selected = subscribedModuleCodes
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Select(m => m.Trim().ToUpperInvariant())
                .Distinct()
                .ToHashSet();

            var roleModuleMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["SALES"] = 3,
                ["CUSTOMERS"] = 3,
                ["SUPPORT"] = 4,
                ["MARKETING"] = 5,
                ["BILLING"] = 6,
                ["INVENTORY"] = 8,
                ["ADMIN"] = 2
            };

            var allManagedModuleCodes = new[] { "SALES", "CUSTOMERS", "INVENTORY", "BILLING", "MARKETING", "SUPPORT", "ADMIN" };
            var allManagedRoles = new[] { 2, 3, 4, 5, 6, 8 };
            var rows = new List<RoleModuleAccess>();

            foreach (var roleId in allManagedRoles)
            {
                foreach (var moduleCode in allManagedModuleCodes)
                {
                    var hasAccess = roleId == 2
                        ? selected.Contains(moduleCode)
                        : selected.Contains(moduleCode)
                          && roleModuleMap.TryGetValue(moduleCode, out var mappedRole)
                          && mappedRole == roleId;

                    rows.Add(new RoleModuleAccess
                    {
                        CompanyId = companyId,
                        RoleId = roleId,
                        ModuleCode = moduleCode,
                        HasAccess = hasAccess,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            return rows;
        }


        /// <summary>
        /// Creates accounting entries (Journal Entry + General Ledger) for a new subscription.
        /// Debits Accounts Receivable for total (price + VAT), Credits Subscription Revenue for price, Credits Tax Payable for VAT.
        /// Uses platform-level accounting (CompanyId = null).
        /// </summary>
        private async Task CreateSubscriptionAccountingEntries(CompanySubscription subscription, Company company, string paymentRef = "")
        {
            try
            {
                const decimal VAT_RATE = 0.12m;
                var planAmount = subscription.MonthlyFee;
                var vatAmount = Math.Round(planAmount * VAT_RATE, 2);
                var totalAmount = planAmount + vatAmount;
                var entryDate = subscription.StartDate;

                // Get platform account IDs by code
                var accountMap = await _context.ChartOfAccounts
                    .Where(a => a.CompanyId == null && !a.IsArchived && a.IsActive)
                    .ToDictionaryAsync(a => a.AccountCode, a => a.AccountId);

                int GetAcctId(string code) => accountMap.ContainsKey(code) ? accountMap[code] : 0;

                var arAcctId = GetAcctId("1100");   // Accounts Receivable
                var revenueAcctId = GetAcctId("4000"); // Subscription Revenue
                var taxAcctId = GetAcctId("2100");  // Tax Payable

                if (arAcctId == 0 || revenueAcctId == 0 || taxAcctId == 0)
                {
                    // Accounts not set up yet - skip accounting but don't fail subscription
                    return;
                }

                // Generate entry number
                var entryCount = await _context.JournalEntries.CountAsync(e => e.CompanyId == null);
                var entryNumber = $"SUB-{subscription.SubscriptionId:D5}";

                // Create Journal Entry
                var journalEntry = new JournalEntry
                {
                    CompanyId = null,
                    EntryNumber = entryNumber,
                    EntryDate = entryDate,
                    Description = $"Subscription: {company.CompanyName} - {subscription.PlanName} ({subscription.BillingCycle})",
                    Reference = $"SUB-{subscription.SubscriptionId}",
                    Status = "Posted",
                    TotalDebit = totalAmount,
                    TotalCredit = totalAmount,
                    Notes = $"Auto-generated from {subscription.PlanName} subscription. Plan: \u20B1{planAmount:N2}, VAT 12%: \u20B1{vatAmount:N2}, Total: \u20B1{totalAmount:N2}.{(string.IsNullOrEmpty(paymentRef) ? "" : $" Payment: {paymentRef}")}",
                    PostedAt = entryDate,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Lines = new List<JournalEntryLine>
                    {
                        new JournalEntryLine
                        {
                            AccountId = arAcctId,
                            Description = $"{company.CompanyName} - {subscription.PlanName} (incl. VAT)",
                            DebitAmount = totalAmount,
                            CreditAmount = 0m
                        },
                        new JournalEntryLine
                        {
                            AccountId = revenueAcctId,
                            Description = $"{subscription.PlanName} {subscription.BillingCycle} Revenue",
                            DebitAmount = 0m,
                            CreditAmount = planAmount
                        },
                        new JournalEntryLine
                        {
                            AccountId = taxAcctId,
                            Description = $"VAT 12% on {subscription.PlanName} subscription",
                            DebitAmount = 0m,
                            CreditAmount = vatAmount
                        }
                    }
                };

                _context.JournalEntries.Add(journalEntry);
                await _context.SaveChangesAsync();

                // Create General Ledger entries (one per line)
                var glEntries = new List<GeneralLedgerEntry>
                {
                    new GeneralLedgerEntry
                    {
                        CompanyId = null,
                        AccountId = arAcctId,
                        EntryId = journalEntry.EntryId,
                        TransactionDate = entryDate,
                        Description = $"{company.CompanyName} - {subscription.PlanName} (incl. VAT)",
                        DebitAmount = totalAmount,
                        CreditAmount = 0m,
                        RunningBalance = 0m,
                        Reference = $"SUB-{subscription.SubscriptionId}",
                        CreatedAt = DateTime.UtcNow
                    },
                    new GeneralLedgerEntry
                    {
                        CompanyId = null,
                        AccountId = revenueAcctId,
                        EntryId = journalEntry.EntryId,
                        TransactionDate = entryDate,
                        Description = $"{subscription.PlanName} {subscription.BillingCycle} Revenue",
                        DebitAmount = 0m,
                        CreditAmount = planAmount,
                        RunningBalance = 0m,
                        Reference = $"SUB-{subscription.SubscriptionId}",
                        CreatedAt = DateTime.UtcNow
                    },
                    new GeneralLedgerEntry
                    {
                        CompanyId = null,
                        AccountId = taxAcctId,
                        EntryId = journalEntry.EntryId,
                        TransactionDate = entryDate,
                        Description = $"VAT 12% on {subscription.PlanName} subscription",
                        DebitAmount = 0m,
                        CreditAmount = vatAmount,
                        RunningBalance = 0m,
                        Reference = $"TAX-SUB-{subscription.SubscriptionId}",
                        CreatedAt = DateTime.UtcNow
                    }
                };

                _context.GeneralLedger.AddRange(glEntries);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                // Don't fail the subscription if accounting entries fail
                // The system-derived entries will still show in SuperAdmin
            }
        }

        private class PlanConfig
        {
            public decimal MonthlyFee { get; set; }
            public int MaxUsers { get; set; }
            public string[] ModuleCodes { get; set; } = Array.Empty<string>();
        }
    }

    // ===== REQUEST MODEL =====

    /// <summary>
    /// Subscription registration request from ERPWebsite
    /// </summary>
    public class ERPSubscribeRequest
    {
        // Company Info
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyEmail { get; set; } = string.Empty;
        public string CompanyPhone { get; set; } = string.Empty;
        public string? CompanyAddress { get; set; }
        public string? CompanyCity { get; set; }
        public string? CompanyCountry { get; set; }

        // Admin Account Info
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string AdminEmail { get; set; } = string.Empty;
        public string? AdminPhone { get; set; }
        public string Password { get; set; } = string.Empty;

        // Subscription
        public string PlanName { get; set; } = "Starter";
        public string? BillingCycle { get; set; } = "Monthly";
        public List<string>? SelectedModuleCodes { get; set; }

        // Contract Agreement
        public bool ContractAgreed { get; set; } = false;
    }

    /// <summary>
    /// Request model for verifying payment after PayMongo checkout
    /// </summary>
    public class PaymentVerifyRequest
    {
        public string RegistrationId { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
    }
}
