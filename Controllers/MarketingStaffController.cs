using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CompuGear.Controllers
{
    /// <summary>
    /// Marketing Staff Portal Controller
    /// RoleId: 5 - Marketing Staff
    /// Access: Campaigns, promotions, segments, analytics/reports
    /// </summary>
    public class MarketingStaffController : Controller
    {
        // Role-based authorization check
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);
            
            var roleId = HttpContext.Session.GetInt32("RoleId");
            
            // Allow access for: Super Admin (1), Company Admin (2), Marketing Staff (5)
            if (roleId == null || (roleId != 1 && roleId != 2 && roleId != 5))
            {
                context.Result = RedirectToAction("Login", "Auth");
            }
        }

        // Dashboard
        public IActionResult Index()
        {
            ViewData["Title"] = "Marketing Dashboard";
            return View("~/Views/MarketingStaff/Index.cshtml");
        }

        // Campaigns
        public IActionResult Campaigns()
        {
            ViewData["Title"] = "Marketing Campaigns";
            return View("~/Views/MarketingStaff/Campaigns.cshtml");
        }

        public IActionResult CampaignDetails(int id)
        {
            ViewData["Title"] = "Campaign Details";
            ViewData["CampaignId"] = id;
            return View("~/Views/MarketingStaff/CampaignDetails.cshtml");
        }

        public IActionResult CreateCampaign()
        {
            ViewData["Title"] = "Create Campaign";
            return View("~/Views/MarketingStaff/CreateCampaign.cshtml");
        }

        // Promotions
        public IActionResult Promotions()
        {
            ViewData["Title"] = "Promotions";
            return View("~/Views/MarketingStaff/Promotions.cshtml");
        }

        public IActionResult PromotionDetails(int id)
        {
            ViewData["Title"] = "Promotion Details";
            ViewData["PromotionId"] = id;
            return View("~/Views/MarketingStaff/PromotionDetails.cshtml");
        }

        public IActionResult CreatePromotion()
        {
            ViewData["Title"] = "Create Promotion";
            return View("~/Views/MarketingStaff/CreatePromotion.cshtml");
        }

        // Customer Segments
        public IActionResult Segments()
        {
            ViewData["Title"] = "Customer Segments";
            return View("~/Views/MarketingStaff/Segments.cshtml");
        }

        public IActionResult SegmentDetails(int id)
        {
            ViewData["Title"] = "Segment Details";
            ViewData["SegmentId"] = id;
            return View("~/Views/MarketingStaff/SegmentDetails.cshtml");
        }

        // Marketing Analytics
        public IActionResult Analytics()
        {
            ViewData["Title"] = "Marketing Analytics";
            return View("~/Views/MarketingStaff/Analytics.cshtml");
        }
    }
}
