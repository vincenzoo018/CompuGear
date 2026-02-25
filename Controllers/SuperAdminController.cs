using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CompuGear.Controllers
{
    /// <summary>
    /// Super Admin Controller - Platform-wide management
    /// Only accessible by Super Admin (RoleId = 1)
    /// </summary>
    public class SuperAdminController : Controller
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);
            var roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId == null || roleId != 1)
            {
                context.Result = RedirectToAction("Login", "Auth");
            }
        }

        // Dashboard
        public IActionResult Index()
        {
            return View("~/Views/SuperAdmin/Index.cshtml");
        }

        // Company Management
        public IActionResult Companies()
        {
            return View("~/Views/SuperAdmin/Companies.cshtml");
        }

        // Subscriptions
        public IActionResult Subscriptions()
        {
            return View("~/Views/SuperAdmin/Subscriptions.cshtml");
        }

        // ERP Modules
        public IActionResult Modules()
        {
            return View("~/Views/SuperAdmin/Modules.cshtml");
        }

        // Platform Usage
        public IActionResult Usage()
        {
            return View("~/Views/SuperAdmin/Usage.cshtml");
        }

        // Platform Reports
        public IActionResult Reports()
        {
            return View("~/Views/SuperAdmin/Reports.cshtml");
        }

        // System Settings
        public IActionResult Settings()
        {
            return View("~/Views/SuperAdmin/Settings.cshtml");
        }

        // User Management
        public IActionResult Users()
        {
            return View("~/Views/SuperAdmin/Users.cshtml");
        }

        // Company Archive
        public IActionResult CompaniesArchive()
        {
            return View("~/Views/SuperAdmin/CompaniesArchive.cshtml");
        }

        // User Archive
        public IActionResult UsersArchive()
        {
            return View("~/Views/SuperAdmin/UsersArchive.cshtml");
        }
    }
}
