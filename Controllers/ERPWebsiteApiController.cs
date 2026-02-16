using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CompuGear.Data;
using CompuGear.Models;

namespace CompuGear.Controllers
{
    /// <summary>
    /// ERPWebsite API Controller - Handles subscription registration from the public website
    /// Creates Company, User (Company Admin), CompanySubscription, and CompanyModuleAccess records
    /// </summary>
    [Route("api/erpwebsite")]
    [ApiController]
    public class ERPWebsiteApiController : ControllerBase
    {
        private readonly CompuGearDbContext _context;

        public ERPWebsiteApiController(CompuGearDbContext context)
        {
            _context = context;
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

                if (request.PlanName != "Basic" && request.PlanName != "Pro")
                    return Ok(new { success = false, message = "Invalid plan selected." });

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

                // ===== 1. CREATE COMPANY =====
                var companyCode = $"CG-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
                var company = new Company
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

                var adminUser = new User
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
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = adminUser.UserId
                };

                _context.CompanySubscriptions.Add(subscription);
                await _context.SaveChangesAsync();

                // ===== 4. CREATE MODULE ACCESS =====
                var modulesCodes = planConfig.ModuleCodes;
                var erpModules = await _context.ERPModules
                    .Where(m => modulesCodes.Contains(m.ModuleCode) && m.IsActive)
                    .ToListAsync();

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

                // Also create RoleModuleAccess for the Company Admin
                foreach (var moduleCode in modulesCodes)
                {
                    var roleAccess = new RoleModuleAccess
                    {
                        CompanyId = company.CompanyId,
                        RoleId = 2, // Company Admin
                        ModuleCode = moduleCode,
                        HasAccess = true
                    };
                    _context.RoleModuleAccess.Add(roleAccess);
                }

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

                return Ok(new
                {
                    success = true,
                    message = "Subscription created successfully!",
                    planName = request.PlanName,
                    companyId = company.CompanyId,
                    companyName = company.CompanyName,
                    adminEmail = adminUser.Email
                });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = "An error occurred: " + ex.Message });
            }
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
                    name = "Basic",
                    monthlyPrice = 2499m,
                    annualPrice = 29988m,
                    maxUsers = 50,
                    maxAdmins = 3,
                    modules = new[] { "SALES", "INVENTORY" },
                    features = new[] { "Up to 50 Users", "Up to 3 Admin Users", "Sales Module", "Inventory Module" }
                },
                new
                {
                    name = "Pro",
                    monthlyPrice = 4999m,
                    annualPrice = 59988m,
                    maxUsers = 200,
                    maxAdmins = 10,
                    modules = new[] { "SALES", "INVENTORY", "BILLING", "SUPPORT", "MARKETING", "ADMIN" },
                    features = new[] { "Up to 200 Users", "Up to 10 Admin Users", "All ERP Modules", "Billing & Invoicing", "Advanced Analytics", "Priority Email Support" }
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
                "Basic" => new PlanConfig
                {
                    MonthlyFee = 2499m,
                    MaxUsers = 50,
                    ModuleCodes = new[] { "SALES", "INVENTORY" }
                },
                "Pro" => new PlanConfig
                {
                    MonthlyFee = 4999m,
                    MaxUsers = 200,
                    ModuleCodes = new[] { "SALES", "INVENTORY", "BILLING", "SUPPORT", "MARKETING", "ADMIN" }
                },
                _ => new PlanConfig
                {
                    MonthlyFee = 2499m,
                    MaxUsers = 50,
                    ModuleCodes = new[] { "SALES", "INVENTORY" }
                }
            };
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
        public string PlanName { get; set; } = "Basic";
        public string? BillingCycle { get; set; } = "Monthly";
    }
}
