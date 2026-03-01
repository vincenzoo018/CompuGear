using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CompuGear.Data;
using CompuGear.Models;

namespace CompuGear.Controllers
{
    /// <summary>
    /// Super Admin API Controller - REST API for Super Admin operations
    /// Only accessible by Super Admin (RoleId = 1)
    /// </summary>
    [Route("api/superadmin")]
    [ApiController]
    public class SuperAdminApiController : ControllerBase
    {
        private readonly CompuGearDbContext _context;

        public SuperAdminApiController(CompuGearDbContext context)
        {
            _context = context;
        }

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

            var totalCompanies = await _context.Companies.CountAsync();
            var activeCompanies = await _context.Companies.CountAsync(c => c.IsActive);
            var totalUsers = await _context.Users.CountAsync();
            var activeUsers = await _context.Users.CountAsync(u => u.IsActive);
            var totalSubscriptions = await _context.CompanySubscriptions.CountAsync();
            var activeSubscriptions = await _context.CompanySubscriptions.CountAsync(s => s.Status == "Active");
            var totalModules = await _context.ERPModules.CountAsync(m => m.IsActive);
            var totalRevenue = await _context.CompanySubscriptions
                .Where(s => s.Status == "Active")
                .SumAsync(s => s.MonthlyFee);

            return Ok(new
            {
                totalCompanies,
                activeCompanies,
                totalUsers,
                activeUsers,
                totalSubscriptions,
                activeSubscriptions,
                totalModules,
                monthlyRevenue = totalRevenue
            });
        }

        #endregion

        #region Companies CRUD

