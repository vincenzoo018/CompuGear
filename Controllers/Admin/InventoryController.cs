using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CompuGear.Controllers
{
    /// <summary>
    /// Inventory Controller for Admin - Uses Views/Admin/Inventory folder
    /// RoleId: 1 - Super Admin, 2 - Company Admin
    /// </summary>
    public class InventoryController : Controller
    {
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

        public IActionResult Products()
        {
            return View("~/Views/Admin/Inventory/Products.cshtml");
        }

        public IActionResult Categories()
        {
            return View("~/Views/Admin/Inventory/Categories.cshtml");
        }

        public IActionResult Stock()
        {
            return View("~/Views/Admin/Inventory/Stock.cshtml");
        }

        public IActionResult Alerts()
        {
            return View("~/Views/Admin/Inventory/Alerts.cshtml");
        }

        public IActionResult Reports()
        {
            return View("~/Views/Admin/Inventory/Reports.cshtml");
        }

        public IActionResult Suppliers()
        {
            return View("~/Views/Admin/Inventory/Suppliers.cshtml");
        }

        public IActionResult PurchaseOrders()
        {
            return View("~/Views/Admin/Inventory/PurchaseOrders.cshtml");
        }

        public IActionResult StockAdjustment()
        {
            return View("~/Views/Admin/Inventory/StockAdjustment.cshtml");
        }
    }
}
