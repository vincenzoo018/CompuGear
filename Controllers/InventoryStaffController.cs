using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CompuGear.Controllers
{
    /// <summary>
    /// Inventory Staff Portal Controller
    /// RoleId: 8 - Inventory Staff
    /// Access: Products, stock levels, alerts, suppliers, inventory reports
    /// </summary>
    public class InventoryStaffController : Controller
    {
        // Role-based authorization check
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);
            
            var roleId = HttpContext.Session.GetInt32("RoleId");
            
            // Allow access for: Super Admin (1), Company Admin (2), Inventory Staff (8)
            if (roleId == null || (roleId != 1 && roleId != 2 && roleId != 8))
            {
                context.Result = RedirectToAction("Login", "Auth");
            }
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
