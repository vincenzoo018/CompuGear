using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using CompuGear.Data;
using CompuGear.Models;
using CompuGear.Services;

namespace CompuGear.Controllers
{
    /// <summary>
    /// Marketing Controller for Admin - Uses Views/Admin/Marketing folder
    /// RoleId: 1 - Super Admin, 2 - Company Admin, 5 - Marketing Staff
    /// </summary>
    public class MarketingController : Controller
    {
        private readonly CompuGearDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly IAuditService _auditService;

        public MarketingController(CompuGearDbContext context, IWebHostEnvironment environment, IConfiguration configuration, IAuditService auditService)
        {
            _context = context;
            _environment = environment;
            _configuration = configuration;
            _auditService = auditService;
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

        #region Helpers

        private int? GetCompanyId()
        {
            var roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId == 1) return null; // Super Admin sees everything
            return HttpContext.Session.GetInt32("CompanyId");
        }

        private int? GetRoleId()
        {
            return HttpContext.Session.GetInt32("RoleId");
        }

        private bool HasMarketingAccess()
        {
            var roleId = GetRoleId();
            return roleId == 1 || roleId == 2 || roleId == 5;
        }

        #endregion

        #region View Endpoints

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

        #endregion

        #region API Endpoints - Existing

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

        #region API Endpoints - Campaigns

        [HttpGet]
        [Route("api/campaigns")]
        public async Task<IActionResult> GetCampaigns()
        {
            if (!HasMarketingAccess())
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "You do not have permission to access campaigns." });

            try
            {
                var companyId = GetCompanyId();
                var campaigns = await _context.Campaigns
                    .Where(c => companyId == null || c.CompanyId == companyId)
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new
                    {
                        c.CampaignId,
                        c.CampaignCode,
                        c.CampaignName,
                        c.Description,
                        c.Type,
                        c.Status,
                        c.StartDate,
                        c.EndDate,
                        c.Budget,
                        c.ActualSpend,
                        c.TargetSegment,
                        c.TotalReach,
                        c.Impressions,
                        c.Clicks,
                        c.Conversions,
                        c.Revenue,
                        c.CreatedAt,
                        ROI = c.ActualSpend > 0 ? (c.Revenue - c.ActualSpend) / c.ActualSpend * 100 : 0
                    })
                    .ToListAsync();

