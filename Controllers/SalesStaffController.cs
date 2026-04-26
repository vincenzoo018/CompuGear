using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using CompuGear.Data;
using Microsoft.Extensions.Caching.Memory;

namespace CompuGear.Controllers
{
    /// <summary>
    /// Sales Staff Portal Controller
    /// RoleId: 3 - Sales Staff
    /// Access: Customer management, orders, leads, sales reports, product/inventory info (read)
    /// </summary>
    public class SalesStaffController(CompuGearDbContext context, IMemoryCache cache) : Controller
    {
        private readonly CompuGearDbContext _context = context;
        private readonly IMemoryCache _cache = cache;

        // Role-based authorization check
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);
            
            var roleId = HttpContext.Session.GetInt32("RoleId");
            
            // Allow access for: Super Admin (1), Company Admin (2), Sales Staff (3)
            if (roleId == null || (roleId != 1 && roleId != 2 && roleId != 3))
            {
                context.Result = RedirectToAction("Login", "Auth");
                return;
            }

            if (roleId == 3)
            {
                var companyId = HttpContext.Session.GetInt32("CompanyId");
                if (!companyId.HasValue || !HasModuleAccess(companyId.Value, roleId.Value, "SALES"))
                {
                    context.Result = RedirectToAction("Index", "Home");
                }
            }
        }

        private bool HasModuleAccess(int companyId, int roleId, string moduleCode)
        {
            var cacheKey = $"module_access_{companyId}_{roleId}_{moduleCode}";
            if (_cache.TryGetValue(cacheKey, out bool cachedResult))
                return cachedResult;

            var companyHasModule = _context.CompanyModuleAccess
                .Any(a => a.CompanyId == companyId && a.IsEnabled && a.Module.ModuleCode == moduleCode);

            if (!companyHasModule)
            {
                _cache.Set(cacheKey, false, TimeSpan.FromMinutes(5));
                return false;
            }

            var roleHasExplicitAccess = _context.RoleModuleAccess
                .Where(r => r.CompanyId == companyId && r.RoleId == roleId)
                .ToList();

            bool result;
            if (roleHasExplicitAccess.Count == 0)
                result = true;
            else
                result = roleHasExplicitAccess.Any(r => r.ModuleCode == moduleCode && r.HasAccess);

            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
            return result;
        }

        // Dashboard
        public IActionResult Index()
        {
            ViewData["Title"] = "Sales Dashboard";
            return View("~/Views/SalesStaff/Index.cshtml");
        }

        // Customer Management
        public IActionResult Customers()
        {
            ViewData["Title"] = "Customer Management";
            return View("~/Views/SalesStaff/Customers.cshtml");
        }

        public IActionResult CustomerDetails(int id)
        {
            ViewData["Title"] = "Customer Details";
            ViewData["CustomerId"] = id;
            return View("~/Views/SalesStaff/CustomerDetails.cshtml");
        }

        // Sales Orders
        public IActionResult Orders()
        {
            ViewData["Title"] = "Sales Orders";
            return View("~/Views/SalesStaff/Orders.cshtml");
        }

        public IActionResult OrderDetails(int id)
        {
            ViewData["Title"] = "Order Details";
            ViewData["OrderId"] = id;
            return View("~/Views/SalesStaff/OrderDetails.cshtml");
        }

        public IActionResult CreateOrder()
        {
            ViewData["Title"] = "Create Order";
            return View("~/Views/SalesStaff/CreateOrder.cshtml");
        }

        // Leads & Opportunities
        public IActionResult Leads()
        {
            ViewData["Title"] = "Leads & Opportunities";
            return View("~/Views/SalesStaff/Leads.cshtml");
        }

        public IActionResult LeadDetails(int id)
        {
            ViewData["Title"] = "Lead Details";
            ViewData["LeadId"] = id;
            return View("~/Views/SalesStaff/LeadDetails.cshtml");
        }

        // Products & Inventory (Read-only access)
        public IActionResult Products()
        {
            ViewData["Title"] = "Products & Inventory";
            return View("~/Views/SalesStaff/Products.cshtml");
        }

        public IActionResult ProductDetails(int id)
        {
            ViewData["Title"] = "Product Details";
            ViewData["ProductId"] = id;
            return View("~/Views/SalesStaff/ProductDetails.cshtml");
        }

        // Sales Reports
        public IActionResult Reports()
        {
            ViewData["Title"] = "Sales Reports";
            return View("~/Views/SalesStaff/Reports.cshtml");
        }

        // Refunds (Requires Admin Approval)
        public IActionResult Refunds()
        {
            ViewData["Title"] = "Refund Requests";
            return View("~/Views/SalesStaff/Refunds.cshtml");
        }
    }
}
