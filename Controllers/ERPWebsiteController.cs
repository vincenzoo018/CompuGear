using Microsoft.AspNetCore.Mvc;

namespace CompuGear.Controllers
{
    /// <summary>
    /// ERPWebsite Controller - Public-facing website for CompuGear ERP subscriptions
    /// </summary>
    public class ERPWebsiteController : Controller
    {
        // Landing Page
        public IActionResult Index()
        {
            return View("~/Views/ERPWebsite/Index.cshtml");
        }

        // Features Page
        public IActionResult Features()
        {
            return View("~/Views/ERPWebsite/Features.cshtml");
        }

        // About Us Page
        public IActionResult About()
        {
            return View("~/Views/ERPWebsite/About.cshtml");
        }

        // Pricing Page
        public IActionResult Pricing()
        {
            return View("~/Views/ERPWebsite/Pricing.cshtml");
        }

        // Contact Page
        public IActionResult Contact()
        {
            return View("~/Views/ERPWebsite/Contact.cshtml");
        }

        // Subscribe Page (with plan pre-selected)
        public IActionResult Subscribe(string? plan)
        {
            ViewData["SelectedPlan"] = plan ?? "Basic";
            return View("~/Views/ERPWebsite/Subscribe.cshtml");
        }
    }
}
