using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CompuGear.Controllers
{
    /// <summary>
    /// Customer Support Staff Portal Controller
    /// RoleId: 4 - Customer Support Staff
    /// Access: Tickets, customer profiles (read-only for sensitive), knowledge base, support reports
    /// </summary>
    public class SupportStaffController : Controller
    {
        // Role-based authorization check
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);
            
            var roleId = HttpContext.Session.GetInt32("RoleId");
            
            // Allow access for: Super Admin (1), Company Admin (2), Customer Support Staff (4)
            if (roleId == null || (roleId != 1 && roleId != 2 && roleId != 4))
            {
                context.Result = RedirectToAction("Login", "Auth");
            }
        }

        // Dashboard
        public IActionResult Index()
        {
            ViewData["Title"] = "Support Dashboard";
            return View("~/Views/SupportStaff/Index.cshtml");
        }

        // Support Tickets
        public IActionResult Tickets()
        {
            ViewData["Title"] = "Support Tickets";
            return View("~/Views/SupportStaff/Tickets.cshtml");
        }

        public IActionResult TicketDetails(int id)
        {
            ViewData["Title"] = "Ticket Details";
            ViewData["TicketId"] = id;
            return View("~/Views/SupportStaff/TicketDetails.cshtml");
        }

        // Customer Profiles (Read-only)
        public IActionResult Customers()
        {
            ViewData["Title"] = "Customer Profiles";
            return View("~/Views/SupportStaff/Customers.cshtml");
        }

        public IActionResult CustomerDetails(int id)
        {
            ViewData["Title"] = "Customer Profile";
            ViewData["CustomerId"] = id;
            return View("~/Views/SupportStaff/CustomerDetails.cshtml");
        }

        // Knowledge Base
        public IActionResult KnowledgeBase()
        {
            ViewData["Title"] = "Knowledge Base";
            return View("~/Views/SupportStaff/KnowledgeBase.cshtml");
        }

        public IActionResult ArticleDetails(int id)
        {
            ViewData["Title"] = "Article Details";
            ViewData["ArticleId"] = id;
            return View("~/Views/SupportStaff/ArticleDetails.cshtml");
        }

        // Support Reports
        public IActionResult Reports()
        {
            ViewData["Title"] = "Support Reports";
            return View("~/Views/SupportStaff/Reports.cshtml");
        }

        // Escalations (Requires Admin Approval)
        public IActionResult Escalations()
        {
            ViewData["Title"] = "Ticket Escalations";
            return View("~/Views/SupportStaff/Escalations.cshtml");
        }
    }
}
