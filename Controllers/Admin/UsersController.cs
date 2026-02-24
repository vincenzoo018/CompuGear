using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using CompuGear.Data;
using CompuGear.Models;
using CompuGear.Services;

namespace CompuGear.Controllers
{
    /// <summary>
    /// Users Controller for Admin - Uses Views/Admin/Users folder
    /// RoleId: 1 - Super Admin, 2 - Company Admin
    /// Includes API endpoints for user management, roles, role access, and activity/audit logs.
    /// </summary>
    public class UsersController : Controller
    {
        private readonly CompuGearDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IAuditService _auditService;

        public UsersController(CompuGearDbContext context, IConfiguration configuration, IAuditService auditService)
        {
            _context = context;
            _configuration = configuration;
            _auditService = auditService;
        }

        // Admin authorization check - only Super Admin (1) and Company Admin (2)
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (roleId == null || (roleId != 1 && roleId != 2))
            {
                context.Result = RedirectToAction("Login", "Auth");
            }
        }

        #region Helper Methods

        private int? GetCompanyId()
        {
            var roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId == 1) return null;
            return HttpContext.Session.GetInt32("CompanyId");
        }

        private int? GetRoleId()
        {
            return HttpContext.Session.GetInt32("RoleId");
        }

        private IQueryable<ActivityLog> GetScopedActivityLogQuery()
        {
            var query = _context.ActivityLogs.AsQueryable();
            var companyId = GetCompanyId();
            if (!companyId.HasValue) return query;
            var staffUserIds = _context.Users.Where(u => u.CompanyId == companyId).Select(u => u.UserId);
            var customerLinkedUserIds = _context.Customers.Where(c => c.CompanyId == companyId && c.UserId.HasValue).Select(c => c.UserId!.Value);
            var companyUserIds = staffUserIds.Union(customerLinkedUserIds);
            return query.Where(a => a.UserId.HasValue && companyUserIds.Contains(a.UserId.Value));
        }

        private IQueryable<ActivityLog> ApplyUserTypeFilter(IQueryable<ActivityLog> query, string? userType)
        {
            if (string.IsNullOrWhiteSpace(userType)) return query;
            var normalized = userType.Trim().ToLowerInvariant();
            if (normalized == "system") return query.Where(a => !a.UserId.HasValue);
            if (normalized == "customer") return query.Where(a => a.UserId.HasValue && _context.Users.Any(u => u.UserId == a.UserId.Value && u.RoleId == 7));
            if (normalized == "staff") return query.Where(a => a.UserId.HasValue && _context.Users.Any(u => u.UserId == a.UserId.Value && u.RoleId != 7));
            return query;
        }

        #endregion

        #region View Actions

        public IActionResult Accounts()
        {
            return View("~/Views/Admin/Users/Accounts.cshtml");
        }

        public IActionResult Archive()
        {
            return View("~/Views/Admin/Users/Archive.cshtml");
        }

        public IActionResult Roles()
        {
            return View("~/Views/Admin/Users/Roles.cshtml");
        }

        public IActionResult Activity()
        {
            return View("~/Views/Admin/Users/Activity.cshtml");
        }

        #endregion

        #region Users CRUD API

        [HttpGet]
        [Route("api/users")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var companyId = GetCompanyId();
                var users = await _context.Users
                    .Include(u => u.Role)
                    .Where(u => (companyId == null || u.CompanyId == companyId) && (companyId == null || u.RoleId != 1))
                    .OrderByDescending(u => u.CreatedAt)
                    .Select(u => new
                    {
                        u.UserId, u.Username, u.Email, u.FirstName, u.LastName,
                        FullName = u.FirstName + " " + u.LastName,
                        u.Phone, u.IsActive,
                        RoleName = u.Role != null ? u.Role.RoleName : "Staff",
                        u.RoleId, u.LastLoginAt, u.CreatedAt
                    })
                    .ToListAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpGet]
        [Route("api/users/{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var companyId = GetCompanyId();
            var user = await _context.Users.Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == id && (companyId == null || u.CompanyId == companyId));
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpPost]
        [Route("api/users")]
        public async Task<IActionResult> CreateUser([FromBody] User user)
        {
            try
            {
                if (await _context.Users.AnyAsync(u => u.Username == user.Username))
                    return BadRequest(new { success = false, message = "Username already exists" });
                if (await _context.Users.AnyAsync(u => u.Email == user.Email))
                    return BadRequest(new { success = false, message = "Email already exists" });
                if (!string.IsNullOrEmpty(user.Password))
                {
                    user.Salt = Guid.NewGuid().ToString("N").Substring(0, 16);
                    user.PasswordHash = Convert.ToBase64String(
                        System.Security.Cryptography.SHA256.HashData(
                            System.Text.Encoding.UTF8.GetBytes(user.Password + user.Salt)));
                }
                else
                {
                    return BadRequest(new { success = false, message = "Password is required" });
                }
                var companyId = GetCompanyId();
                user.CompanyId = companyId;
                if (user.RoleId == 0) user.RoleId = 3;
                user.CreatedAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "User created successfully", data = new { user.UserId, user.Username, user.Email, user.FullName } });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPut]
        [Route("api/users/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] User user)
        {
            try
            {
                var companyId = GetCompanyId();
                var existing = await _context.Users.FindAsync(id);
                if (existing == null) return NotFound();
                if (companyId != null && existing.CompanyId != null && existing.CompanyId != companyId) return NotFound();
                if (existing.CompanyId == null && companyId != null) existing.CompanyId = companyId;
                existing.FirstName = user.FirstName;
                existing.LastName = user.LastName;
                existing.Email = user.Email;
                existing.Phone = user.Phone;
                existing.RoleId = user.RoleId;
                existing.IsActive = user.IsActive;
                existing.UpdatedAt = DateTime.UtcNow;
                if (!string.IsNullOrEmpty(user.Password))
                {
                    existing.Salt = Guid.NewGuid().ToString("N").Substring(0, 16);
                    existing.PasswordHash = Convert.ToBase64String(
                        System.Security.Cryptography.SHA256.HashData(
                            System.Text.Encoding.UTF8.GetBytes(user.Password + existing.Salt)));
                    existing.PasswordChangedAt = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "User updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPut]
        [Route("api/users/{id}/toggle-status")]
        public async Task<IActionResult> ToggleUserStatus(int id)
        {
            try
            {
                var companyId = GetCompanyId();
                var user = await _context.Users.FindAsync(id);
                if (user == null) return NotFound();
                if (companyId != null && user.CompanyId != null && user.CompanyId != companyId) return NotFound();
                user.IsActive = !user.IsActive;
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = $"User {(user.IsActive ? "activated" : "deactivated")} successfully", isActive = user.IsActive });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpDelete]
        [Route("api/users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var companyId = GetCompanyId();
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            if (companyId != null && user.CompanyId != null && user.CompanyId != companyId) return NotFound();
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "User deleted successfully" });
        }

        #endregion

        #region Roles API

        [HttpGet]
        [Route("api/roles")]
        public async Task<IActionResult> GetRoles()
        {
            try
            {
                var roles = await _context.Roles.ToListAsync();
                if (!roles.Any())
                {
                    return Ok(new[]
                    {
                        new { roleId = 1, roleName = "Super Admin", isActive = true },
                        new { roleId = 2, roleName = "Company Admin", isActive = true },
                        new { roleId = 3, roleName = "Sales Staff", isActive = true },
                        new { roleId = 4, roleName = "Customer Support Staff", isActive = true },
                        new { roleId = 5, roleName = "Marketing Staff", isActive = true },
                        new { roleId = 6, roleName = "Accounting & Billing Staff", isActive = true }
                    });
                }
                return Ok(roles);
            }
            catch (Exception)
            {
                return Ok(new[]
                {
                    new { roleId = 1, roleName = "Super Admin", isActive = true },
                    new { roleId = 2, roleName = "Company Admin", isActive = true },
                    new { roleId = 3, roleName = "Sales Staff", isActive = true },
                    new { roleId = 4, roleName = "Customer Support Staff", isActive = true },
                    new { roleId = 5, roleName = "Marketing Staff", isActive = true },
                    new { roleId = 6, roleName = "Accounting & Billing Staff", isActive = true }
                });
            }
        }

        #endregion

        #region Role Access API

        [HttpGet]
        [Route("api/role-access")]
        public async Task<IActionResult> GetRoleAccess()
        {
            try
            {
                var companyId = GetCompanyId();
                if (companyId == null)
                {
                    var qCompanyId = HttpContext.Request.Query["companyId"].FirstOrDefault();
                    if (!string.IsNullOrEmpty(qCompanyId) && int.TryParse(qCompanyId, out var cid))
                        companyId = cid;
                    else
                        return BadRequest(new { success = false, message = "CompanyId is required" });
                }
                var access = await _context.RoleModuleAccess
                    .Where(r => r.CompanyId == companyId)
                    .Include(r => r.Role)
                    .Select(r => new { r.Id, r.CompanyId, r.RoleId, RoleName = r.Role != null ? r.Role.RoleName : "", r.ModuleCode, r.HasAccess })
                    .ToListAsync();
                var roles = await _context.Roles.Where(r => r.RoleId != 1 && r.RoleId != 7).OrderBy(r => r.RoleId)
                    .Select(r => new { r.RoleId, r.RoleName }).ToListAsync();
                var modules = new[]
                {
                    new { Code = "SALES", Name = "Sales" },
                    new { Code = "CUSTOMERS", Name = "Customers" },
                    new { Code = "INVENTORY", Name = "Inventory" },
                    new { Code = "BILLING", Name = "Billing" },
                    new { Code = "MARKETING", Name = "Marketing" },
                    new { Code = "SUPPORT", Name = "Support" }
                };
                return Ok(new { success = true, data = access, roles, modules });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route("api/role-access")]
        public async Task<IActionResult> SaveRoleAccess([FromBody] RoleAccessSaveRequest request)
        {
            try
            {
                var companyId = GetCompanyId();
                if (companyId == null)
                {
                    if (request.CompanyId.HasValue) companyId = request.CompanyId.Value;
                    else return BadRequest(new { success = false, message = "CompanyId is required" });
                }
                var existing = await _context.RoleModuleAccess.Where(r => r.CompanyId == companyId).ToListAsync();
                _context.RoleModuleAccess.RemoveRange(existing);
                foreach (var item in request.AccessList)
                {
                    _context.RoleModuleAccess.Add(new RoleModuleAccess
                    {
                        CompanyId = companyId.Value,
                        RoleId = item.RoleId,
                        ModuleCode = item.ModuleCode,
                        HasAccess = item.HasAccess,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Role access settings saved successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        #endregion

        #region Activity / Audit Logs API

        [HttpGet]
        [Route("api/activity-logs")]
        public async Task<IActionResult> GetActivityLogs([FromQuery] string? module, [FromQuery] string? userType, [FromQuery] int? limit)
        {
            var query = GetScopedActivityLogQuery();
            if (!string.IsNullOrEmpty(module)) query = query.Where(a => a.Module == module);
            query = ApplyUserTypeFilter(query, userType);
            var logs = await query.OrderByDescending(a => a.CreatedAt).Take(limit ?? 50)
                .Select(a => new
                {
                    a.LogId, a.UserId, a.UserName, a.Action, a.Module, a.EntityType, a.EntityId, a.Description,
                    UserType = !a.UserId.HasValue ? "System" : (_context.Users.Where(u => u.UserId == a.UserId).Select(u => u.RoleId).FirstOrDefault() == 7 ? "Customer" : "Staff"),
                    a.CreatedAt
                }).ToListAsync();
            return Ok(new { success = true, data = logs });
        }

        [HttpGet]
        [Route("api/audit-logs")]
        public async Task<IActionResult> GetAuditLogs([FromQuery] string? module, [FromQuery] string? action, [FromQuery] string? userType, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var query = GetScopedActivityLogQuery();
                if (!string.IsNullOrEmpty(module)) query = query.Where(a => a.Module == module);
                if (!string.IsNullOrEmpty(action)) query = query.Where(a => a.Action == action);
                query = ApplyUserTypeFilter(query, userType);
                if (startDate.HasValue) query = query.Where(a => a.CreatedAt >= startDate.Value);
                if (endDate.HasValue) query = query.Where(a => a.CreatedAt <= endDate.Value.AddDays(1));
                var totalCount = await query.CountAsync();
                var logs = await query.OrderByDescending(a => a.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
                    .Select(a => new
                    {
                        a.LogId, a.UserId, a.UserName, a.Action, a.Module, a.EntityType, a.EntityId, a.Description,
                        UserType = !a.UserId.HasValue ? "System" : (_context.Users.Where(u => u.UserId == a.UserId).Select(u => u.RoleId).FirstOrDefault() == 7 ? "Customer" : "Staff"),
                        a.IPAddress, a.CreatedAt
                    }).ToListAsync();
                return Ok(new { success = true, data = logs, totalCount, page, pageSize, totalPages = (int)Math.Ceiling(totalCount / (double)pageSize) });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [Route("api/audit-logs/modules")]
        public async Task<IActionResult> GetAuditModules()
        {
            var modules = await GetScopedActivityLogQuery().Select(a => a.Module).Distinct().OrderBy(m => m).ToListAsync();
            return Ok(modules);
        }

        [HttpGet]
        [Route("api/audit-logs/actions")]
        public async Task<IActionResult> GetAuditActions()
        {
            var actions = await GetScopedActivityLogQuery().Select(a => a.Action).Distinct().OrderBy(a => a).ToListAsync();
            return Ok(actions);
        }

        #endregion
    }
}
