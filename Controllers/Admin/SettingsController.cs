using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CompuGear.Controllers
{
    /// <summary>
    /// Settings Controller for Admin - Uses Views/Admin/Settings folder
    /// RoleId: 1 - Super Admin, 2 - Company Admin
    /// </summary>
    public class SettingsController : Controller
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

        public IActionResult Index()
        {
            return View("~/Views/Admin/Settings/Index.cshtml");
        }
    }
}
