using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using CompuGear.Data;
using Microsoft.Extensions.Caching.Memory;

namespace CompuGear.Controllers
{
    /// <summary>
    /// Inventory Staff Portal Controller
    /// RoleId: 8 - Inventory Staff
    /// Access: Products, stock levels, alerts, suppliers, inventory reports
    /// </summary>
    public class InventoryStaffController : Controller
    {
        private readonly CompuGearDbContext _context;
        private readonly IMemoryCache _cache;

        public InventoryStaffController(CompuGearDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // Role-based authorization check
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);
            
            var roleId = HttpContext.Session.GetInt32("RoleId");
            
            // Allow access for: Super Admin (1), Company Admin (2), Inventory Staff (8)
            if (roleId == null || (roleId != 1 && roleId != 2 && roleId != 8))
            {
                context.Result = RedirectToAction("Login", "Auth");
                return;
            }

            if (roleId == 8)
            {
                var companyId = HttpContext.Session.GetInt32("CompanyId");
                if (!companyId.HasValue || !HasModuleAccess(companyId.Value, roleId.Value, "INVENTORY"))
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
            if (!roleHasExplicitAccess.Any())
                result = true;
            else
                result = roleHasExplicitAccess.Any(r => r.ModuleCode == moduleCode && r.HasAccess);

            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
            return result;
        }

        // Dashboard
        public IActionResult Index()
        {
            ViewData["Title"] = "Inventory Dashboard";
            return View("~/Views/InventoryStaff/Index.cshtml");
        }

        // Products
        public IActionResult Products()
        {
            ViewData["Title"] = "Products";
            return View("~/Views/InventoryStaff/Products.cshtml");
        }

        public IActionResult ProductDetails(int id)
        {
            ViewData["Title"] = "Product Details";
            ViewData["ProductId"] = id;
            return View("~/Views/InventoryStaff/ProductDetails.cshtml");
        }

        // Stock Levels
        public IActionResult Stock()
        {
            ViewData["Title"] = "Stock Levels";
            return View("~/Views/InventoryStaff/Stock.cshtml");
        }

        // Stock Alerts
        public IActionResult Alerts()
        {
            ViewData["Title"] = "Stock Alerts";
            return View("~/Views/InventoryStaff/Alerts.cshtml");
        }

        // Suppliers
        public IActionResult Suppliers()
        {
            ViewData["Title"] = "Suppliers";
            return View("~/Views/InventoryStaff/Suppliers.cshtml");
        }

        public IActionResult SupplierDetails(int id)
        {
            ViewData["Title"] = "Supplier Details";
            ViewData["SupplierId"] = id;
            return View("~/Views/InventoryStaff/SupplierDetails.cshtml");
        }

        // Inventory Reports
        public IActionResult Reports()
        {
            ViewData["Title"] = "Inventory Reports";
            return View("~/Views/InventoryStaff/Reports.cshtml");
        }

        // Stock Adjustment (Requires Admin Approval)
        public IActionResult StockAdjustment()
        {
            ViewData["Title"] = "Stock Adjustment";
            return View("~/Views/InventoryStaff/StockAdjustment.cshtml");
        }

        // Categories (Requires Admin Approval for modifications)
        public IActionResult Categories()
        {
            ViewData["Title"] = "Product Categories";
            return View("~/Views/InventoryStaff/Categories.cshtml");
        }
    }
}