                return Ok(campaigns);
            }
            catch (Exception)
            {
                return Ok(new List<object>());
            }
        }

        [HttpGet]
        [Route("api/campaigns/{id}")]
        public async Task<IActionResult> GetCampaign(int id)
        {
            if (!HasMarketingAccess())
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "You do not have permission to access campaigns." });

            var companyId = GetCompanyId();
            var campaign = await _context.Campaigns.FirstOrDefaultAsync(c => c.CampaignId == id && (companyId == null || c.CompanyId == companyId));
            if (campaign == null) return NotFound();
            return Ok(campaign);
        }

        [HttpPost]
        [Route("api/campaigns")]
        public async Task<IActionResult> CreateCampaign([FromBody] Campaign campaign)
        {
            if (!HasMarketingAccess())
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "You do not have permission to create campaigns." });

            try
            {
                var companyId = GetCompanyId();
                campaign.CompanyId = companyId;
                campaign.CampaignCode = $"CMP-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
                campaign.CreatedAt = DateTime.UtcNow;
                campaign.UpdatedAt = DateTime.UtcNow;

                _context.Campaigns.Add(campaign);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Campaign created successfully", data = campaign });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPut]
        [Route("api/campaigns/{id}")]
        public async Task<IActionResult> UpdateCampaign(int id, [FromBody] Campaign campaign)
        {
            if (!HasMarketingAccess())
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "You do not have permission to update campaigns." });

            try
            {
                var companyId = GetCompanyId();
                var existing = await _context.Campaigns.FindAsync(id);
                if (existing == null) return NotFound();
                if (companyId != null && existing.CompanyId != null && existing.CompanyId != companyId) return NotFound();

                // Assign CompanyId if not set (legacy data migration)
                if (existing.CompanyId == null && companyId != null)
                    existing.CompanyId = companyId;

                existing.CampaignName = campaign.CampaignName;
                existing.Description = campaign.Description;
                existing.Type = campaign.Type;
                existing.Status = campaign.Status;
                existing.StartDate = campaign.StartDate;
                existing.EndDate = campaign.EndDate;
                existing.Budget = campaign.Budget;
                existing.TargetSegment = campaign.TargetSegment;
                existing.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Campaign updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpDelete]
        [Route("api/campaigns/{id}")]
        public async Task<IActionResult> DeleteCampaign(int id)
        {
            if (!HasMarketingAccess())
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "You do not have permission to delete campaigns." });

            var companyId = GetCompanyId();
            var campaign = await _context.Campaigns.FindAsync(id);
            if (campaign == null) return NotFound();
            if (companyId != null && campaign.CompanyId != null && campaign.CompanyId != companyId) return NotFound();

            _context.Campaigns.Remove(campaign);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Campaign deleted successfully" });
        }

        #endregion

        #region API Endpoints - Promotions

        [HttpGet]
        [Route("api/promotions")]
        public async Task<IActionResult> GetPromotions()
        {
            if (!HasMarketingAccess())
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "You do not have permission to access promotions." });

            try
            {
                var companyId = GetCompanyId();
                var promotions = await _context.Promotions
                    .Where(p => companyId == null || p.CompanyId == companyId)
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new
                    {
                        p.PromotionId,
                        p.PromotionCode,
                        p.PromotionName,
                        p.Description,
                        p.ImageUrl,
                        p.DiscountType,
                        p.DiscountValue,
                        p.MinOrderAmount,
                        p.MaxDiscountAmount,
                        p.StartDate,
                        p.EndDate,
                        p.UsageLimit,
                        p.TimesUsed,
                        p.IsActive,
                        p.CampaignId,
                        p.CreatedAt,
                        IsShowInCustomer = p.IsActive && p.StartDate <= DateTime.Now && p.EndDate >= DateTime.Now
                    })
                    .ToListAsync();

                return Ok(promotions);
            }
            catch (Exception)
            {
                return Ok(new List<object>());
            }
        }

        [HttpGet]
        [Route("api/promotions/active")]
        public async Task<IActionResult> GetActivePromotions()
        {
            if (!HasMarketingAccess())
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "You do not have permission to access promotions." });

            try
            {
                var companyId = GetCompanyId();
                var now = DateTime.Now;
                var promotions = await _context.Promotions
                    .Where(p => p.IsActive && p.StartDate <= now && p.EndDate >= now && (companyId == null || p.CompanyId == companyId))
                    .OrderByDescending(p => p.DiscountValue)
                    .ToListAsync();

                return Ok(promotions);
            }
            catch (Exception)
            {
                return Ok(new List<object>());
            }
        }

        [HttpGet]
        [Route("api/promotions/{id}")]
        public async Task<IActionResult> GetPromotion(int id)
        {
            if (!HasMarketingAccess())
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "You do not have permission to access promotions." });

            var companyId = GetCompanyId();
            var promotion = await _context.Promotions.FirstOrDefaultAsync(p => p.PromotionId == id && (companyId == null || p.CompanyId == companyId));
            if (promotion == null) return NotFound();
            return Ok(promotion);
        }

        [HttpPost]
        [Route("api/promotions")]
        public async Task<IActionResult> CreatePromotion([FromBody] Promotion promotion)
        {
            if (!HasMarketingAccess())
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "You do not have permission to create promotions." });

            try
            {
                var companyId = GetCompanyId();
                promotion.CompanyId = companyId;
                promotion.CreatedAt = DateTime.UtcNow;
                promotion.UpdatedAt = DateTime.UtcNow;

                _context.Promotions.Add(promotion);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Promotion created successfully", data = promotion });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPut]
        [Route("api/promotions/{id}")]
        public async Task<IActionResult> UpdatePromotion(int id, [FromBody] Promotion promotion)
        {
            if (!HasMarketingAccess())
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "You do not have permission to update promotions." });

            try
            {
                var companyId = GetCompanyId();
                var existing = await _context.Promotions.FindAsync(id);
                if (existing == null) return NotFound();
                if (companyId != null && existing.CompanyId != null && existing.CompanyId != companyId) return NotFound();

                // Assign CompanyId if not set (legacy data migration)
                if (existing.CompanyId == null && companyId != null)
                    existing.CompanyId = companyId;

                existing.PromotionCode = promotion.PromotionCode;
                existing.PromotionName = promotion.PromotionName;
                existing.Description = promotion.Description;
                existing.ImageUrl = promotion.ImageUrl;
                existing.DiscountType = promotion.DiscountType;
                existing.DiscountValue = promotion.DiscountValue;
                existing.MinOrderAmount = promotion.MinOrderAmount;
                existing.MaxDiscountAmount = promotion.MaxDiscountAmount;
                existing.StartDate = promotion.StartDate;
                existing.EndDate = promotion.EndDate;
                existing.UsageLimit = promotion.UsageLimit;
                existing.IsActive = promotion.IsActive;
                existing.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Promotion updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPut]
        [Route("api/promotions/{id}/toggle")]
        public async Task<IActionResult> TogglePromotionVisibility(int id)
        {
            if (!HasMarketingAccess())
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "You do not have permission to update promotions." });

            try
            {
                var companyId = GetCompanyId();
                var promotion = await _context.Promotions.FindAsync(id);
                if (promotion == null) return NotFound();
                if (companyId != null && promotion.CompanyId != null && promotion.CompanyId != companyId) return NotFound();

                promotion.IsActive = !promotion.IsActive;
                promotion.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = $"Promotion {(promotion.IsActive ? "activated" : "deactivated")} successfully", isActive = promotion.IsActive });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPut]
        [Route("api/promotions/{id}/status")]
        public async Task<IActionResult> UpdatePromotionStatus(int id, [FromBody] StatusUpdateDto status)
        {
            if (!HasMarketingAccess())
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "You do not have permission to update promotions." });

            try
            {
                var companyId = GetCompanyId();
                var promotion = await _context.Promotions.FindAsync(id);
                if (promotion == null)
                    return NotFound(new { success = false, message = "Promotion not found" });
                if (companyId != null && promotion.CompanyId != null && promotion.CompanyId != companyId)
                    return NotFound(new { success = false, message = "Promotion not found" });

                promotion.IsActive = status.IsActive;
                promotion.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = $"Promotion {(status.IsActive ? "activated" : "deactivated")} successfully", isActive = promotion.IsActive });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpDelete]
        [Route("api/promotions/{id}")]
        public async Task<IActionResult> DeletePromotion(int id)
        {
            if (!HasMarketingAccess())
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "You do not have permission to delete promotions." });

            var companyId = GetCompanyId();
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null) return NotFound();
            if (companyId != null && promotion.CompanyId != null && promotion.CompanyId != companyId) return NotFound();

            _context.Promotions.Remove(promotion);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Promotion deleted successfully" });
        }

        #endregion

        #region API Endpoints - Marketing Segments & Analytics

        [HttpGet]
        [Route("api/marketing/segments")]
        public async Task<IActionResult> GetMarketingSegments()
        {
            if (!HasMarketingAccess())
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "You do not have permission to access customer segments." });

            try
            {
                var companyId = GetCompanyId();
                var customers = await _context.Customers
                    .Where(c => (companyId == null || c.CompanyId == companyId || c.CompanyId == null) && c.Status == "Active")
                    .ToListAsync();

                var now = DateTime.UtcNow;
                var thirtyDaysAgo = now.AddDays(-30);

                var highSpendThreshold = customers.Any()
                    ? customers.OrderByDescending(c => c.TotalSpent).Take(Math.Max(1, customers.Count / 10)).Min(c => c.TotalSpent)
                    : 500m;
                highSpendThreshold = Math.Max(highSpendThreshold, 500m);

                var segments = new
                {
                    vip = new
                    {
                        name = "VIP & Premium Customers",
                        description = "High-value customers with significant purchase history",
                        count = customers.Count(c => c.TotalSpent >= highSpendThreshold || c.LoyaltyPoints >= 500),
                        color = "#10b981"
                    },
                    business = new
                    {
                        name = "Business Accounts",
                        description = "B2B and enterprise customers",
                        count = customers.Count(c => !string.IsNullOrWhiteSpace(c.CompanyName)),
                        color = "#3b82f6"
                    },
                    newCustomers = new
                    {
                        name = "New Customers",
                        description = "Customers acquired in the last 30 days",
                        count = customers.Count(c => c.CreatedAt >= thirtyDaysAgo),
                        color = "#f59e0b"
                    },
                    regular = new
                    {
                        name = "Regular Customers",
                        description = "Standard customer accounts",
                        count = customers.Count(c => c.TotalSpent < highSpendThreshold && string.IsNullOrWhiteSpace(c.CompanyName) && c.CreatedAt < thirtyDaysAgo),
                        color = "#6b7280"
                    },
                    all = new
                    {
                        name = "All Customers",
                        description = "Complete active customer base",
                        count = customers.Count,
                        color = "#008080"
                    }
                };

                return Ok(new { success = true, data = segments });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message, data = new { } });
            }
        }

        [HttpGet]
        [Route("api/marketing/analytics")]
        public async Task<IActionResult> GetMarketingAnalytics()
        {
            if (!HasMarketingAccess())
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "You do not have permission to access marketing analytics." });

            try
            {
                var companyId = GetCompanyId();
                var campaigns = await _context.Campaigns
                    .Where(c => companyId == null || c.CompanyId == companyId || c.CompanyId == null)
                    .ToListAsync();
                var promotions = await _context.Promotions
                    .Where(p => companyId == null || p.CompanyId == companyId || p.CompanyId == null)
                    .ToListAsync();

                var totalReach = campaigns.Sum(c => c.TotalReach);
                var totalClicks = campaigns.Sum(c => c.Clicks);
                var totalImpressions = campaigns.Sum(c => c.Impressions);
                var totalSpend = campaigns.Sum(c => c.ActualSpend);
                var totalRevenue = campaigns.Sum(c => c.Revenue);

                var data = new
                {
                    totalCampaigns = campaigns.Count,
                    totalReach,
                    engagementRate = totalImpressions > 0 ? ((decimal)totalClicks / totalImpressions) * 100 : 0,
                    roi = totalSpend > 0 ? ((totalRevenue - totalSpend) / totalSpend) * 100 : 0,
                    activeCampaigns = campaigns.Count(c => c.Status == "Active"),
                    promotionsCount = promotions.Count,
                    topCampaigns = campaigns
                        .OrderByDescending(c => c.Revenue)
                        .Take(5)
                        .Select(c => new
                        {
                            c.CampaignId,
                            c.CampaignName,
                            c.TotalReach,
                            c.Conversions,
                            c.ActualSpend,
                            c.Revenue
                        })
                        .ToList(),
                    topPromotions = promotions
                        .OrderByDescending(p => p.TimesUsed)
                        .Take(5)
                        .Select(p => new
                        {
                            p.PromotionId,
                            p.PromotionName,
                            p.PromotionCode,
                            UsageCount = p.TimesUsed
                        })
                        .ToList()
                };

                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message, data = new { } });
            }
        }

        #endregion
    }
}
