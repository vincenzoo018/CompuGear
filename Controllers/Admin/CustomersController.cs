using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using CompuGear.Data;
using CompuGear.Models;
using CompuGear.Services;
using System.ComponentModel.DataAnnotations;

namespace CompuGear.Controllers.Admin
{
    /// <summary>
    /// Customers Controller for Admin - Uses Views/Admin/Customers folder
    /// RoleId: 1 - Super Admin, 2 - Company Admin
    /// </summary>
    [Authorize(Policy = "FirmMember")]
    [AutoValidateAntiforgeryToken]
    public class CustomersController(CompuGearDbContext context, IConfiguration configuration, IAuditService auditService) : Controller
    {
        private readonly CompuGearDbContext _context = context;
        private readonly IConfiguration _configuration = configuration;
        private readonly IAuditService _auditService = auditService;

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
                if (customer == null)
                    return BadRequest(new { success = false, message = "Invalid customer payload." });

                customer.FirstName = customer.FirstName?.Trim() ?? string.Empty;
                customer.LastName = customer.LastName?.Trim() ?? string.Empty;
                customer.Email = customer.Email?.Trim() ?? string.Empty;
                customer.Phone = customer.Phone?.Trim() ?? string.Empty;
                customer.CompanyName = customer.CompanyName?.Trim();
                customer.BillingAddress = customer.BillingAddress?.Trim();
                customer.BillingCity = customer.BillingCity?.Trim();
                customer.BillingState = customer.BillingState?.Trim();
                customer.BillingZipCode = customer.BillingZipCode?.Trim();
                customer.BillingCountry = customer.BillingCountry?.Trim();

                if (string.IsNullOrWhiteSpace(customer.FirstName) || string.IsNullOrWhiteSpace(customer.LastName))
                    return BadRequest(new { success = false, message = "First name and last name are required." });

                if (string.IsNullOrWhiteSpace(customer.Email) || !new EmailAddressAttribute().IsValid(customer.Email))
                    return BadRequest(new { success = false, message = "A valid email address is required." });

                if (!string.IsNullOrEmpty(customer.Phone) && (!customer.Phone.All(char.IsDigit) || customer.Phone.Length != 11))
                    return BadRequest(new { success = false, message = "Phone number must be exactly 11 digits." });

                var companyId = GetCompanyId();
                var normalizedEmail = customer.Email.ToLowerInvariant();

                var duplicateCustomerExists = await _context.Customers.AnyAsync(c =>
                    c.Email.ToLower() == normalizedEmail
                    && (companyId == null || c.CompanyId == companyId));
                if (duplicateCustomerExists)
                    return BadRequest(new { success = false, message = "A customer with this email already exists." });

                var duplicateUserExists = await _context.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail);
                if (duplicateUserExists)
                    return BadRequest(new { success = false, message = "A user account with this email already exists." });

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
                if (customer == null)
                    return BadRequest(new { success = false, message = "Invalid customer payload." });

                var companyId = GetCompanyId();
                var existing = await _context.Customers.FindAsync(id);
                if (existing == null) return NotFound();
                if (companyId != null && existing.CompanyId != null && existing.CompanyId != companyId) return NotFound();

                // Assign CompanyId if not set (legacy data migration)
                if (existing.CompanyId == null && companyId != null)
                    existing.CompanyId = companyId;

                var firstName = customer.FirstName?.Trim() ?? string.Empty;
                var lastName = customer.LastName?.Trim() ?? string.Empty;
                var email = customer.Email?.Trim() ?? string.Empty;
                var phone = customer.Phone?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                    return BadRequest(new { success = false, message = "First name and last name are required." });

                if (string.IsNullOrWhiteSpace(email) || !new EmailAddressAttribute().IsValid(email))
                    return BadRequest(new { success = false, message = "A valid email address is required." });

                if (!string.IsNullOrEmpty(phone) && (!phone.All(char.IsDigit) || phone.Length != 11))
                    return BadRequest(new { success = false, message = "Phone number must be exactly 11 digits." });

                var normalizedEmail = email.ToLowerInvariant();
                var duplicateCustomerExists = await _context.Customers.AnyAsync(c =>
                    c.CustomerId != id
                    && c.Email.ToLower() == normalizedEmail
                    && (companyId == null || c.CompanyId == companyId));
                if (duplicateCustomerExists)
                    return BadRequest(new { success = false, message = "A customer with this email already exists." });

                var duplicateUserExists = await _context.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail);
                if (duplicateUserExists && !string.Equals(existing.Email, email, StringComparison.OrdinalIgnoreCase))
                    return BadRequest(new { success = false, message = "A user account with this email already exists." });

                existing.FirstName = firstName;
                existing.LastName = lastName;
                existing.Email = email;
                existing.Phone = phone;
                existing.CategoryId = customer.CategoryId;
                existing.BillingAddress = customer.BillingAddress?.Trim();
                existing.BillingCity = customer.BillingCity?.Trim();
                existing.BillingState = customer.BillingState?.Trim();
                existing.BillingZipCode = customer.BillingZipCode?.Trim();
                existing.BillingCountry = customer.BillingCountry?.Trim();
                existing.CompanyName = customer.CompanyName?.Trim();
                existing.CreditLimit = customer.CreditLimit;
                existing.Notes = customer.Notes;
                existing.UpdatedAt = DateTime.UtcNow;

                // Update status if provided (Active/Inactive)
                if (!string.IsNullOrEmpty(customer.Status))
                    existing.Status = string.Equals(customer.Status.Trim(), "Inactive", StringComparison.OrdinalIgnoreCase)
                        ? "Inactive"
                        : "Active";

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
