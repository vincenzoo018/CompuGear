using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using CompuGear.Data;
using CompuGear.Models;
using CompuGear.Services;

namespace CompuGear.Controllers
{
    /// <summary>
    /// Customers Controller for Admin - Uses Views/Admin/Customers folder
    /// RoleId: 1 - Super Admin, 2 - Company Admin
    /// </summary>
    public class CustomersController : Controller
    {
        private readonly CompuGearDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IAuditService _auditService;

        public CustomersController(CompuGearDbContext context, IConfiguration configuration, IAuditService auditService)
        {
            _context = context;
            _configuration = configuration;
            _auditService = auditService;
        }

        // Helper: returns CompanyId from session. Super Admin (RoleId=1) gets null → sees all data.
        private int? GetCompanyId()
        {
            var roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId == 1) return null; // Super Admin sees everything
            return HttpContext.Session.GetInt32("CompanyId");
        }

        // Authorization check - Admins for views, all authenticated staff for API
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);
            
            var roleId = HttpContext.Session.GetInt32("RoleId");
            var isApiRequest = HttpContext.Request.Path.StartsWithSegments("/api");
            
            if (roleId == null)
            {
                context.Result = isApiRequest
                    ? new JsonResult(new { success = false, message = "Not authenticated" }) { StatusCode = 401 }
                    : RedirectToAction("Login", "Auth");
                return;
            }
            
            // API endpoints: allow all authenticated staff + admin roles (data is company-scoped)
            if (isApiRequest) return;
            
            // View endpoints: admin only
            if (roleId != 1 && roleId != 2)
            {
                context.Result = RedirectToAction("Login", "Auth");
            }
        }

        #region View Actions

        public IActionResult List()
        {
            return View("~/Views/Admin/Customers/List.cshtml");
        }

        public IActionResult Archive()
        {
            return View("~/Views/Admin/Customers/Archive.cshtml");
        }

        public IActionResult Profile(int? id)
        {
            return View("~/Views/Admin/Customers/Profile.cshtml");
        }

        public IActionResult History(int? id)
        {
            return View("~/Views/Admin/Customers/History.cshtml");
        }

        #endregion

        #region Customer API Endpoints

        [HttpGet]
        [Route("api/customers")]
        public async Task<IActionResult> GetCustomers()
        {
            try
            {
                var companyId = GetCompanyId();
                var customers = await _context.Customers
                    .Include(c => c.Category)
                    .Where(c => companyId == null || c.CompanyId == companyId)
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new
                    {
                        c.CustomerId,
                        c.CustomerCode,
                        c.FirstName,
                        c.LastName,
                        FullName = c.FirstName + " " + c.LastName,
                        c.Email,
                        c.Phone,
                        c.Status,
                        c.TotalOrders,
                        c.TotalSpent,
                        c.LoyaltyPoints,
                        CategoryName = c.Category != null ? c.Category.CategoryName : "Standard",
                        c.CategoryId,
                        c.BillingAddress,
                        c.BillingCity,
                        c.BillingState,
                        c.BillingZipCode,
                        c.BillingCountry,
                        c.CompanyName,
                        c.CreditLimit,
                        c.Notes,
                        c.CreatedAt,
                        c.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(customers);
            }
            catch (Exception)
            {
                return Ok(new List<object>());
            }
        }

        [HttpGet]
        [Route("api/customers/{id}")]
        public async Task<IActionResult> GetCustomer(int id)
        {
            try
            {
                var companyId = GetCompanyId();
                var customer = await _context.Customers
                    .Include(c => c.Category)
                    .FirstOrDefaultAsync(c => c.CustomerId == id && (companyId == null || c.CompanyId == companyId));
                if (customer == null) return NotFound();
                return Ok(customer);
            }
            catch (Exception)
            {
                return NotFound();
            }
        }

        [HttpPost]
        [Route("api/customers")]
        public async Task<IActionResult> CreateCustomer([FromBody] Customer customer)
        {
            try
            {
                var companyId = GetCompanyId();
                customer.CompanyId = companyId;
                customer.CustomerCode = $"CUST-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
                customer.CreatedAt = DateTime.UtcNow;
                customer.UpdatedAt = DateTime.UtcNow;
                var normalizedStatus = customer.Status?.Trim();
                customer.Status = string.Equals(normalizedStatus, "Inactive", StringComparison.OrdinalIgnoreCase)
                    ? "Inactive"
                    : "Active";

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Customer created successfully", data = customer });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPut]
        [Route("api/customers/{id}")]
        public async Task<IActionResult> UpdateCustomer(int id, [FromBody] Customer customer)
        {
            try
            {
                var companyId = GetCompanyId();
                var existing = await _context.Customers.FindAsync(id);
                if (existing == null) return NotFound();
                if (companyId != null && existing.CompanyId != null && existing.CompanyId != companyId) return NotFound();

                // Assign CompanyId if not set (legacy data migration)
                if (existing.CompanyId == null && companyId != null)
                    existing.CompanyId = companyId;

                existing.FirstName = customer.FirstName;
                existing.LastName = customer.LastName;
                existing.Email = customer.Email;
                existing.Phone = customer.Phone;
                existing.CategoryId = customer.CategoryId;
                existing.BillingAddress = customer.BillingAddress;
                existing.BillingCity = customer.BillingCity;
                existing.BillingState = customer.BillingState;
                existing.BillingZipCode = customer.BillingZipCode;
                existing.BillingCountry = customer.BillingCountry;
                existing.CompanyName = customer.CompanyName;
                existing.CreditLimit = customer.CreditLimit;
                existing.Notes = customer.Notes;
                existing.UpdatedAt = DateTime.UtcNow;

                // Update status if provided (Active/Inactive)
                if (!string.IsNullOrEmpty(customer.Status))
                    existing.Status = customer.Status;

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Customer updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpDelete]
        [Route("api/customers/{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var companyId = GetCompanyId();
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound();
            if (companyId != null && customer.CompanyId != null && customer.CompanyId != companyId) return NotFound();

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Customer deleted successfully" });
        }

        [HttpPut]
        [Route("api/customers/{id}/toggle-status")]
        public async Task<IActionResult> ToggleCustomerStatus(int id)
        {
            try
            {
                var companyId = GetCompanyId();
                var customer = await _context.Customers.FindAsync(id);
                if (customer == null) return NotFound();
                if (companyId != null && customer.CompanyId != null && customer.CompanyId != companyId) return NotFound();

                customer.Status = customer.Status == "Active" ? "Inactive" : "Active";
                customer.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = $"Customer {(customer.Status == "Active" ? "activated" : "deactivated")} successfully", status = customer.Status });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [Route("api/customer-categories")]
        public async Task<IActionResult> GetCustomerCategories()
        {
            try
            {
                var categories = await _context.CustomerCategories.ToListAsync();
                return Ok(categories);
            }
            catch (Exception)
            {
                return Ok(new List<object>());
            }
        }

        #endregion
    }
}
