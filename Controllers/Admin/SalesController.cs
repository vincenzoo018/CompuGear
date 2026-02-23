using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CompuGear.Controllers
{
    /// <summary>
    /// Sales Controller for Admin - Uses Views/Admin/Sales folder
    /// RoleId: 1 - Super Admin, 2 - Company Admin
    /// </summary>
    public class SalesController : Controller
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

        public IActionResult Orders()
        {
            return View("~/Views/Admin/Sales/Orders.cshtml");
        }

        public IActionResult Leads()
        {
            return View("~/Views/Admin/Sales/Leads.cshtml");
        }

        public IActionResult LeadsArchive()
        {
            return View("~/Views/Admin/Sales/LeadsArchive.cshtml");
        }

        public IActionResult Reports()
        {
            return View("~/Views/Admin/Sales/Reports.cshtml");
        }
    }
}
