using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using CompuGear.Data;
using CompuGear.Models;
using System.Text;
using System.Security.Cryptography;

namespace CompuGear.Controllers
{
    /// <summary>
    /// Super Admin API Controller - REST API for Super Admin operations
    /// Only accessible by Super Admin (RoleId = 1)
    /// </summary>
    [Route("api/superadmin")]
    [ApiController]
    public class SuperAdminApiController(CompuGearDbContext context, IMemoryCache cache) : ControllerBase
    {
        private readonly CompuGearDbContext _context = context;
        private readonly IMemoryCache _cache = cache;

        private bool IsSuperAdmin()
        {
            var roleId = HttpContext.Session.GetInt32("RoleId");
            return roleId == 1;
        }

        #region Dashboard Stats

        [HttpGet("dashboard/stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            if (!IsSuperAdmin()) return Unauthorized();

            // Use memory cache for dashboard stats (refresh every 30 seconds)
            var cacheKey = "sa_dashboard_stats";
            if (_cache.TryGetValue(cacheKey, out object? cached))
                return Ok(cached);

            // Batch multiple counts into fewer queries using raw SQL or parallel-safe approach
            var companyStats = await _context.Companies
                .GroupBy(c => 1)
                .Select(g => new { Total = g.Count(), Active = g.Count(c => c.IsActive) })
                .FirstOrDefaultAsync();

            var userStats = await _context.Users
                .GroupBy(u => 1)
                .Select(g => new { Total = g.Count(), Active = g.Count(u => u.IsActive) })
                .FirstOrDefaultAsync();

            var subStats = await _context.CompanySubscriptions
                .GroupBy(s => 1)
                .Select(g => new { 
                    Total = g.Count(), 
                    Active = g.Count(s => s.Status == "Active"),
                    Revenue = g.Where(s => s.Status == "Active").Sum(s => s.MonthlyFee)
                })
                .FirstOrDefaultAsync();

            var totalModules = await _context.ERPModules.CountAsync(m => m.IsActive);

            var result = new
            {
                totalCompanies = companyStats?.Total ?? 0,
                activeCompanies = companyStats?.Active ?? 0,
                totalUsers = userStats?.Total ?? 0,
                activeUsers = userStats?.Active ?? 0,
                totalSubscriptions = subStats?.Total ?? 0,
                activeSubscriptions = subStats?.Active ?? 0,
                totalModules,
                monthlyRevenue = subStats?.Revenue ?? 0m
            };

            _cache.Set(cacheKey, result, TimeSpan.FromSeconds(30));
            return Ok(result);
        }

        #endregion

        #region Companies CRUD

        [HttpGet("companies")]
        public async Task<IActionResult> GetCompanies()
        {
            if (!IsSuperAdmin()) return Unauthorized();

            var companies = await _context.Companies
                .AsNoTracking()
                .Select(c => new
                {
                    c.CompanyId,
                    c.CompanyName,
                    c.CompanyCode,
                    c.Email,
                    c.Phone,
                    c.Address,
                    c.City,
                    c.Country,
                    c.Website,
                    c.TaxId,
                    c.IsActive,
                    c.CreatedAt,
                    UserCount = _context.Users.Count(u => u.CompanyId == c.CompanyId),
                    Subscription = _context.CompanySubscriptions
                        .Where(s => s.CompanyId == c.CompanyId)
                        .OrderByDescending(s => s.CreatedAt)
                        .Select(s => new { s.PlanName, s.Status, s.MonthlyFee, s.MaxUsers })
                        .FirstOrDefault(),
                    ModuleCount = _context.CompanyModuleAccess
                        .Count(a => a.CompanyId == c.CompanyId && a.IsEnabled)
                })
                .OrderBy(c => c.CompanyName)
                .ToListAsync();

            return Ok(companies);
        }

        [HttpGet("companies/{id}")]
        public async Task<IActionResult> GetCompany(int id)
        {
            if (!IsSuperAdmin()) return Unauthorized();

            var company = await _context.Companies.FindAsync(id);
            if (company == null) return NotFound();

            var users = await _context.Users
                .Where(u => u.CompanyId == id)
                .Include(u => u.Role)
                .Select(u => new
                {
                    u.UserId,
                    u.Username,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.IsActive,
                    RoleName = u.Role != null ? u.Role.RoleName : "Unknown",
                    u.LastLoginAt,
                    u.CreatedAt
                })
                .ToListAsync();

            var subscription = await _context.CompanySubscriptions
                .Where(s => s.CompanyId == id)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();

            var modules = await _context.CompanyModuleAccess
                .Where(a => a.CompanyId == id)
                .Include(a => a.Module)
                .Select(a => new
                {
                    a.AccessId,
                    a.ModuleId,
                    a.Module.ModuleName,
                    a.Module.ModuleCode,
                    a.IsEnabled,
                    a.ActivatedAt,
                    a.DeactivatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                company,
                users,
                subscription,
                modules
            });
        }

        [HttpPost("companies")]
        public async Task<IActionResult> CreateCompany([FromBody] Company company)
        {
            if (!IsSuperAdmin()) return Unauthorized();

            if (string.IsNullOrEmpty(company.CompanyName))
                return BadRequest(new { message = "Company name is required" });

            // Generate company code
            if (string.IsNullOrEmpty(company.CompanyCode))
            {
                var initials = string.Join("", company.CompanyName.Split(' ').Select(w => w.Length > 0 ? w[0].ToString().ToUpper() : ""));
                var count = await _context.Companies.CountAsync() + 1;
                company.CompanyCode = $"{initials}-{count:D3}";
            }

            // Check unique code
            if (await _context.Companies.AnyAsync(c => c.CompanyCode == company.CompanyCode))
                return BadRequest(new { message = "Company code already exists" });

            company.CreatedAt = DateTime.UtcNow;
            company.UpdatedAt = DateTime.UtcNow;

            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            // Create default subscription
            _context.CompanySubscriptions.Add(new CompanySubscription
            {
                CompanyId = company.CompanyId,
                PlanName = "Trial",
                Status = "Trial",
                BillingCycle = "Monthly",
                MonthlyFee = 0,
                MaxUsers = 3,
                StartDate = DateTime.UtcNow,
                TrialEndDate = DateTime.UtcNow.AddDays(30)
            });
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Company created successfully", data = company });
        }

        [HttpPut("companies/{id}")]
        public async Task<IActionResult> UpdateCompany(int id, [FromBody] Company company)
        {
            if (!IsSuperAdmin()) return Unauthorized();

            var existing = await _context.Companies.FindAsync(id);
            if (existing == null) return NotFound();

            existing.CompanyName = company.CompanyName ?? existing.CompanyName;
            existing.CompanyCode = company.CompanyCode ?? existing.CompanyCode;
            existing.Email = company.Email ?? existing.Email;
            existing.Phone = company.Phone ?? existing.Phone;
            existing.Address = company.Address ?? existing.Address;
            existing.City = company.City ?? existing.City;
            existing.Country = company.Country ?? existing.Country;
            existing.Website = company.Website ?? existing.Website;
            existing.TaxId = company.TaxId ?? existing.TaxId;
            existing.IsActive = company.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Company updated successfully" });
        }

        [HttpDelete("companies/{id}")]
        public async Task<IActionResult> DeleteCompany(int id)
        {
            if (!IsSuperAdmin()) return Unauthorized();

            var company = await _context.Companies.FindAsync(id);
            if (company == null) return NotFound();

            // Soft delete - deactivate
            company.IsActive = false;
            company.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Company deactivated successfully" });
        }

        #endregion

        #region Subscriptions CRUD

        [HttpGet("subscriptions")]
        public async Task<IActionResult> GetSubscriptions()
        {
            if (!IsSuperAdmin()) return Unauthorized();

            var subs = await _context.CompanySubscriptions
                .Include(s => s.Company)
                .Select(s => new
                {
                    s.SubscriptionId,
                    s.CompanyId,
                    s.Company.CompanyName,
                    s.PlanName,
                    s.Status,
                    s.BillingCycle,
                    s.StartDate,
                    s.EndDate,
                    s.TrialEndDate,
                    s.MonthlyFee,
                    s.MaxUsers,
                    s.Notes,
                    s.CreatedAt
                })
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return Ok(subs);
        }

        [HttpPost("subscriptions")]
        public async Task<IActionResult> CreateSubscription([FromBody] CompanySubscription sub)
        {
            if (!IsSuperAdmin()) return Unauthorized();

            sub.CreatedAt = DateTime.UtcNow;
            sub.UpdatedAt = DateTime.UtcNow;
            sub.CreatedBy = HttpContext.Session.GetInt32("UserId");

            _context.CompanySubscriptions.Add(sub);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Subscription created successfully" });
        }

        [HttpPut("subscriptions/{id}")]
        public async Task<IActionResult> UpdateSubscription(int id, [FromBody] CompanySubscription sub)
        {
            if (!IsSuperAdmin()) return Unauthorized();

            var existing = await _context.CompanySubscriptions.FindAsync(id);
            if (existing == null) return NotFound();

            existing.PlanName = sub.PlanName ?? existing.PlanName;
            existing.Status = sub.Status ?? existing.Status;
            existing.BillingCycle = sub.BillingCycle ?? existing.BillingCycle;
            existing.MonthlyFee = sub.MonthlyFee;
            existing.MaxUsers = sub.MaxUsers;
            existing.EndDate = sub.EndDate;
            existing.Notes = sub.Notes;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = HttpContext.Session.GetInt32("UserId");

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Subscription updated successfully" });
        }

        [HttpDelete("subscriptions/{id}")]
        public async Task<IActionResult> DeleteSubscription(int id)
        {
            if (!IsSuperAdmin()) return Unauthorized();

            var sub = await _context.CompanySubscriptions.FindAsync(id);
            if (sub == null) return NotFound();

            sub.Status = "Cancelled";
            sub.EndDate = DateTime.UtcNow;
            sub.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Subscription cancelled" });
        }

        #endregion

        #region ERP Modules CRUD

        [HttpGet("modules")]
        public async Task<IActionResult> GetModules()
        {
            if (!IsSuperAdmin()) return Unauthorized();

            var modules = await _context.ERPModules
                .Select(m => new
                {
                    m.ModuleId,
                    m.ModuleName,
                    m.ModuleCode,
                    m.Description,
                    m.IsActive,
                    m.MonthlyPrice,
                    m.AnnualPrice,
                    m.Features,
                    m.SortOrder,
                    m.CreatedAt,
                    SubscribedCompanies = _context.CompanyModuleAccess.Count(a => a.ModuleId == m.ModuleId && a.IsEnabled)
                })
                .OrderBy(m => m.SortOrder)
                .ToListAsync();

            return Ok(modules);
        }

        [HttpPost("modules")]
        public async Task<IActionResult> CreateModule([FromBody] ERPModule module)
        {
            if (!IsSuperAdmin()) return Unauthorized();

            if (string.IsNullOrEmpty(module.ModuleName) || string.IsNullOrEmpty(module.ModuleCode))
                return BadRequest(new { message = "Module name and code are required" });

            if (await _context.ERPModules.AnyAsync(m => m.ModuleCode == module.ModuleCode))
                return BadRequest(new { message = "Module code already exists" });

            module.CreatedAt = DateTime.UtcNow;
            module.UpdatedAt = DateTime.UtcNow;

            _context.ERPModules.Add(module);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Module created successfully", data = module });
        }

        [HttpPut("modules/{id}")]
        public async Task<IActionResult> UpdateModule(int id, [FromBody] ERPModule module)
        {
            if (!IsSuperAdmin()) return Unauthorized();

            var existing = await _context.ERPModules.FindAsync(id);
            if (existing == null) return NotFound();

            existing.ModuleName = module.ModuleName ?? existing.ModuleName;
            existing.ModuleCode = module.ModuleCode ?? existing.ModuleCode;
            existing.Description = module.Description ?? existing.Description;
            existing.MonthlyPrice = module.MonthlyPrice;
            existing.AnnualPrice = module.AnnualPrice;
            existing.IsActive = module.IsActive;
            existing.Features = module.Features ?? existing.Features;
            existing.SortOrder = module.SortOrder;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Module updated successfully" });
        }

        [HttpDelete("modules/{id}")]
        public async Task<IActionResult> DeleteModule(int id)
        {
            if (!IsSuperAdmin()) return Unauthorized();

            var module = await _context.ERPModules.FindAsync(id);
            if (module == null) return NotFound();

            module.IsActive = false;
            module.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Module deactivated successfully" });
        }

        #endregion

        #region Company Module Access

        [HttpGet("company-modules/{companyId}")]
        public async Task<IActionResult> GetCompanyModules(int companyId)
        {
            if (!IsSuperAdmin()) return Unauthorized();

            var allModules = await _context.ERPModules
                .Where(m => m.IsActive)
                .OrderBy(m => m.SortOrder)
                .ToListAsync();

            var companyAccess = await _context.CompanyModuleAccess
                .Where(a => a.CompanyId == companyId)
                .ToListAsync();

            var result = allModules.Select(m =>
            {
                var access = companyAccess.FirstOrDefault(a => a.ModuleId == m.ModuleId);
                return new
                {
                    m.ModuleId,
                    m.ModuleName,
                    m.ModuleCode,
                    m.Description,
                    m.MonthlyPrice,
                    m.AnnualPrice,
                    m.Features,
                    IsEnabled = access?.IsEnabled ?? false,
                    access?.AccessId,
                    access?.ActivatedAt
                };
            });

            return Ok(result);
        }

        [HttpPost("company-modules")]
        public async Task<IActionResult> ToggleModuleAccess([FromBody] CompanyModuleAccessRequest request)
        {
            if (!IsSuperAdmin()) return Unauthorized();

            var existing = await _context.CompanyModuleAccess
                .FirstOrDefaultAsync(a => a.CompanyId == request.CompanyId && a.ModuleId == request.ModuleId);

            if (existing != null)
            {
                existing.IsEnabled = request.IsEnabled;
                existing.ActivatedAt = request.IsEnabled ? DateTime.UtcNow : existing.ActivatedAt;
                existing.DeactivatedAt = !request.IsEnabled ? DateTime.UtcNow : null;
            }
            else
            {
                _context.CompanyModuleAccess.Add(new CompanyModuleAccess
                {
                    CompanyId = request.CompanyId,
                    ModuleId = request.ModuleId,
                    IsEnabled = request.IsEnabled,
                    ActivatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = request.IsEnabled ? "Module enabled" : "Module disabled" });
        }

        [HttpPost("company-modules/bulk")]
        public async Task<IActionResult> BulkToggleModules([FromBody] BulkModuleAccessRequest request)
        {
            if (!IsSuperAdmin()) return Unauthorized();

            foreach (var moduleId in request.ModuleIds)
            {
                var existing = await _context.CompanyModuleAccess
                    .FirstOrDefaultAsync(a => a.CompanyId == request.CompanyId && a.ModuleId == moduleId);

                if (existing != null)
                {
                    existing.IsEnabled = request.IsEnabled;
                    existing.ActivatedAt = request.IsEnabled ? DateTime.UtcNow : existing.ActivatedAt;
                    existing.DeactivatedAt = !request.IsEnabled ? DateTime.UtcNow : null;
                }
                else
                {
                    _context.CompanyModuleAccess.Add(new CompanyModuleAccess
                    {
                        CompanyId = request.CompanyId,
                        ModuleId = moduleId,
                        IsEnabled = request.IsEnabled,
                        ActivatedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = $"{request.ModuleIds.Count} modules updated" });
        }

        #endregion

        #region Platform Usage

        [HttpGet("usage")]
        public async Task<IActionResult> GetUsageLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            if (!IsSuperAdmin()) return Unauthorized();

            var query = _context.PlatformUsageLogs
                .Include(l => l.Company)
                .Include(l => l.User)
                .OrderByDescending(l => l.CreatedAt);

            var total = await query.CountAsync();
            var logs = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new
                {
                    l.LogId,
                    CompanyName = l.Company != null ? l.Company.CompanyName : "N/A",
                    UserName = l.User != null ? l.User.FirstName + " " + l.User.LastName : "System",
                    l.ModuleCode,
                    l.Action,
                    l.Details,
                    l.IPAddress,
                    l.CreatedAt
                })
                .ToListAsync();

            return Ok(new { data = logs, total, page, pageSize });
        }

        [HttpGet("usage/stats")]
        public async Task<IActionResult> GetUsageStats()
        {
            if (!IsSuperAdmin()) return Unauthorized();

            var now = DateTime.UtcNow;
            var last30Days = now.AddDays(-30);

            var totalLogs = await _context.PlatformUsageLogs.CountAsync();
            var recentLogs = await _context.PlatformUsageLogs.CountAsync(l => l.CreatedAt >= last30Days);
            var activeCompanies = await _context.PlatformUsageLogs
                .Where(l => l.CreatedAt >= last30Days && l.CompanyId != null)
                .Select(l => l.CompanyId)
                .Distinct()
                .CountAsync();

            var moduleUsage = await _context.PlatformUsageLogs
                .Where(l => l.CreatedAt >= last30Days && l.ModuleCode != null)
                .GroupBy(l => l.ModuleCode)
                .Select(g => new { Module = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ToListAsync();

            return Ok(new
            {
                totalLogs,
                recentLogs,
                activeCompanies,
                moduleUsage
            });
        }

        #endregion

        #region All Users (Platform-wide)

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            if (!IsSuperAdmin()) return Unauthorized();

            var users = await _context.Users
                .AsNoTracking()
                .Select(u => new
                {
                    u.UserId,
                    u.Username,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.Phone,
                    u.RoleId,
                    RoleName = u.Role != null ? u.Role.RoleName : "Unknown",
                    CompanyName = u.Company != null ? u.Company.CompanyName : "N/A",
                    u.CompanyId,
                    u.IsActive,
                    u.IsEmailVerified,
                    u.LastLoginAt,
                    u.CreatedAt
                })
                .OrderBy(u => u.CompanyName)
                .ThenBy(u => u.LastName)
                .ToListAsync();

            return Ok(users);
        }

        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] User user)
        {
            if (!IsSuperAdmin()) return Unauthorized();

            if (string.IsNullOrEmpty(user.Email) || string.IsNullOrEmpty(user.Password))
                return BadRequest(new { message = "Email and password are required" });

            if (await _context.Users.AnyAsync(u => u.Email == user.Email))
                return BadRequest(new { message = "Email already exists" });

            var salt = Guid.NewGuid().ToString("N")[..16];
            user.Salt = salt;
            user.PasswordHash = Convert.ToBase64String(
                SHA256.HashData(
                    Encoding.UTF8.GetBytes(user.Password + salt)));
            user.Username = user.Email.Split('@')[0] + DateTime.Now.Ticks.ToString()[10..];
            user.IsActive = true;
            user.IsEmailVerified = true;
            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            user.CreatedBy = HttpContext.Session.GetInt32("UserId");

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "User created successfully" });
        }

        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] User user)
        {
            if (!IsSuperAdmin()) return Unauthorized();

            var existing = await _context.Users.FindAsync(id);
            if (existing == null) return NotFound();

            existing.FirstName = user.FirstName ?? existing.FirstName;
            existing.LastName = user.LastName ?? existing.LastName;
            existing.Email = user.Email ?? existing.Email;
            existing.Phone = user.Phone ?? existing.Phone;
            existing.RoleId = user.RoleId > 0 ? user.RoleId : existing.RoleId;
            existing.CompanyId = user.CompanyId ?? existing.CompanyId;
            existing.IsActive = user.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = HttpContext.Session.GetInt32("UserId");

            if (!string.IsNullOrEmpty(user.Password))
            {
                var salt = existing.Salt ?? Guid.NewGuid().ToString("N")[..16];
                existing.Salt = salt;
                existing.PasswordHash = Convert.ToBase64String(
                    SHA256.HashData(
                        Encoding.UTF8.GetBytes(user.Password + salt)));
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "User updated successfully" });
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            if (!IsSuperAdmin()) return Unauthorized();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "User deactivated successfully" });
        }

        #endregion

        #region Platform Reports

        [HttpGet("reports/overview")]
        public async Task<IActionResult> GetReportsOverview()
        {
            if (!IsSuperAdmin()) return Unauthorized();

            // Cache reports for 60 seconds
            var cacheKey = "sa_reports_overview";
            if (_cache.TryGetValue(cacheKey, out object? cachedReport))
                return Ok(cachedReport);

            var companies = await _context.Companies.CountAsync();
            var users = await _context.Users.CountAsync();
            var orderStats = await _context.Orders
                .GroupBy(o => 1)
                .Select(g => new { Count = g.Count(), Revenue = g.Sum(o => o.TotalAmount) })
                .FirstOrDefaultAsync();
            var orders = orderStats?.Count ?? 0;
            var revenue = orderStats?.Revenue ?? 0m;
            var tickets = await _context.SupportTickets.CountAsync();
            var products = await _context.Products.CountAsync();

            var subscriptionsByPlan = await _context.CompanySubscriptions
                .Where(s => s.Status == "Active")
                .GroupBy(s => s.PlanName)
                .Select(g => new { Plan = g.Key, Count = g.Count(), Revenue = g.Sum(s => s.MonthlyFee) })
                .ToListAsync();

            var usersByRole = await _context.Users
                .Include(u => u.Role)
                .GroupBy(u => u.Role!.RoleName)
                .Select(g => new { Role = g.Key, Count = g.Count() })
                .ToListAsync();

            var reportResult = new
            {
                companies,
                users,
                orders,
                revenue,
                tickets,
                products,
                subscriptionsByPlan,
                usersByRole
            };

            _cache.Set(cacheKey, reportResult, TimeSpan.FromSeconds(60));
            return Ok(reportResult);
        }

        #endregion

        #region System Settings

        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings()
        {
            if (!IsSuperAdmin()) return Unauthorized();

            var settings = await _context.SystemSettings.AsNoTracking().ToListAsync();
            return Ok(settings);
        }

        [HttpPost("settings")]
        public async Task<IActionResult> SaveSetting([FromBody] SystemSetting setting)
        {
            if (!IsSuperAdmin()) return Unauthorized();

            var existing = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.SettingKey == setting.SettingKey);

            if (existing != null)
            {
                existing.SettingValue = setting.SettingValue;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                setting.UpdatedAt = DateTime.UtcNow;
                _context.SystemSettings.Add(setting);
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Setting saved successfully" });
        }

        #endregion

        #region Roles

        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles()
        {
            if (!IsSuperAdmin()) return Unauthorized();
            var roles = await _context.Roles.AsNoTracking().OrderBy(r => r.RoleId).ToListAsync();
            return Ok(roles);
        }

        #endregion

        #region Subscription Penalties & Overdue Management

        /// <summary>
        /// Get all subscriptions with their payment/penalty status
        /// </summary>
        [HttpGet("subscription-penalties")]
        public async Task<IActionResult> GetSubscriptionPenalties()
        {
            if (!IsSuperAdmin()) return Unauthorized();
            try
            {
                var subs = await _context.CompanySubscriptions
                    .AsNoTracking()
                    .Include(s => s.Company)
                    .Where(s => s.Status != "Cancelled")
                    .OrderByDescending(s => s.OverdueMonths)
                    .ThenByDescending(s => s.PenaltyAmount)
                    .Select(s => new
                    {
                        s.SubscriptionId, s.CompanyId,
                        s.Company.CompanyName,
                        CompanyEmail = s.Company.Email,
                        s.PlanName, s.Status, s.BillingCycle, s.MonthlyFee,
                        s.ContractAgreed, s.ContractAgreedAt, s.ContractType, s.ContractTermMonths,
                        s.OverdueMonths, s.PenaltyAmount, s.TotalAmountDue,
                        s.PaymentStatus, s.LastPaymentDate, s.NextDueDate,
                        s.StartDate, s.CreatedAt
                    }).ToListAsync();

                var stats = new
                {
                    TotalSubscriptions = subs.Count,
                    Current = subs.Count(s => s.PaymentStatus == "Current"),
                    Overdue = subs.Count(s => s.PaymentStatus == "Overdue"),
                    Delinquent = subs.Count(s => s.PaymentStatus == "Delinquent"),
                    Suspended = subs.Count(s => s.PaymentStatus == "Suspended"),
                    TotalPenalties = subs.Sum(s => s.PenaltyAmount),
                    TotalOverdue = subs.Sum(s => s.TotalAmountDue)
                };

                return Ok(new { data = subs, stats });
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message, inner = ex.InnerException?.Message }); }
        }

        /// <summary>
        /// Process overdue check for all active subscriptions.
        /// Calculates penalties based on 3% monthly rate.
        /// </summary>
        [HttpPost("process-overdue")]
        public async Task<IActionResult> ProcessOverdueSubscriptions()
        {
            if (!IsSuperAdmin()) return Unauthorized();
            try
            {
                const decimal PENALTY_RATE = 0.03m; // 3% per month
                var now = DateTime.UtcNow;
                var processed = 0;

                var subs = await _context.CompanySubscriptions
                    .Where(s => s.Status == "Active" && s.NextDueDate.HasValue && s.NextDueDate.Value < now)
                    .ToListAsync();

                foreach (var sub in subs)
                {
                    var monthsOverdue = (int)Math.Floor((now - sub.NextDueDate!.Value).TotalDays / 30.0);
                    if (monthsOverdue < 1) continue;

                    sub.OverdueMonths = monthsOverdue;
                    var outstanding = sub.MonthlyFee * monthsOverdue;
                    sub.PenaltyAmount = Math.Round(outstanding * PENALTY_RATE * monthsOverdue, 2);
                    sub.TotalAmountDue = outstanding + sub.PenaltyAmount;

                    if (monthsOverdue >= 3)
                    {
                        sub.PaymentStatus = "Delinquent";
                        sub.Status = "Suspended";
                    }
                    else if (monthsOverdue >= 2)
                    {
                        sub.PaymentStatus = "Suspended";
                    }
                    else
                    {
                        sub.PaymentStatus = "Overdue";
                    }

                    sub.UpdatedAt = now;
                    processed++;
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = $"Processed {processed} overdue subscriptions.", processedCount = processed });
            }
            catch (Exception ex) { return Ok(new { success = false, message = ex.Message }); }
        }

        /// <summary>
        /// Record a payment for an overdue subscription, clearing penalties.
        /// </summary>
        [HttpPost("record-payment/{subscriptionId}")]
        public async Task<IActionResult> RecordPayment(int subscriptionId)
        {
            if (!IsSuperAdmin()) return Unauthorized();
            try
            {
                var sub = await _context.CompanySubscriptions.FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId);
                if (sub == null) return NotFound(new { message = "Subscription not found" });

                sub.OverdueMonths = 0;
                sub.PenaltyAmount = 0;
                sub.TotalAmountDue = 0;
                sub.LastPaymentDate = DateTime.UtcNow;
                sub.NextDueDate = sub.BillingCycle == "Annual" ? DateTime.UtcNow.AddYears(1) : DateTime.UtcNow.AddMonths(1);
                sub.PaymentStatus = "Current";
                sub.Status = "Active";
                sub.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Payment recorded. Account restored to current status." });
            }
            catch (Exception ex) { return Ok(new { success = false, message = ex.Message }); }
        }

        #endregion

        #region Platform Accounting - Chart of Accounts

        [HttpGet("chart-of-accounts")]
        public async Task<IActionResult> GetChartOfAccounts()
        {
            if (!IsSuperAdmin()) return Unauthorized();
            try
            {
                var accounts = await _context.ChartOfAccounts
                    .Where(a => a.CompanyId == null && !a.IsArchived)
                    .OrderBy(a => a.AccountCode)
                    .Select(a => new
                    {
                        a.AccountId, a.AccountCode, a.AccountName, a.AccountType,
                        a.NormalBalance, a.ParentAccountId, a.Description,
                        a.IsActive, a.CreatedAt, a.UpdatedAt
                    }).ToListAsync();

                // Compute balances from GL + system data
                var balances = await ComputePlatformBalances();

                var result = accounts.Select(a =>
                {
                    var bal = balances.TryGetValue(a.AccountId, out var foundBal) ? foundBal : (0m, 0m);
                    var net = bal.Item1 - bal.Item2;
                    return new
                    {
                        a.AccountId, a.AccountCode, a.AccountName, a.AccountType,
                        a.NormalBalance, a.ParentAccountId, a.Description,
                        a.IsActive, a.CreatedAt, a.UpdatedAt,
                        TotalDebit = bal.Item1,
                        TotalCredit = bal.Item2,
                        Balance = net,
                        DisplayBalance = a.NormalBalance == "Debit" ? net : -net
                    };
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message, inner = ex.InnerException?.Message }); }
        }

        /// <summary>
        /// Lightweight COA list for dropdowns - cached for 2 minutes
        /// </summary>
        [HttpGet("chart-of-accounts/simple")]
        public async Task<IActionResult> GetChartOfAccountsSimple()
        {
            if (!IsSuperAdmin()) return Unauthorized();
            const string cacheKey = "coa_simple_list";
            if (!_cache.TryGetValue(cacheKey, out object? cached))
            {
                var accounts = await _context.ChartOfAccounts
                    .Where(a => a.CompanyId == null && !a.IsArchived && a.IsActive)
                    .OrderBy(a => a.AccountCode)
                    .Select(a => new { a.AccountId, a.AccountCode, a.AccountName, a.AccountType, a.NormalBalance })
                    .ToListAsync();
                cached = accounts;
                _cache.Set(cacheKey, cached, TimeSpan.FromMinutes(2));
            }
            return Ok(cached);
        }

        [HttpGet("chart-of-accounts/{id}")]
        public async Task<IActionResult> GetChartOfAccountById(int id)
        {
            if (!IsSuperAdmin()) return Unauthorized();
            var account = await _context.ChartOfAccounts
                .Where(a => a.AccountId == id && a.CompanyId == null)
                .FirstOrDefaultAsync();
            if (account == null) return NotFound(new { message = "Account not found" });
            return Ok(account);
        }

        [HttpPost("chart-of-accounts")]
        public async Task<IActionResult> CreateChartOfAccount([FromBody] ChartOfAccount account)
        {
            if (!IsSuperAdmin()) return Unauthorized();
            if (string.IsNullOrWhiteSpace(account.AccountCode) || string.IsNullOrWhiteSpace(account.AccountName))
                return BadRequest(new { message = "Account code and name are required" });

            if (await _context.ChartOfAccounts.AnyAsync(a => a.CompanyId == null && a.AccountCode == account.AccountCode && !a.IsArchived))
                return BadRequest(new { message = "Account code already exists" });

            account.CompanyId = null;
            account.CreatedAt = DateTime.UtcNow;
            account.UpdatedAt = DateTime.UtcNow;
            _context.ChartOfAccounts.Add(account);
            await _context.SaveChangesAsync();
            _cache.Remove("coa_simple_list");
            return Ok(new { success = true, message = "Account created", data = account });
        }

        [HttpPut("chart-of-accounts/{id}")]
        public async Task<IActionResult> UpdateChartOfAccount(int id, [FromBody] ChartOfAccount account)
        {
            if (!IsSuperAdmin()) return Unauthorized();
            var existing = await _context.ChartOfAccounts.FirstOrDefaultAsync(a => a.AccountId == id && a.CompanyId == null);
            if (existing == null) return NotFound(new { message = "Account not found" });

            existing.AccountCode = account.AccountCode;
            existing.AccountName = account.AccountName;
            existing.AccountType = account.AccountType;
            existing.NormalBalance = account.NormalBalance;
            existing.ParentAccountId = account.ParentAccountId;
            existing.Description = account.Description;
            existing.IsActive = account.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            _cache.Remove("coa_simple_list");
            return Ok(new { success = true, message = "Account updated" });
        }

        [HttpPut("chart-of-accounts/{id}/archive")]
        public async Task<IActionResult> ArchiveChartOfAccount(int id)
        {
            if (!IsSuperAdmin()) return Unauthorized();
            var account = await _context.ChartOfAccounts.FirstOrDefaultAsync(a => a.AccountId == id && a.CompanyId == null);
            if (account == null) return NotFound(new { message = "Account not found" });
            account.IsArchived = true;
            account.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            _cache.Remove("coa_simple_list");
            return Ok(new { success = true, message = "Account archived" });
        }

        #endregion

        #region Platform Accounting - Journal Entries

        [HttpGet("journal-entries")]
        public async Task<IActionResult> GetJournalEntries()
        {
            if (!IsSuperAdmin()) return Unauthorized();
            try
            {
                // Manual entries - no Line eager-loading for list view (loaded on detail only)
                var manualEntries = await _context.JournalEntries
                    .Where(e => e.CompanyId == null && !e.IsArchived)
                    .OrderByDescending(e => e.EntryDate)
                    .Select(e => new
                    {
                        e.EntryId, e.EntryNumber, e.EntryDate, e.Description, e.Reference,
                        e.Status, e.TotalDebit, e.TotalCredit, e.Notes, e.PostedAt, e.CreatedAt,
                        Source = (e.Reference != null && (e.Reference.StartsWith("SUB-") || e.Reference.StartsWith("TAX-"))) ? "System" : "Manual",
                        SourceType = e.Reference != null && e.Reference.StartsWith("SUB-") ? "Subscription"
                                   : e.Reference != null && e.Reference.StartsWith("TAX-") ? "Tax"
                                   : "Journal Entry"
                    }).ToListAsync();

                // System-derived entries: quick subscription summary (single optimized query)
                var existingSubRefsSet = manualEntries
                    .Where(e => e.Reference != null && e.Reference.StartsWith("SUB-"))
                    .Select(e => e.Reference!)
                    .ToHashSet();

                var subEntries = await _context.CompanySubscriptions
                    .Where(s => s.Status == "Active" || s.Status == "Trial" || s.Status == "Cancelled")
                    .OrderByDescending(s => s.StartDate)
                    .Select(s => new
                    {
                        s.SubscriptionId, CompanyName = s.Company != null ? s.Company.CompanyName : "N/A",
                        s.PlanName, s.BillingCycle, s.MonthlyFee, s.StartDate, s.Notes, s.CreatedAt
                    }).ToListAsync();

                var systemEntries = subEntries
                    .Where(s => !existingSubRefsSet.Contains($"SUB-{s.SubscriptionId}"))
                    .Select(s => (object)new
                    {
                        EntryId = -(s.SubscriptionId),
                        EntryNumber = $"SYS-SUB-{s.SubscriptionId:D5}",
                        EntryDate = s.StartDate,
                        Description = $"Subscription: {s.CompanyName} - {s.PlanName} ({s.BillingCycle})",
                        Reference = (string?)$"SUB-{s.SubscriptionId}",
                        Status = "Posted",
                        TotalDebit = s.MonthlyFee,
                        TotalCredit = s.MonthlyFee,
                        Notes = (string?)s.Notes,
                        PostedAt = (DateTime?)s.StartDate,
                        s.CreatedAt,
                        Source = "System",
                        SourceType = "Subscription"
                    }).ToList();

                var all = manualEntries.Cast<object>()
                    .Concat(systemEntries)
                    .ToList();

                return Ok(all);
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message, inner = ex.InnerException?.Message }); }
        }

        [HttpGet("journal-entries/{id}")]
        public async Task<IActionResult> GetJournalEntryById(int id)
        {
            if (!IsSuperAdmin()) return Unauthorized();

            if (id < 0)
            {
                // System-derived entry
                var subId = -id;
                var sub = await _context.CompanySubscriptions
                    .Include(s => s.Company)
                    .FirstOrDefaultAsync(s => s.SubscriptionId == subId);
                if (sub == null) return NotFound(new { message = "System entry not found" });

                var map = await GetPlatformAccountCodeMap();
                int Acct(string code) => map.TryGetValue(code, out var id) ? id : 0;

                return Ok(new
                {
                    EntryId = id,
                    EntryNumber = $"SYS-SUB-{subId:D5}",
                    EntryDate = sub.StartDate,
                    Description = $"Subscription: {sub.Company?.CompanyName ?? "N/A"} - {sub.PlanName}",
                    Reference = $"SUB-{subId}",
                    Status = "Posted",
                    TotalDebit = sub.MonthlyFee,
                    TotalCredit = sub.MonthlyFee,
                    Notes = sub.Notes ?? "",
                    PostedAt = sub.StartDate,
                    sub.CreatedAt,
                    Source = "System",
                    SourceType = "Subscription",
                    Lines = new[]
                    {
                        new { LineId = 0, AccountId = Acct("1100"), AccountCode = "1100", AccountName = "Accounts Receivable", Description = $"{sub.Company?.CompanyName} - {sub.PlanName}", DebitAmount = sub.MonthlyFee, CreditAmount = 0m },
                        new { LineId = 0, AccountId = Acct("4000"), AccountCode = "4000", AccountName = "Subscription Revenue", Description = $"{sub.PlanName} {sub.BillingCycle}", DebitAmount = 0m, CreditAmount = sub.MonthlyFee }
                    }
                });
            }

            var entry = await _context.JournalEntries
                .Include(e => e.Lines).ThenInclude(l => l.Account)
                .FirstOrDefaultAsync(e => e.EntryId == id && e.CompanyId == null);
            if (entry == null) return NotFound(new { message = "Journal entry not found" });

            return Ok(new
            {
                entry.EntryId, entry.EntryNumber, entry.EntryDate, entry.Description,
                entry.Reference, entry.Status, entry.TotalDebit, entry.TotalCredit,
                entry.Notes, entry.PostedAt, entry.CreatedAt,
                Source = (entry.Reference != null && (entry.Reference.StartsWith("SUB-") || entry.Reference.StartsWith("TAX-"))) ? "System" : "Manual",
                SourceType = entry.Reference != null && entry.Reference.StartsWith("SUB-") ? "Subscription"
                           : entry.Reference != null && entry.Reference.StartsWith("TAX-") ? "Tax"
                           : "Journal Entry",
                Lines = entry.Lines.Select(l => new { l.LineId, l.AccountId, l.Account.AccountCode, l.Account.AccountName, l.Description, l.DebitAmount, l.CreditAmount })
            });
        }

        [HttpPost("journal-entries")]
        public async Task<IActionResult> CreateJournalEntry([FromBody] JournalEntry entry)
        {
            if (!IsSuperAdmin()) return Unauthorized();
            if (string.IsNullOrWhiteSpace(entry.Description))
                return BadRequest(new { message = "Description is required" });
            if (entry.Lines == null || entry.Lines.Count < 2)
                return BadRequest(new { message = "At least 2 line items required" });

            var totalDr = entry.Lines.Sum(l => l.DebitAmount);
            var totalCr = entry.Lines.Sum(l => l.CreditAmount);
            if (Math.Abs(totalDr - totalCr) >= 0.01m)
                return BadRequest(new { message = "Debits must equal credits" });

            var count = await _context.JournalEntries.CountAsync(e => e.CompanyId == null) + 1;
            entry.CompanyId = null;
            entry.EntryNumber = $"SA-JE-{count:D5}";
            entry.TotalDebit = totalDr;
            entry.TotalCredit = totalCr;
            entry.Status = "Draft";
            entry.CreatedAt = DateTime.UtcNow;
            entry.UpdatedAt = DateTime.UtcNow;

            _context.JournalEntries.Add(entry);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Journal entry created", data = new { entry.EntryId, entry.EntryNumber } });
        }

        [HttpPut("journal-entries/{id}")]
        public async Task<IActionResult> UpdateJournalEntry(int id, [FromBody] JournalEntry entry)
        {
            if (!IsSuperAdmin()) return Unauthorized();
            var existing = await _context.JournalEntries.Include(e => e.Lines)
                .FirstOrDefaultAsync(e => e.EntryId == id && e.CompanyId == null);
            if (existing == null) return NotFound(new { message = "Entry not found" });
            if (existing.Status != "Draft") return BadRequest(new { message = "Only draft entries can be edited" });

            existing.EntryDate = entry.EntryDate;
            existing.Description = entry.Description;
            existing.Reference = entry.Reference;
            existing.Notes = entry.Notes;

            _context.JournalEntryLines.RemoveRange(existing.Lines);
            if (entry.Lines != null)
            {
                foreach (var line in entry.Lines) { line.EntryId = id; _context.JournalEntryLines.Add(line); }
            }

            existing.TotalDebit = entry.Lines?.Sum(l => l.DebitAmount) ?? 0;
            existing.TotalCredit = entry.Lines?.Sum(l => l.CreditAmount) ?? 0;
            existing.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Entry updated" });
        }

        [HttpPut("journal-entries/{id}/post")]
        public async Task<IActionResult> PostJournalEntry(int id)
        {
            if (!IsSuperAdmin()) return Unauthorized();
            var entry = await _context.JournalEntries.Include(e => e.Lines)
                .FirstOrDefaultAsync(e => e.EntryId == id && e.CompanyId == null);
            if (entry == null) return NotFound(new { message = "Entry not found" });
            if (entry.Status != "Draft") return BadRequest(new { message = "Only draft entries can be posted" });
            if (Math.Abs(entry.TotalDebit - entry.TotalCredit) >= 0.01m)
                return BadRequest(new { message = "Entry is not balanced" });

            entry.Status = "Posted";
            entry.PostedAt = DateTime.UtcNow;

            foreach (var line in entry.Lines)
            {
                _context.GeneralLedger.Add(new GeneralLedgerEntry
                {
                    CompanyId = null, AccountId = line.AccountId, EntryId = entry.EntryId,
                    TransactionDate = entry.EntryDate, Description = line.Description ?? entry.Description,
                    DebitAmount = line.DebitAmount, CreditAmount = line.CreditAmount,
                    Reference = entry.Reference, CreatedAt = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Entry posted to General Ledger" });
        }

        [HttpPut("journal-entries/{id}/void")]
        public async Task<IActionResult> VoidJournalEntry(int id)
        {
            if (!IsSuperAdmin()) return Unauthorized();
            var entry = await _context.JournalEntries.FirstOrDefaultAsync(e => e.EntryId == id && e.CompanyId == null);
            if (entry == null) return NotFound(new { message = "Entry not found" });
            if (entry.Status != "Posted") return BadRequest(new { message = "Only posted entries can be voided" });

            entry.Status = "Void";
            entry.UpdatedAt = DateTime.UtcNow;

            var glEntries = await _context.GeneralLedger.Where(g => g.EntryId == id && g.CompanyId == null).ToListAsync();
            _context.GeneralLedger.RemoveRange(glEntries);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Entry voided" });
        }

        [HttpPut("journal-entries/{id}/archive")]
        public async Task<IActionResult> ArchiveJournalEntry(int id)
        {
            if (!IsSuperAdmin()) return Unauthorized();
            var entry = await _context.JournalEntries.FirstOrDefaultAsync(e => e.EntryId == id && e.CompanyId == null);
            if (entry == null) return NotFound(new { message = "Entry not found" });
            entry.IsArchived = true;
            entry.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Entry archived" });
        }

        #endregion

        #region Platform Accounting - General Ledger

        [HttpGet("general-ledger")]
        public async Task<IActionResult> GetGeneralLedger([FromQuery] int? accountId, [FromQuery] string? dateFrom, [FromQuery] string? dateTo)
        {
            if (!IsSuperAdmin()) return Unauthorized();
            try
            {
                // Manual GL entries
                var query = _context.GeneralLedger
                    .Include(g => g.Account)
                    .Include(g => g.JournalEntry)
                    .Where(g => g.CompanyId == null && !g.IsArchived);

                if (accountId.HasValue) query = query.Where(g => g.AccountId == accountId.Value);
                if (!string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out var df)) query = query.Where(g => g.TransactionDate >= df);
                if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out var dt)) query = query.Where(g => g.TransactionDate <= dt.AddDays(1));

                var manualGL = await query.OrderByDescending(g => g.TransactionDate)
                    .Select(g => new
                    {
                        g.LedgerId, g.AccountId,
                        g.Account.AccountCode,
                        g.Account.AccountName,
                        g.Account.AccountType, g.TransactionDate, g.Description,
                        g.DebitAmount, g.CreditAmount, g.RunningBalance, g.Reference,
                        EntryNumber = g.JournalEntry != null ? g.JournalEntry.EntryNumber : "",
                        Source = (g.Reference != null && (g.Reference.StartsWith("SUB-") || g.Reference.StartsWith("TAX-SUB-"))) ? "System" : "Manual",
                        SourceType = g.Reference != null && g.Reference.StartsWith("TAX-SUB-") ? "Tax"
                                   : g.Reference != null && g.Reference.StartsWith("SUB-") ? "Subscription"
                                   : "Journal Entry"
                    }).ToListAsync();

                // System-derived GL from subscriptions - use dedup from manualGL refs
                var existingGLRefs = manualGL
                    .Where(g => g.Reference != null && g.Reference.StartsWith("SUB-"))
                    .Select(g => g.Reference!)
                    .ToHashSet();

                // Build subscription filter query with date/account constraints
                var subsQuery = _context.CompanySubscriptions
                    .Where(s => s.Status == "Active" || s.Status == "Trial" || s.Status == "Cancelled");

                if (!string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out var sdf2))
                    subsQuery = subsQuery.Where(s => s.StartDate >= sdf2);
                if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out var sdt2))
                    subsQuery = subsQuery.Where(s => s.StartDate <= sdt2.AddDays(1));

                var subData = await subsQuery
                    .Select(s => new { s.SubscriptionId, CompanyName = s.Company != null ? s.Company.CompanyName : "N/A", s.PlanName, s.MonthlyFee, s.StartDate })
                    .ToListAsync();

                var map = await GetPlatformAccountCodeMap();
                int Acct(string code) => map.TryGetValue(code, out var id) ? id : 0;
                var arId = Acct("1100"); var revId = Acct("4000");

                var subGL = new List<object>();
                foreach (var s in subData.Where(s => !existingGLRefs.Contains($"SUB-{s.SubscriptionId}")))
                {
                    if (accountId.HasValue && accountId.Value != arId && accountId.Value != revId) continue;
                    if (!accountId.HasValue || accountId.Value == arId)
                        subGL.Add(new { LedgerId = -(s.SubscriptionId), AccountId = arId, AccountCode = "1100", AccountName = "Accounts Receivable", AccountType = "Asset", TransactionDate = s.StartDate, Description = $"{s.CompanyName} - {s.PlanName}", DebitAmount = s.MonthlyFee, CreditAmount = 0m, RunningBalance = 0m, Reference = (string?)$"SUB-{s.SubscriptionId}", EntryNumber = $"SYS-SUB-{s.SubscriptionId:D5}", Source = "System", SourceType = "Subscription" });
                    if (!accountId.HasValue || accountId.Value == revId)
                        subGL.Add(new { LedgerId = -(100000 + s.SubscriptionId), AccountId = revId, AccountCode = "4000", AccountName = "Subscription Revenue", AccountType = "Revenue", TransactionDate = s.StartDate, Description = $"{s.CompanyName} - {s.PlanName} Revenue", DebitAmount = 0m, CreditAmount = s.MonthlyFee, RunningBalance = 0m, Reference = (string?)$"SUB-{s.SubscriptionId}", EntryNumber = $"SYS-SUB-{s.SubscriptionId:D5}", Source = "System", SourceType = "Subscription" });
                }

                var allGL = manualGL.Cast<object>().Concat(subGL).ToList();

                // Lightweight trial balance summary using GL aggregation only (skip heavy ComputePlatformBalances)
                var glSummary = await _context.GeneralLedger
                    .Where(g => g.CompanyId == null && !g.IsArchived)
                    .GroupBy(g => g.AccountId)
                    .Select(g => new { AccountId = g.Key, Dr = g.Sum(x => x.DebitAmount), Cr = g.Sum(x => x.CreditAmount) })
                    .ToListAsync();

                var summaryAccounts = await _context.ChartOfAccounts
                    .Where(a => a.CompanyId == null && !a.IsArchived && a.IsActive)
                    .OrderBy(a => a.AccountCode)
                    .Select(a => new { a.AccountId, a.AccountCode, a.AccountName, a.AccountType })
                    .ToListAsync();

                var glSumMap = glSummary.ToDictionary(g => g.AccountId, g => (g.Dr, g.Cr));
                var summary = summaryAccounts
                    .Where(a => glSumMap.TryGetValue(a.AccountId, out var glBal) && (glBal.Dr != 0 || glBal.Cr != 0))
                    .Select(a => { var b = glSumMap[a.AccountId]; return new { a.AccountId, a.AccountCode, a.AccountName, a.AccountType, TotalDebit = b.Dr, TotalCredit = b.Cr }; })
                    .ToList();

                return Ok(new { data = allGL, summary });
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message, inner = ex.InnerException?.Message }); }
        }

        [HttpPut("general-ledger/{id}/archive")]
        public async Task<IActionResult> ArchiveGeneralLedgerEntry(int id)
        {
            if (!IsSuperAdmin()) return Unauthorized();
            if (id < 0) return BadRequest(new { message = "System entries cannot be archived" });
            var entry = await _context.GeneralLedger.FirstOrDefaultAsync(g => g.LedgerId == id && g.CompanyId == null);
            if (entry == null) return NotFound(new { message = "Entry not found" });
            entry.IsArchived = true;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Ledger entry archived" });
        }

        #endregion

        #region Platform Accounting Helpers

        private async Task<Dictionary<string, int>> GetPlatformAccountCodeMap()
        {
            return await _context.ChartOfAccounts
                .Where(a => a.CompanyId == null && !a.IsArchived)
                .ToDictionaryAsync(a => a.AccountCode, a => a.AccountId);
        }

        private async Task<Dictionary<int, (decimal Dr, decimal Cr)>> ComputePlatformBalances()
        {
            var result = new Dictionary<int, (decimal Dr, decimal Cr)>();

            // Run queries sequentially (EF Core DbContext is not thread-safe)
            var glBalances = await _context.GeneralLedger
                .Where(g => g.CompanyId == null && !g.IsArchived)
                .GroupBy(g => g.AccountId)
                .Select(g => new { AccountId = g.Key, Dr = g.Sum(x => x.DebitAmount), Cr = g.Sum(x => x.CreditAmount) })
                .ToListAsync();

            var mapList = await _context.ChartOfAccounts
                .Where(a => a.CompanyId == null && !a.IsArchived)
                .Select(a => new { a.AccountCode, a.AccountId })
                .ToListAsync();

            var existingBalSubRefs = await _context.JournalEntries
                .Where(e => e.CompanyId == null && !e.IsArchived && e.Reference != null && e.Reference.StartsWith("SUB-"))
                .Select(e => e.Reference)
                .ToListAsync();

            foreach (var b in glBalances) result[b.AccountId] = (b.Dr, b.Cr);

            var map = mapList.ToDictionary(a => a.AccountCode, a => a.AccountId);
            var existingBalSubIds = existingBalSubRefs
                .Where(r => r != null)
                .Select(r => { return int.TryParse(r!.Replace("SUB-", ""), out int id) ? id : 0; })
                .Where(id => id > 0)
                .ToHashSet();

            var subTotal = await _context.CompanySubscriptions
                .Where(s => (s.Status == "Active" || s.Status == "Trial" || s.Status == "Cancelled") && !existingBalSubIds.Contains(s.SubscriptionId))
                .SumAsync(s => s.MonthlyFee);

            var taxTotal = await _context.Invoices
                .Where(i => i.Status != "Cancelled" && i.Status != "Void" && i.Status != "Draft" && i.TaxAmount > 0)
                .SumAsync(i => i.TaxAmount);

            if (subTotal > 0)
            {
                if (map.TryGetValue("1100", out var arAcctId))
                {
                    var cur = result.TryGetValue(arAcctId, out var arCur) ? arCur : (0m, 0m);
                    result[arAcctId] = (cur.Item1 + subTotal, cur.Item2);
                }
                if (map.TryGetValue("4000", out var revAcctId))
                {
                    var cur = result.TryGetValue(revAcctId, out var revCur) ? revCur : (0m, 0m);
                    result[revAcctId] = (cur.Item1, cur.Item2 + subTotal);
                }
            }

            if (taxTotal > 0)
            {
                if (map.TryGetValue("1000", out var cashAcctId))
                {
                    var cur = result.TryGetValue(cashAcctId, out var cashCur) ? cashCur : (0m, 0m);
                    result[cashAcctId] = (cur.Item1 + taxTotal, cur.Item2);
                }
                if (map.TryGetValue("2100", out var taxAcctId))
                {
                    var cur = result.TryGetValue(taxAcctId, out var taxCur) ? taxCur : (0m, 0m);
                    result[taxAcctId] = (cur.Item1, cur.Item2 + taxTotal);
                }
            }

            return result;
        }

        #endregion
    }

    // Request models
    public class CompanyModuleAccessRequest
    {
        public int CompanyId { get; set; }
        public int ModuleId { get; set; }
        public bool IsEnabled { get; set; }
    }

    public class BulkModuleAccessRequest
    {
        public int CompanyId { get; set; }
        public List<int> ModuleIds { get; set; } = [];
        public bool IsEnabled { get; set; }
    }
}
