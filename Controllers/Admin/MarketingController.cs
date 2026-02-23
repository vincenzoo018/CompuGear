using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using CompuGear.Data;
using CompuGear.Models;

namespace CompuGear.Controllers
{
    /// <summary>
    /// Marketing Controller for Admin - Uses Views/Admin/Marketing folder
    /// RoleId: 1 - Super Admin, 2 - Company Admin
    /// </summary>
    public class MarketingController : Controller
    {
        private readonly CompuGearDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public MarketingController(CompuGearDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

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

        public IActionResult Campaigns()
        {
            return View("~/Views/Admin/Marketing/Campaigns.cshtml");
        }

        public IActionResult CampaignsArchive()
        {
            return View("~/Views/Admin/Marketing/CampaignsArchive.cshtml");
        }

        public IActionResult Promotions()
        {
            return View("~/Views/Admin/Marketing/Promotions.cshtml");
        }

        public IActionResult PromotionsArchive()
        {
            return View("~/Views/Admin/Marketing/PromotionsArchive.cshtml");
        }

        public IActionResult Segments()
        {
            return View("~/Views/Admin/Marketing/Segments.cshtml");
        }

        public IActionResult Analytics()
        {
            return View("~/Views/Admin/Marketing/Analytics.cshtml");
        }

        #region API Endpoints

        // Get all segments with customer counts
        [HttpGet]
        public async Task<IActionResult> GetSegments()
        {
            try
            {
                var customers = await _context.Customers
                    .Include(c => c.Category)
                    .Where(c => c.Status == "Active")
                    .ToListAsync();

                var now = DateTime.Now;
                var thirtyDaysAgo = now.AddDays(-30);

                // VIP: high spenders (top 10% by TotalSpent or over $1000 spent)
                var highSpendThreshold = customers.Any() ? customers.OrderByDescending(c => c.TotalSpent).Take(Math.Max(1, customers.Count / 10)).Min(c => c.TotalSpent) : 1000m;
                highSpendThreshold = Math.Max(highSpendThreshold, 500m);

                var segments = new
                {
                    vip = new
                    {
                        name = "VIP & Premium Customers",
                        description = "High-value customers with significant purchase history",
                        customerType = "VIP / Premium",
                        priority = "High priority support",
                        count = customers.Count(c => c.TotalSpent >= highSpendThreshold || c.LoyaltyPoints >= 500),
                        color = "#10b981"
                    },
                    business = new
                    {
                        name = "Business Accounts",
                        description = "B2B and enterprise customers",
                        customerType = "Business",
                        features = "Bulk orders, invoicing",
                        count = customers.Count(c => !string.IsNullOrEmpty(c.CompanyName)),
                        color = "#3b82f6"
                    },
                    newCustomers = new
                    {
                        name = "New Customers",
                        description = "Customers acquired in last 30 days",
                        onboarding = "Welcome emails sent",
                        focus = "First purchase incentives",
                        count = customers.Count(c => c.CreatedAt >= thirtyDaysAgo),
                        color = "#f59e0b"
                    },
                    regular = new
                    {
                        name = "Regular Customers",
                        description = "Standard customer accounts",
                        engagement = "Newsletter, promotions",
                        potential = "Upgrade to VIP",
                        count = customers.Count(c => c.TotalSpent < highSpendThreshold && string.IsNullOrEmpty(c.CompanyName) && c.CreatedAt < thirtyDaysAgo),
                        color = "#6b7280"
                    },
                    all = new
                    {
                        name = "All Customers",
                        description = "Complete customer database",
                        active = customers.Count(c => c.Status == "Active"),
                        withEmail = customers.Count(c => !string.IsNullOrEmpty(c.Email)),
                        count = customers.Count,
                        color = "#008080"
                    }
                };

                return Json(new { success = true, data = segments });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Get analytics data
        [HttpGet]
        public async Task<IActionResult> GetAnalyticsData()
        {
            try
            {
                var campaigns = await _context.Campaigns.ToListAsync();
                var promotions = await _context.Promotions.ToListAsync();

                var totalBudget = campaigns.Sum(c => c.Budget ?? 0);
                var totalSpent = campaigns.Sum(c => c.ActualSpend);
                var totalRevenue = campaigns.Sum(c => c.Revenue);
                var activeCampaigns = campaigns.Count(c => c.Status == "Active");
                var activePromotions = promotions.Count(p => p.IsActive && p.StartDate <= DateTime.Now && p.EndDate >= DateTime.Now);
                
                var percentageDiscounts = promotions.Where(p => p.DiscountType == "Percentage").Select(p => p.DiscountValue).ToList();
                var avgDiscount = percentageDiscounts.Count > 0 ? percentageDiscounts.Average() : 0;

                // Status distribution
                var statusCounts = new
                {
                    draft = campaigns.Count(c => c.Status == "Draft"),
                    active = campaigns.Count(c => c.Status == "Active"),
                    paused = campaigns.Count(c => c.Status == "Paused"),
                    completed = campaigns.Count(c => c.Status == "Completed")
                };

                // Budget by campaign (top 5)
                var budgetByCampaign = campaigns
                    .OrderByDescending(c => c.Budget)
                    .Take(5)
                    .Select(c => new { name = c.CampaignName, budget = c.Budget ?? 0 })
                    .ToList();

                // Monthly performance
                var monthlyData = campaigns
                    .GroupBy(c => c.CreatedAt.Month)
                    .Select(g => new { month = g.Key, count = g.Count(), budget = g.Sum(c => c.Budget ?? 0) })
                    .OrderBy(m => m.month)
                    .ToList();

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        totalBudget,
                        totalSpent,
                        totalRevenue,
                        activeCampaigns,
                        activePromotions,
                        avgDiscount,
                        totalPromotions = promotions.Count,
                        totalCampaigns = campaigns.Count,
                        statusCounts,
                        budgetByCampaign,
                        monthlyData,
                        overallROI = totalSpent > 0 ? (totalRevenue - totalSpent) / totalSpent * 100 : 0
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Upload promotion image
        [HttpPost]
        public async Task<IActionResult> UploadPromotionImage(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return Json(new { success = false, message = "No file uploaded" });

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(extension))
                    return Json(new { success = false, message = "Invalid file type. Allowed: jpg, jpeg, png, gif, webp" });

                // Create promotions folder if it doesn't exist
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "promotions");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                // Generate unique filename
                var fileName = $"promo_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString()[..8]}{extension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var imageUrl = $"/images/promotions/{fileName}";
                return Json(new { success = true, imageUrl });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Upload campaign image
        [HttpPost]
        public async Task<IActionResult> UploadCampaignImage(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return Json(new { success = false, message = "No file uploaded" });

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(extension))
                    return Json(new { success = false, message = "Invalid file type" });

                var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "campaigns");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = $"campaign_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString()[..8]}{extension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var imageUrl = $"/images/campaigns/{fileName}";
                return Json(new { success = true, imageUrl });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        #endregion
    }
}
