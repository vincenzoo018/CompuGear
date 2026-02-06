using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CompuGear.Controllers
{
    /// <summary>
    /// Customers Controller for Admin - Uses Views/Admin/Customers folder
    /// RoleId: 1 - Super Admin, 2 - Company Admin
    /// </summary>
    public class CustomersController : Controller
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

        public IActionResult List()
        {
            return View("~/Views/Admin/Customers/List.cshtml");
        }

        public IActionResult Profile(int? id)
        {
            return View("~/Views/Admin/Customers/Profile.cshtml");
        }

        public IActionResult History(int? id)
        {
            return View("~/Views/Admin/Customers/History.cshtml");
        }
    }
}
