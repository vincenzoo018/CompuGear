using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using CompuGear.Data;
using Microsoft.Extensions.Caching.Memory;

namespace CompuGear.Controllers
{
    /// <summary>
    /// Customer Support Staff Portal Controller
    /// RoleId: 4 - Customer Support Staff
    /// Access: Tickets, customer profiles (read-only for sensitive), knowledge base, support reports
    /// </summary>
    public class SupportStaffController : Controller
    {
        private readonly CompuGearDbContext _context;
        private readonly IMemoryCache _cache;

        public SupportStaffController(CompuGearDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // Role-based authorization check
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);
            
            var roleId = HttpContext.Session.GetInt32("RoleId");
            
            // Allow access for: Super Admin (1), Company Admin (2), Customer Support Staff (4)
            if (roleId == null || (roleId != 1 && roleId != 2 && roleId != 4))
            {
                context.Result = RedirectToAction("Login", "Auth");
                return;
            }

            if (roleId == 4)
            {
                var companyId = HttpContext.Session.GetInt32("CompanyId");
                if (!companyId.HasValue || !HasModuleAccess(companyId.Value, roleId.Value, "SUPPORT"))
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

        // Live Chat Support
        public IActionResult LiveChat()
        {
            ViewData["Title"] = "Live Chat Support";
            return View("~/Views/SupportStaff/LiveChat.cshtml");
        }
    }
}