        [HttpGet("companies")]
        public async Task<IActionResult> GetCompanies()
        {
            if (!IsSuperAdmin()) return Unauthorized();

            var companies = await _context.Companies
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
                    ModuleName = a.Module.ModuleName,
                    ModuleCode = a.Module.ModuleCode,
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
                    CompanyName = s.Company.CompanyName,
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
                    AccessId = access?.AccessId,
                    ActivatedAt = access?.ActivatedAt
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
                .Include(u => u.Role)
                .Include(u => u.Company)
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
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(user.Password + salt)));
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
                    System.Security.Cryptography.SHA256.HashData(
                        System.Text.Encoding.UTF8.GetBytes(user.Password + salt)));
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

            var companies = await _context.Companies.CountAsync();
            var users = await _context.Users.CountAsync();
            var orders = await _context.Orders.CountAsync();
            var revenue = await _context.Orders.SumAsync(o => o.TotalAmount);
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

            return Ok(new
            {
                companies,
                users,
                orders,
                revenue,
                tickets,
                products,
                subscriptionsByPlan,
                usersByRole
            });
        }

        #endregion

        #region System Settings

        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings()
        {
            if (!IsSuperAdmin()) return Unauthorized();

            var settings = await _context.SystemSettings.ToListAsync();
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
            var roles = await _context.Roles.OrderBy(r => r.RoleId).ToListAsync();
            return Ok(roles);
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
                    var bal = balances.ContainsKey(a.AccountId) ? balances[a.AccountId] : (0m, 0m);
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
            catch (Exception) { return Ok(new List<object>()); }
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
                // Manual entries
                var manualEntries = await _context.JournalEntries
                    .Include(e => e.Lines).ThenInclude(l => l.Account)
                    .Where(e => e.CompanyId == null && !e.IsArchived)
                    .OrderByDescending(e => e.EntryDate)
                    .Select(e => new
                    {
                        e.EntryId, e.EntryNumber, e.EntryDate, e.Description, e.Reference,
                        e.Status, e.TotalDebit, e.TotalCredit, e.Notes, e.PostedAt, e.CreatedAt,
                        Source = "Manual", SourceType = "Journal Entry",
                        Lines = e.Lines.Select(l => new { l.LineId, l.AccountId, AccountCode = l.Account.AccountCode, AccountName = l.Account.AccountName, l.Description, l.DebitAmount, l.CreditAmount })
                    }).ToListAsync();

                // System-derived: subscription revenue entries
                var map = await GetPlatformAccountCodeMap();
                int Acct(string code) => map.ContainsKey(code) ? map[code] : 0;

                var subscriptions = await _context.CompanySubscriptions
                    .Include(s => s.Company)
                    .Where(s => s.Status == "Active" || s.Status == "Trial" || s.Status == "Cancelled")
                    .OrderByDescending(s => s.StartDate)
                    .ToListAsync();

                var subEntries = subscriptions.Select(s => new
                {
                    EntryId = -(s.SubscriptionId),
                    EntryNumber = $"SYS-SUB-{s.SubscriptionId:D5}",
                    EntryDate = s.StartDate,
                    Description = $"Subscription: {s.Company?.CompanyName ?? "N/A"} - {s.PlanName} ({s.BillingCycle})",
                    Reference = (string?)$"SUB-{s.SubscriptionId}",
                    Status = "Posted",
                    TotalDebit = s.MonthlyFee,
                    TotalCredit = s.MonthlyFee,
                    Notes = (string?)s.Notes,
                    PostedAt = (DateTime?)s.StartDate,
                    s.CreatedAt,
                    Source = "System",
                    SourceType = "Subscription",
                    Lines = new[]
                    {
                        new { LineId = 0, AccountId = Acct("1100"), AccountCode = "1100", AccountName = "Accounts Receivable", Description = (string?)$"{s.Company?.CompanyName} - {s.PlanName}", DebitAmount = s.MonthlyFee, CreditAmount = 0m },
                        new { LineId = 0, AccountId = Acct("4000"), AccountCode = "4000", AccountName = "Subscription Revenue", Description = (string?)$"{s.PlanName} {s.BillingCycle}", DebitAmount = 0m, CreditAmount = s.MonthlyFee }
                    }.AsEnumerable()
                }).ToList();

                // System-derived: tax entries from company invoices (platform aggregated)
                var taxTotal = await _context.Invoices
                    .Where(i => i.Status != "Cancelled" && i.Status != "Void" && i.Status != "Draft" && i.TaxAmount > 0)
                    .GroupBy(i => 1)
                    .Select(g => new { TotalTax = g.Sum(i => i.TaxAmount), Count = g.Count(), LastDate = g.Max(i => i.InvoiceDate) })
                    .FirstOrDefaultAsync();

                var taxEntries = new List<object>();
                if (taxTotal != null && taxTotal.TotalTax > 0)
                {
                    taxEntries.Add(new
                    {
                        EntryId = -900000,
                        EntryNumber = "SYS-TAX-AGGR",
                        EntryDate = taxTotal.LastDate,
                        Description = $"Platform Tax Collected ({taxTotal.Count} invoices)",
                        Reference = (string?)"TAX-AGGREGATE",
                        Status = "Posted",
                        TotalDebit = taxTotal.TotalTax,
                        TotalCredit = taxTotal.TotalTax,
                        Notes = (string?)"Aggregated tax from all company invoices",
                        PostedAt = (DateTime?)taxTotal.LastDate,
                        CreatedAt = DateTime.UtcNow,
                        Source = "System",
                        SourceType = "Tax",
                        Lines = new[]
                        {
                            new { LineId = 0, AccountId = Acct("1000"), AccountCode = "1000", AccountName = "Platform Cash", Description = (string?)"Tax collected", DebitAmount = taxTotal.TotalTax, CreditAmount = 0m },
                            new { LineId = 0, AccountId = Acct("2100"), AccountCode = "2100", AccountName = "Tax Payable", Description = (string?)"Tax liability", DebitAmount = 0m, CreditAmount = taxTotal.TotalTax }
                        }.AsEnumerable()
                    });
                }

                var all = manualEntries.Cast<object>()
                    .Concat(subEntries.Cast<object>())
                    .Concat(taxEntries)
                    .ToList();

                return Ok(all);
            }
            catch (Exception) { return Ok(new List<object>()); }
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
                int Acct(string code) => map.ContainsKey(code) ? map[code] : 0;

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
                Source = "Manual", SourceType = "Journal Entry",
                Lines = entry.Lines.Select(l => new { l.LineId, l.AccountId, AccountCode = l.Account.AccountCode, AccountName = l.Account.AccountName, l.Description, l.DebitAmount, l.CreditAmount })
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
                        g.LedgerId, g.AccountId, AccountCode = g.Account.AccountCode, AccountName = g.Account.AccountName,
                        AccountType = g.Account.AccountType, g.TransactionDate, g.Description,
                        g.DebitAmount, g.CreditAmount, g.RunningBalance, g.Reference,
                        EntryNumber = g.JournalEntry != null ? g.JournalEntry.EntryNumber : "",
                        Source = "Manual", SourceType = "Journal Entry"
                    }).ToListAsync();

                // System-derived GL lines from subscriptions
                var map = await GetPlatformAccountCodeMap();
                int Acct(string code) => map.ContainsKey(code) ? map[code] : 0;

                var subs = await _context.CompanySubscriptions
                    .Include(s => s.Company)
                    .Where(s => s.Status == "Active" || s.Status == "Trial" || s.Status == "Cancelled")
                    .ToListAsync();

                var subGL = new List<object>();
                foreach (var s in subs)
                {
                    var matchDate = true;
                    if (!string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out var sdf) && s.StartDate < sdf) matchDate = false;
                    if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out var sdt) && s.StartDate > sdt.AddDays(1)) matchDate = false;
                    if (!matchDate) continue;

                    var arId = Acct("1100"); var revId = Acct("4000");
                    if (accountId.HasValue && accountId.Value != arId && accountId.Value != revId) continue;

                    if (!accountId.HasValue || accountId.Value == arId)
                    {
                        subGL.Add(new { LedgerId = -(s.SubscriptionId), AccountId = arId, AccountCode = "1100", AccountName = "Accounts Receivable", AccountType = "Asset", TransactionDate = s.StartDate, Description = $"{s.Company?.CompanyName} - {s.PlanName}", DebitAmount = s.MonthlyFee, CreditAmount = 0m, RunningBalance = 0m, Reference = (string?)$"SUB-{s.SubscriptionId}", EntryNumber = $"SYS-SUB-{s.SubscriptionId:D5}", Source = "System", SourceType = "Subscription" });
                    }
                    if (!accountId.HasValue || accountId.Value == revId)
                    {
                        subGL.Add(new { LedgerId = -(100000 + s.SubscriptionId), AccountId = revId, AccountCode = "4000", AccountName = "Subscription Revenue", AccountType = "Revenue", TransactionDate = s.StartDate, Description = $"{s.Company?.CompanyName} - {s.PlanName} Revenue", DebitAmount = 0m, CreditAmount = s.MonthlyFee, RunningBalance = 0m, Reference = (string?)$"SUB-{s.SubscriptionId}", EntryNumber = $"SYS-SUB-{s.SubscriptionId:D5}", Source = "System", SourceType = "Subscription" });
                    }
                }

                var allGL = manualGL.Cast<object>().Concat(subGL).ToList();

                // Account summary (trial balance) from system balances
                var balances = await ComputePlatformBalances();
                var allAccounts = await _context.ChartOfAccounts
                    .Where(a => a.CompanyId == null && !a.IsArchived && a.IsActive)
                    .OrderBy(a => a.AccountCode)
                    .ToListAsync();

                var summary = allAccounts.Where(a => balances.ContainsKey(a.AccountId) && (balances[a.AccountId].Item1 != 0 || balances[a.AccountId].Item2 != 0)).Select(a =>
                {
                    var b = balances[a.AccountId];
                    return new { a.AccountId, a.AccountCode, a.AccountName, a.AccountType, TotalDebit = b.Item1, TotalCredit = b.Item2 };
                }).ToList();

                return Ok(new { data = allGL, summary });
            }
            catch (Exception) { return Ok(new { data = new List<object>(), summary = new List<object>() }); }
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

            // From manual GL entries
            var glBalances = await _context.GeneralLedger
                .Where(g => g.CompanyId == null && !g.IsArchived)
                .GroupBy(g => g.AccountId)
                .Select(g => new { AccountId = g.Key, Dr = g.Sum(x => x.DebitAmount), Cr = g.Sum(x => x.CreditAmount) })
                .ToListAsync();

            foreach (var b in glBalances) result[b.AccountId] = (b.Dr, b.Cr);

            // From subscriptions
            var map = await GetPlatformAccountCodeMap();
            var subTotal = await _context.CompanySubscriptions
                .Where(s => s.Status == "Active" || s.Status == "Trial" || s.Status == "Cancelled")
                .SumAsync(s => s.MonthlyFee);

            if (subTotal > 0)
            {
                if (map.ContainsKey("1100"))
                {
                    var id = map["1100"];
                    var cur = result.ContainsKey(id) ? result[id] : (0m, 0m);
                    result[id] = (cur.Item1 + subTotal, cur.Item2);
                }
                if (map.ContainsKey("4000"))
                {
                    var id = map["4000"];
                    var cur = result.ContainsKey(id) ? result[id] : (0m, 0m);
                    result[id] = (cur.Item1, cur.Item2 + subTotal);
                }
            }

            // From platform tax (aggregate of all company invoices tax)
            var taxTotal = await _context.Invoices
                .Where(i => i.Status != "Cancelled" && i.Status != "Void" && i.Status != "Draft" && i.TaxAmount > 0)
                .SumAsync(i => i.TaxAmount);

            if (taxTotal > 0)
            {
                if (map.ContainsKey("1000"))
                {
                    var id = map["1000"];
                    var cur = result.ContainsKey(id) ? result[id] : (0m, 0m);
                    result[id] = (cur.Item1 + taxTotal, cur.Item2);
                }
                if (map.ContainsKey("2100"))
                {
                    var id = map["2100"];
                    var cur = result.ContainsKey(id) ? result[id] : (0m, 0m);
                    result[id] = (cur.Item1, cur.Item2 + taxTotal);
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
        public List<int> ModuleIds { get; set; } = new();
        public bool IsEnabled { get; set; }
    }
}
