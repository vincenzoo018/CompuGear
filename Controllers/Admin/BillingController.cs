using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CompuGear.Controllers
{
    /// <summary>
    /// Billing Controller for Admin - Uses Views/Admin/Billing folder
    /// RoleId: 1 - Super Admin, 2 - Company Admin
    /// </summary>
    public class BillingController : Controller
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

        public IActionResult Invoices()
        {
            return View("~/Views/Admin/Billing/Invoices.cshtml");
        }

        public IActionResult Payments()
        {
            return View("~/Views/Admin/Billing/Payments.cshtml");
        }

        public IActionResult Summary()
        {
            return View("~/Views/Admin/Billing/Summary.cshtml");
        }

        public IActionResult Records()
        {
            return View("~/Views/Admin/Billing/Records.cshtml");
        }
    }
}
