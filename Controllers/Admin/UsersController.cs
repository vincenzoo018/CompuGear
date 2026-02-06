using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CompuGear.Controllers
{
    /// <summary>
    /// Users Controller for Admin - Uses Views/Admin/Users folder
    /// RoleId: 1 - Super Admin, 2 - Company Admin
    /// </summary>
    public class UsersController : Controller
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

        public IActionResult Accounts()
        {
            return View("~/Views/Admin/Users/Accounts.cshtml");
        }

        public IActionResult Roles()
        {
            return View("~/Views/Admin/Users/Roles.cshtml");
        }

        public IActionResult Activity()
        {
            return View("~/Views/Admin/Users/Activity.cshtml");
        }
    }
}
