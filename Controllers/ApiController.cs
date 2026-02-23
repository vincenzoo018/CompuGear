using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CompuGear.Data;
using CompuGear.Models;
using CompuGear.Services;
using System.Text;

namespace CompuGear.Controllers
{
    [Route("api")]
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly CompuGearDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IAuditService _auditService;

        public ApiController(CompuGearDbContext context, IConfiguration configuration, IAuditService auditService)
        {
            _context = context;
            _configuration = configuration;
            _auditService = auditService;
        }

        // Helper: returns CompanyId from session. Super Admin (RoleId=1) gets null → sees all data.
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

        private IQueryable<ActivityLog> GetScopedActivityLogQuery()
        {
            var query = _context.ActivityLogs.AsQueryable();
            var companyId = GetCompanyId();

            if (!companyId.HasValue)
                return query;

            var staffUserIds = _context.Users
                .Where(u => u.CompanyId == companyId)
                .Select(u => u.UserId);

            var customerLinkedUserIds = _context.Customers
                .Where(c => c.CompanyId == companyId && c.UserId.HasValue)
                .Select(c => c.UserId!.Value);

            var companyUserIds = staffUserIds.Union(customerLinkedUserIds);

            return query.Where(a => a.UserId.HasValue && companyUserIds.Contains(a.UserId.Value));
        }

        private IQueryable<ActivityLog> ApplyUserTypeFilter(IQueryable<ActivityLog> query, string? userType)
        {
            if (string.IsNullOrWhiteSpace(userType))
                return query;

            var normalized = userType.Trim().ToLowerInvariant();

            if (normalized == "system")
                return query.Where(a => !a.UserId.HasValue);

            if (normalized == "customer")
            {
                return query.Where(a => a.UserId.HasValue && _context.Users
                    .Any(u => u.UserId == a.UserId.Value && u.RoleId == 7));
            }

            if (normalized == "staff")
            {
                return query.Where(a => a.UserId.HasValue && _context.Users
                    .Any(u => u.UserId == a.UserId.Value && u.RoleId != 7));
            }

            return query;
        }

        private bool HasFullBillingAccess()
        {
            return false;
        }

        private bool HasAdminOrderAccess()
        {
            var roleId = GetRoleId();
            return roleId == 1 || roleId == 2;
        }

        private bool HasMarketingAccess()
        {
            var roleId = GetRoleId();
            return roleId == 1 || roleId == 2 || roleId == 5;
        }

        private static void SyncInvoiceFromOrderState(Invoice invoice, Order order)
        {
            var isOrderConfirmed = order.OrderStatus == "Confirmed";
            var isOrderPaid = order.PaymentStatus == "Paid" || order.PaidAmount >= order.TotalAmount;

            if (isOrderConfirmed && isOrderPaid)
            {
                invoice.Status = "Paid";
                invoice.PaidAmount = invoice.TotalAmount;
                invoice.BalanceDue = 0;
                if (!invoice.PaidAt.HasValue)
                    invoice.PaidAt = DateTime.UtcNow;
            }
            else
            {
                invoice.PaidAmount = Math.Min(invoice.PaidAmount, invoice.TotalAmount);
                invoice.BalanceDue = Math.Max(0, invoice.TotalAmount - invoice.PaidAmount);

                if (invoice.PaidAmount >= invoice.TotalAmount)
                {
                    invoice.Status = "Paid";
                    if (!invoice.PaidAt.HasValue)
                        invoice.PaidAt = DateTime.UtcNow;
                }
                else if (invoice.PaidAmount > 0 && invoice.Status != "Cancelled" && invoice.Status != "Void")
                {
                    invoice.Status = "Partial";
                }
                else if (invoice.PaidAmount <= 0 && invoice.Status == "Paid")
                {
                    invoice.Status = "Pending";
                    invoice.PaidAt = null;
                }
            }

            invoice.UpdatedAt = DateTime.UtcNow;
        }

        #region Marketing - Campaigns

        [HttpGet("campaigns")]
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

        [HttpGet("campaigns/{id}")]
        public async Task<IActionResult> GetCampaign(int id)
        {
            if (!HasMarketingAccess())
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "You do not have permission to access campaigns." });

            var companyId = GetCompanyId();
            var campaign = await _context.Campaigns.FirstOrDefaultAsync(c => c.CampaignId == id && (companyId == null || c.CompanyId == companyId));
            if (campaign == null) return NotFound();
            return Ok(campaign);
        }

        [HttpPost("campaigns")]
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

        [HttpPut("campaigns/{id}")]
        public async Task<IActionResult> UpdateCampaign(int id, [FromBody] Campaign campaign)
        {
            if (!HasMarketingAccess())
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "You do not have permission to update campaigns." });

            try
            {
                var companyId = GetCompanyId();
                var existing = await _context.Campaigns.FindAsync(id);
                if (existing == null || (companyId != null && existing.CompanyId != companyId)) return NotFound();

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

        [HttpDelete("campaigns/{id}")]
        public async Task<IActionResult> DeleteCampaign(int id)
        {
            if (!HasMarketingAccess())
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "You do not have permission to delete campaigns." });

            var companyId = GetCompanyId();
            var campaign = await _context.Campaigns.FindAsync(id);
            if (campaign == null || (companyId != null && campaign.CompanyId != companyId)) return NotFound();

            _context.Campaigns.Remove(campaign);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Campaign deleted successfully" });
        }

        #endregion

        #region Marketing - Promotions

        [HttpGet("promotions")]
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

        [HttpGet("promotions/active")]
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

        [HttpGet("promotions/{id}")]
        public async Task<IActionResult> GetPromotion(int id)
        {
            if (!HasMarketingAccess())
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "You do not have permission to access promotions." });

            var companyId = GetCompanyId();
            var promotion = await _context.Promotions.FirstOrDefaultAsync(p => p.PromotionId == id && (companyId == null || p.CompanyId == companyId));
            if (promotion == null) return NotFound();
            return Ok(promotion);
        }

        [HttpPost("promotions")]
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

        [HttpPut("promotions/{id}")]
        public async Task<IActionResult> UpdatePromotion(int id, [FromBody] Promotion promotion)
        {
            if (!HasMarketingAccess())
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "You do not have permission to update promotions." });

            try
            {
                var companyId = GetCompanyId();
                var existing = await _context.Promotions.FindAsync(id);
                if (existing == null || (companyId != null && existing.CompanyId != companyId)) return NotFound();

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

        [HttpPut("promotions/{id}/toggle")]
        public async Task<IActionResult> TogglePromotionVisibility(int id)
        {
            if (!HasMarketingAccess())
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "You do not have permission to update promotions." });

            try
            {
                var companyId = GetCompanyId();
                var promotion = await _context.Promotions.FindAsync(id);
                if (promotion == null || (companyId != null && promotion.CompanyId != companyId)) return NotFound();

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

        [HttpPut("promotions/{id}/status")]
        public async Task<IActionResult> UpdatePromotionStatus(int id, [FromBody] StatusUpdateDto status)
        {
            if (!HasMarketingAccess())
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "You do not have permission to update promotions." });

            try
            {
                var companyId = GetCompanyId();
                var promotion = await _context.Promotions.FindAsync(id);
                if (promotion == null || (companyId != null && promotion.CompanyId != companyId))
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

        [HttpDelete("promotions/{id}")]
        public async Task<IActionResult> DeletePromotion(int id)
        {
            if (!HasMarketingAccess())
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "You do not have permission to delete promotions." });

            var companyId = GetCompanyId();
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null || (companyId != null && promotion.CompanyId != companyId)) return NotFound();

            _context.Promotions.Remove(promotion);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Promotion deleted successfully" });
        }

        [HttpGet("marketing/segments")]
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

        [HttpGet("marketing/analytics")]
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

        #region Customers

        [HttpGet("customers")]
        public async Task<IActionResult> GetCustomers()
        {
            try
            {
                var companyId = GetCompanyId();
                var customers = await _context.Customers
                    .Include(c => c.Category)
                    .Where(c => companyId == null || c.CompanyId == companyId)
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new
                    {
                        c.CustomerId,
                        c.CustomerCode,
                        c.FirstName,
                        c.LastName,
                        FullName = c.FirstName + " " + c.LastName,
                        c.Email,
                        c.Phone,
                        c.Status,
                        c.TotalOrders,
                        c.TotalSpent,
                        c.LoyaltyPoints,
                        CategoryName = c.Category != null ? c.Category.CategoryName : "Standard",
                        c.CategoryId,
                        c.BillingAddress,
                        c.BillingCity,
                        c.BillingState,
                        c.BillingZipCode,
                        c.BillingCountry,
                        c.CompanyName,
                        c.Notes,
                        c.CreatedAt
                    })
                    .ToListAsync();

                return Ok(customers);
            }
            catch (Exception)
            {
                return Ok(new List<object>());
            }
        }

        [HttpGet("customers/{id}")]
        public async Task<IActionResult> GetCustomer(int id)
        {
            try
            {
                var companyId = GetCompanyId();
                var customer = await _context.Customers
                    .Include(c => c.Category)
                    .FirstOrDefaultAsync(c => c.CustomerId == id && (companyId == null || c.CompanyId == companyId));
                if (customer == null) return NotFound();
                return Ok(customer);
            }
            catch (Exception)
            {
                return NotFound();
            }
        }

        [HttpPost("customers")]
        public async Task<IActionResult> CreateCustomer([FromBody] Customer customer)
        {
            try
            {
                var companyId = GetCompanyId();
                customer.CompanyId = companyId;
                customer.CustomerCode = $"CUST-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
                customer.CreatedAt = DateTime.UtcNow;
                customer.UpdatedAt = DateTime.UtcNow;
                customer.Status = "Active";

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Customer created successfully", data = customer });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPut("customers/{id}")]
        public async Task<IActionResult> UpdateCustomer(int id, [FromBody] Customer customer)
        {
            try
            {
                var companyId = GetCompanyId();
                var existing = await _context.Customers.FindAsync(id);
                if (existing == null || (companyId != null && existing.CompanyId != companyId)) return NotFound();

                existing.FirstName = customer.FirstName;
                existing.LastName = customer.LastName;
                existing.Email = customer.Email;
                existing.Phone = customer.Phone;
                existing.Status = customer.Status;
                existing.CategoryId = customer.CategoryId;
                existing.BillingAddress = customer.BillingAddress;
                existing.BillingCity = customer.BillingCity;
                existing.BillingCountry = customer.BillingCountry;
                existing.Notes = customer.Notes;
                existing.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Customer updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpDelete("customers/{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var companyId = GetCompanyId();
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null || (companyId != null && customer.CompanyId != companyId)) return NotFound();

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Customer deleted successfully" });
        }

        [HttpPut("customers/{id}/toggle-status")]
        public async Task<IActionResult> ToggleCustomerStatus(int id)
        {
            try
            {
                var companyId = GetCompanyId();
                var customer = await _context.Customers.FindAsync(id);
                if (customer == null || (companyId != null && customer.CompanyId != companyId)) return NotFound();

                customer.Status = customer.Status == "Active" ? "Inactive" : "Active";
                customer.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = $"Customer {(customer.Status == "Active" ? "activated" : "deactivated")} successfully", status = customer.Status });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("customer-categories")]
        public async Task<IActionResult> GetCustomerCategories()
        {
            try
            {
                var categories = await _context.CustomerCategories.ToListAsync();
                return Ok(categories);
            }
            catch (Exception)
            {
                return Ok(new List<object>());
            }
        }

        #endregion

        #region Products / Inventory

        [HttpGet("products")]
        public async Task<IActionResult> GetProducts()
        {
            try
            {
                var companyId = GetCompanyId();
                var products = await _context.Products
                    .AsNoTracking()
                    .Where(p => companyId == null || p.CompanyId == companyId)
                    .OrderByDescending(p => p.ProductId)
                    .Select(p => new
                    {
                        p.ProductId,
                        p.ProductCode,
                        p.SKU,
                        p.ProductName,
                        p.ShortDescription,
                        CategoryName = p.Category != null ? p.Category.CategoryName : "",
                        p.CategoryId,
                        BrandName = p.Brand != null ? p.Brand.BrandName : "",
                        p.BrandId,
                        SupplierName = p.Supplier != null ? p.Supplier.SupplierName : "",
                        p.SupplierId,
                        p.CostPrice,
                        p.SellingPrice,
                        p.CompareAtPrice,
                        p.StockQuantity,
                        p.ReorderLevel,
                        p.MaxStockLevel,
                        p.Status,
                        p.IsFeatured,
                        p.IsOnSale,
                        p.MainImageUrl,
                        p.CreatedAt
                    })
                    .ToListAsync();

                return Ok(products);
            }
            catch (Exception)
            {
                return Ok(new List<object>());
            }
        }

        [HttpGet("products/{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            try
            {
                var companyId = GetCompanyId();
                var product = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .FirstOrDefaultAsync(p => p.ProductId == id && (companyId == null || p.CompanyId == companyId));
                if (product == null) return NotFound();
                return Ok(product);
            }
            catch (Exception)
            {
                return NotFound();
            }
        }

        [HttpPost("products")]
        public async Task<IActionResult> CreateProduct([FromBody] Product product)
        {
            try
            {
                var companyId = GetCompanyId();
                product.ProductCode = $"PRD-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
                product.CreatedAt = DateTime.UtcNow;
                product.UpdatedAt = DateTime.UtcNow;

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Product created successfully", data = product });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPut("products/{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] Product product)
        {
            try
            {
                var companyId = GetCompanyId();
                var existing = await _context.Products.FindAsync(id);
                if (existing == null || (companyId != null && existing.CompanyId != companyId)) return NotFound();

                existing.ProductName = product.ProductName;
                existing.ShortDescription = product.ShortDescription;
                existing.CategoryId = product.CategoryId;
                existing.BrandId = product.BrandId;
                existing.SupplierId = product.SupplierId;
                existing.CostPrice = product.CostPrice;
                existing.SellingPrice = product.SellingPrice;
                existing.CompareAtPrice = product.CompareAtPrice;
                existing.StockQuantity = product.StockQuantity;
                existing.ReorderLevel = product.ReorderLevel;
                existing.Status = product.Status;
                existing.IsFeatured = product.IsFeatured;
                existing.IsOnSale = product.IsOnSale;
                existing.MainImageUrl = product.MainImageUrl;
                existing.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Product updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPut("products/{id}/status")]
        public async Task<IActionResult> UpdateProductStatus(int id, [FromBody] StatusUpdateRequest request)
        {
            try
            {
                var companyId = GetCompanyId();
                var product = await _context.Products.FindAsync(id);
                if (product == null || (companyId != null && product.CompanyId != companyId))
                    return NotFound(new { success = false, message = "Product not found" });

                product.Status = string.IsNullOrWhiteSpace(request.Status) ? product.Status : request.Status.Trim();
                product.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = $"Product {(product.Status == "Active" ? "activated" : "deactivated")} successfully", status = product.Status });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPut("products/{id}/stock")]
        public async Task<IActionResult> UpdateStock(int id, [FromBody] StockUpdateRequest request)
        {
            try
            {
                var companyId = GetCompanyId();
                var product = await _context.Products
                    .AsTracking()
                    .FirstOrDefaultAsync(p => p.ProductId == id && (companyId == null || p.CompanyId == companyId));
                if (product == null) return NotFound();

                var previousStock = product.StockQuantity;
                product.StockQuantity = request.NewQuantity;
                product.UpdatedAt = DateTime.UtcNow;

                // Create inventory transaction
                var transaction = new InventoryTransaction
                {
                    ProductId = id,
                    TransactionType = request.TransactionType ?? "Adjustment",
                    Quantity = request.NewQuantity - previousStock,
                    PreviousStock = previousStock,
                    NewStock = request.NewQuantity,
                    Notes = request.Notes,
                    TransactionDate = DateTime.UtcNow
                };

                _context.InventoryTransactions.Add(transaction);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Stock updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpDelete("products/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var companyId = GetCompanyId();
            var product = await _context.Products.FindAsync(id);
            if (product == null || (companyId != null && product.CompanyId != companyId)) return NotFound();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Product deleted successfully" });
        }

        [HttpGet("product-categories")]
        public async Task<IActionResult> GetProductCategories()
        {
            try
            {
                var categories = await _context.ProductCategories.Where(c => c.IsActive).ToListAsync();
                return Ok(categories);
            }
            catch (Exception)
            {
                return Ok(new List<object>());
            }
        }

        [HttpGet("product-categories/all")]
        public async Task<IActionResult> GetAllProductCategories()
        {
            try
            {
                var categories = await _context.ProductCategories
                    .OrderBy(c => c.DisplayOrder)
                    .ThenBy(c => c.CategoryName)
                    .Select(c => new
                    {
                        c.CategoryId,
                        c.CategoryName,
                        c.Description,
                        c.DisplayOrder,
                        c.IsActive,
                        c.CreatedAt,
                        c.UpdatedAt,
                        ProductCount = _context.Products.Count(p => p.CategoryId == c.CategoryId)
                    })
                    .ToListAsync();
                return Ok(categories);
            }
            catch (Exception)
            {
                return Ok(new List<object>());
            }
        }

        [HttpGet("product-categories/{id}")]
        public async Task<IActionResult> GetProductCategory(int id)
        {
            try
            {
                var category = await _context.ProductCategories.FindAsync(id);
                if (category == null)
                    return NotFound(new { success = false, message = "Category not found" });

                return Ok(category);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("product-categories")]
        public async Task<IActionResult> CreateProductCategory([FromBody] ProductCategory category)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(category.CategoryName))
                    return BadRequest(new { success = false, message = "Category name is required" });

                category.CreatedAt = DateTime.UtcNow;
                category.UpdatedAt = DateTime.UtcNow;

                _context.ProductCategories.Add(category);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Category created successfully", categoryId = category.CategoryId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("product-categories/{id}")]
        public async Task<IActionResult> UpdateProductCategory(int id, [FromBody] ProductCategory category)
        {
            try
            {
                var existing = await _context.ProductCategories.FindAsync(id);
                if (existing == null)
                    return NotFound(new { success = false, message = "Category not found" });

                existing.CategoryName = category.CategoryName;
                existing.Description = category.Description;
                existing.DisplayOrder = category.DisplayOrder;
                existing.IsActive = category.IsActive;
                existing.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Category updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("product-categories/{id}/status")]
        public async Task<IActionResult> UpdateProductCategoryStatus(int id, [FromBody] StatusUpdateDto status)
        {
            try
            {
                Console.WriteLine($"[STATUS UPDATE] Category ID: {id}, New IsActive: {status?.IsActive}");
                
                if (status == null)
                {
                    Console.WriteLine("[STATUS UPDATE] Status payload is null!");
                    return BadRequest(new { success = false, message = "Invalid status payload" });
                }
                
                var category = await _context.ProductCategories.FindAsync(id);
                if (category == null)
                {
                    Console.WriteLine($"[STATUS UPDATE] Category not found: {id}");
                    return NotFound(new { success = false, message = "Category not found" });
                }

                Console.WriteLine($"[STATUS UPDATE] Found category: {category.CategoryName}, Current IsActive: {category.IsActive}");
                
                category.IsActive = status.IsActive;
                category.UpdatedAt = DateTime.UtcNow;

                _context.Entry(category).Property(c => c.IsActive).IsModified = true;
                _context.Entry(category).Property(c => c.UpdatedAt).IsModified = true;

                var changes = await _context.SaveChangesAsync();
                Console.WriteLine($"[STATUS UPDATE] SaveChanges returned: {changes} changes");

                return Ok(new { success = true, message = $"Category {(status.IsActive ? "activated" : "deactivated")} successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[STATUS UPDATE] Error: {ex.Message}");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("brands")]
        public async Task<IActionResult> GetBrands()
        {
            try
            {
                var brands = await _context.Brands.Where(b => b.IsActive).ToListAsync();
                return Ok(brands);
            }
            catch (Exception)
            {
                return Ok(new List<object>());
            }
        }

        [HttpGet("stock-alerts")]
        public async Task<IActionResult> GetStockAlerts()
        {
            try
            {
                var companyId = GetCompanyId();
                var alerts = await _context.StockAlerts
                    .Include(a => a.Product)
                    .Where(a => !a.IsResolved && (companyId == null || a.Product.CompanyId == companyId))
                    .OrderByDescending(a => a.CreatedAt)
                    .ToListAsync();

                return Ok(alerts);
            }
            catch (Exception)
            {
                return Ok(new List<object>());
            }
        }

        [HttpPost("stock-alerts")]
        public async Task<IActionResult> CreateStockAlert([FromBody] StockAlertRequest request)
        {
            try
            {
                var companyId = GetCompanyId();
                // Verify product belongs to company
                var product = await _context.Products.FindAsync(request.ProductId);
                if (product == null || (companyId != null && product.CompanyId != companyId))
                    return NotFound(new { success = false, message = "Product not found" });

                // Check if alert already exists for this product
                var existingAlert = await _context.StockAlerts
                    .FirstOrDefaultAsync(a => a.ProductId == request.ProductId && !a.IsResolved);

                if (existingAlert != null)
                {
                    // Update existing alert
                    existingAlert.CurrentStock = request.CurrentStock;
                    existingAlert.AlertType = request.AlertType;
                    await _context.SaveChangesAsync();
                    return Ok(new { success = true, message = "Alert updated", alertId = existingAlert.AlertId });
                }

                // Create new alert
                var alert = new StockAlert
                {
                    ProductId = request.ProductId,
                    AlertType = request.AlertType,
                    CurrentStock = request.CurrentStock,
                    ThresholdLevel = request.ThresholdLevel,
                    IsResolved = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.StockAlerts.Add(alert);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Stock alert created", alertId = alert.AlertId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPut("stock-alerts/{id}/resolve")]
        public async Task<IActionResult> ResolveStockAlert(int id)
        {
            try
            {
                var alert = await _context.StockAlerts.FindAsync(id);
                if (alert == null)
                    return NotFound(new { success = false, message = "Alert not found" });

                alert.IsResolved = true;
                alert.ResolvedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Alert resolved" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Purchase Orders

        [HttpGet("purchase-orders")]
        public async Task<IActionResult> GetPurchaseOrders()
        {
            try
            {
                var companyId = GetCompanyId();
                var orders = await _context.PurchaseOrders
                    .Where(po => companyId == null || po.CompanyId == companyId)
                    .Include(po => po.Supplier)
                    .Include(po => po.Items)
                    .OrderByDescending(po => po.OrderDate)
                    .Select(po => new
                    {
                        po.PurchaseOrderId,
                        PoNumber = "PO-" + po.PurchaseOrderId.ToString("D4"),
                        po.SupplierId,
                        SupplierName = po.Supplier != null ? po.Supplier.SupplierName : "",
                        po.OrderDate,
                        po.ExpectedDeliveryDate,
                        po.Status,
                        po.TotalAmount,
                        po.Notes,
                        ItemCount = po.Items.Count
                    })
                    .ToListAsync();

                return Ok(orders);
            }
            catch (Exception)
            {
                return Ok(new List<object>());
            }
        }

        [HttpGet("purchase-orders/{id}")]
        public async Task<IActionResult> GetPurchaseOrder(int id)
        {
            try
            {
                var companyId = GetCompanyId();
                var order = await _context.PurchaseOrders
                    .Include(po => po.Supplier)
                    .Include(po => po.Items)
                        .ThenInclude(i => i.Product)
                    .Where(po => po.PurchaseOrderId == id && (companyId == null || po.CompanyId == companyId))
                    .Select(po => new
                    {
                        po.PurchaseOrderId,
                        PoNumber = "PO-" + po.PurchaseOrderId.ToString("D4"),
                        po.SupplierId,
                        SupplierName = po.Supplier != null ? po.Supplier.SupplierName : "",
                        po.OrderDate,
                        po.ExpectedDeliveryDate,
                        po.ActualDeliveryDate,
                        po.Status,
                        po.TotalAmount,
                        po.Notes,
                        Items = po.Items.Select(i => new
                        {
                            i.PurchaseOrderItemId,
                            i.ProductId,
                            ProductName = i.Product != null ? i.Product.ProductName : "Unknown",
                            ProductSKU = i.Product != null ? i.Product.SKU : "",
                            i.Quantity,
                            i.UnitPrice,
                            i.Subtotal
                        })
                    })
                    .FirstOrDefaultAsync();

                if (order == null)
                    return NotFound(new { success = false, message = "Purchase order not found" });

                return Ok(new { success = true, data = order });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("purchase-orders")]
        public async Task<IActionResult> CreatePurchaseOrder([FromBody] PurchaseOrderRequest request)
        {
            try
            {
                var companyId = GetCompanyId();
                var purchaseOrder = new PurchaseOrder
                {
                    SupplierId = request.SupplierId,
                    OrderDate = DateTime.Parse(request.OrderDate),
                    ExpectedDeliveryDate = !string.IsNullOrEmpty(request.ExpectedDelivery) ? DateTime.Parse(request.ExpectedDelivery) : null,
                    Status = "Pending",
                    Notes = request.Notes,
                    CreatedAt = DateTime.UtcNow
                };
                purchaseOrder.CompanyId = companyId;

                decimal totalAmount = 0;
                purchaseOrder.Items = new List<PurchaseOrderItem>();

                foreach (var item in request.Items)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    var unitPrice = item.UnitPrice > 0 ? item.UnitPrice : (product?.CostPrice ?? 0);
                    var subtotal = unitPrice * item.Quantity;
                    totalAmount += subtotal;

                    purchaseOrder.Items.Add(new PurchaseOrderItem
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = unitPrice,
                        Subtotal = subtotal
                    });
                }

                purchaseOrder.TotalAmount = totalAmount;

                _context.PurchaseOrders.Add(purchaseOrder);
                await _context.SaveChangesAsync();

                return Ok(new { 
                    success = true, 
                    message = "Purchase order created successfully",
                    purchaseOrderId = purchaseOrder.PurchaseOrderId
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPut("purchase-orders/{id}/approve")]
        public async Task<IActionResult> ApprovePurchaseOrder(int id)
        {
            try
            {
                var companyId = GetCompanyId();
                var order = await _context.PurchaseOrders.FindAsync(id);
                if (order == null || (companyId != null && order.CompanyId != companyId))
                    return NotFound(new { success = false, message = "Purchase order not found" });

                order.Status = "Approved";
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Purchase order approved" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("purchase-orders/{id}/ship")]
        public async Task<IActionResult> ShipPurchaseOrder(int id)
        {
            try
            {
                var companyId = GetCompanyId();
                var order = await _context.PurchaseOrders.FindAsync(id);
                if (order == null || (companyId != null && order.CompanyId != companyId))
                    return NotFound(new { success = false, message = "Purchase order not found" });

                order.Status = "Shipped";
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Purchase order marked as shipped" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("purchase-orders/{id}/complete")]
        public async Task<IActionResult> CompletePurchaseOrder(int id)
        {
            try
            {
                var companyId = GetCompanyId();
                var order = await _context.PurchaseOrders
                    .Include(po => po.Items)
                    .FirstOrDefaultAsync(po => po.PurchaseOrderId == id && (companyId == null || po.CompanyId == companyId));

                if (order == null)
                    return NotFound(new { success = false, message = "Purchase order not found" });

                // Update stock levels for each item
                foreach (var item in order.Items)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        var previousStock = product.StockQuantity;
                        product.StockQuantity += item.Quantity;
                        product.UpdatedAt = DateTime.UtcNow;

                        // Create inventory transaction
                        _context.InventoryTransactions.Add(new InventoryTransaction
                        {
                            ProductId = item.ProductId,
                            TransactionType = "Stock In",
                            Quantity = item.Quantity,
                            PreviousStock = previousStock,
                            NewStock = product.StockQuantity,
                            UnitCost = item.UnitPrice,
                            TotalCost = item.Subtotal,
                            ReferenceType = "Purchase Order",
                            ReferenceId = order.PurchaseOrderId,
                            Notes = $"From PO-{order.PurchaseOrderId:D4}",
                            TransactionDate = DateTime.UtcNow
                        });

                        // Resolve any stock alerts for this product
                        var alerts = await _context.StockAlerts
                            .Where(a => a.ProductId == item.ProductId && !a.IsResolved)
                            .ToListAsync();

                        foreach (var alert in alerts)
                        {
                            if (product.StockQuantity > alert.ThresholdLevel)
                            {
                                alert.IsResolved = true;
                                alert.ResolvedAt = DateTime.UtcNow;
                            }
                        }
                    }
                }

                order.Status = "Completed";
                order.ActualDeliveryDate = DateTime.UtcNow;
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Purchase order completed and stock updated" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        #endregion

        #region Orders / Sales

        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders()
        {
            try
            {
                var companyId = GetCompanyId();
                var orders = await _context.Orders
                    .Where(o => companyId == null || o.CompanyId == companyId)
                    .Include(o => o.Customer)
                    .Include(o => o.OrderItems)
                    .OrderByDescending(o => o.OrderDate)
                    .Select(o => new
                    {
                        o.OrderId,
                        o.OrderNumber,
                        CustomerName = o.Customer != null ? o.Customer.FirstName + " " + o.Customer.LastName : "",
                        o.CustomerId,
                        o.OrderDate,
                        o.OrderStatus,
                        o.PaymentStatus,
                        o.Subtotal,
                        o.DiscountAmount,
                        o.TaxAmount,
                        o.ShippingAmount,
                        o.TotalAmount,
                        o.PaidAmount,
                        o.PaymentMethod,
                        o.PaymentReference,
                        o.ShippingAddress,
                        o.ShippingCity,
                        o.ShippingMethod,
                        o.TrackingNumber,
                        o.Notes,
                        o.ConfirmedAt,
                        ItemCount = o.OrderItems.Count,
                        Items = o.OrderItems.Select(i => new {
                            i.ProductName,
                            i.Quantity,
                            i.UnitPrice,
                            i.TotalPrice,
                            i.ProductCode
                        }),
                        o.CreatedAt
                    })
                    .ToListAsync();

                return Ok(orders);
            }
            catch (Exception)
            {
                return Ok(new List<object>());
            }
        }

        [HttpGet("orders/{id}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            try
            {
                var companyId = GetCompanyId();
                var order = await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderItems)
                        .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(o => o.OrderId == id && (companyId == null || o.CompanyId == companyId));

                if (order == null) return NotFound();
                return Ok(order);
            }
            catch (Exception)
            {
                return NotFound();
            }
        }

        [HttpPost("orders")]
        public async Task<IActionResult> CreateOrder([FromBody] Order order)
        {
            try
            {
                var companyId = GetCompanyId();
                order.CompanyId = companyId;
                order.OrderNumber = $"ORD-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
                order.OrderDate = DateTime.UtcNow;
                order.CreatedAt = DateTime.UtcNow;
                order.UpdatedAt = DateTime.UtcNow;

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Order created successfully", data = order });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("orders/{id}")]
        public async Task<IActionResult> UpdateOrder(int id, [FromBody] Order order)
        {
            if (!HasAdminOrderAccess())
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Only admins can update orders." });

            try
            {
                var companyId = GetCompanyId();
                var existing = await _context.Orders.FindAsync(id);
                if (existing == null || (companyId != null && existing.CompanyId != companyId)) return NotFound();

                existing.OrderStatus = order.OrderStatus;
                existing.PaymentStatus = order.PaymentStatus;
                existing.PaymentMethod = order.PaymentMethod;
                existing.ShippingMethod = order.ShippingMethod;
                existing.TrackingNumber = order.TrackingNumber;
                existing.Notes = order.Notes;
                existing.UpdatedAt = DateTime.UtcNow;

                // Update status timestamps
                if (order.OrderStatus == "Confirmed" && !existing.ConfirmedAt.HasValue)
                    existing.ConfirmedAt = DateTime.UtcNow;
                if (order.OrderStatus == "Shipped" && !existing.ShippedAt.HasValue)
                    existing.ShippedAt = DateTime.UtcNow;
                if (order.OrderStatus == "Delivered" && !existing.DeliveredAt.HasValue)
                    existing.DeliveredAt = DateTime.UtcNow;
                if (order.OrderStatus == "Cancelled" && !existing.CancelledAt.HasValue)
                    existing.CancelledAt = DateTime.UtcNow;

                if (order.OrderStatus == "Confirmed")
                {
                    existing.PaymentStatus = "Paid";
                    existing.PaidAmount = existing.TotalAmount;
                }

                var linkedInvoice = await _context.Invoices
                    .FirstOrDefaultAsync(i => i.OrderId == existing.OrderId && (companyId == null || i.CompanyId == companyId));

                if (linkedInvoice != null)
                {
                    SyncInvoiceFromOrderState(linkedInvoice, existing);
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Order updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPut("orders/{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] StatusUpdateRequest request)
        {
            if (!HasAdminOrderAccess())
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Only admins can update order status." });

            try
            {
                var companyId = GetCompanyId();
                var order = await _context.Orders
                    .AsTracking()
                    .FirstOrDefaultAsync(o => o.OrderId == id && (companyId == null || o.CompanyId == companyId));
                if (order == null) return NotFound();

                var previousStatus = order.OrderStatus;
                order.OrderStatus = request.Status;
                order.UpdatedAt = DateTime.UtcNow;

                // Update status timestamps
                if (request.Status == "Confirmed" && !order.ConfirmedAt.HasValue)
                    order.ConfirmedAt = DateTime.UtcNow;
                if (request.Status == "Shipped" && !order.ShippedAt.HasValue)
                    order.ShippedAt = DateTime.UtcNow;
                if (request.Status == "Delivered" && !order.DeliveredAt.HasValue)
                    order.DeliveredAt = DateTime.UtcNow;
                if (request.Status == "Cancelled" && !order.CancelledAt.HasValue)
                    order.CancelledAt = DateTime.UtcNow;

                // Business rule: once order is confirmed, payment is marked as paid
                if (request.Status == "Confirmed")
                {
                    order.PaymentStatus = "Paid";
                    order.PaidAmount = order.TotalAmount;
                }

                var linkedInvoice = await _context.Invoices
                    .FirstOrDefaultAsync(i => i.OrderId == order.OrderId && (companyId == null || i.CompanyId == companyId));

                if (linkedInvoice != null)
                {
                    SyncInvoiceFromOrderState(linkedInvoice, order);
                }

                // Create status history
                var history = new OrderStatusHistory
                {
                    OrderId = id,
                    PreviousStatus = previousStatus,
                    NewStatus = request.Status,
                    Notes = request.Notes,
                    ChangedAt = DateTime.UtcNow
                };

                _context.OrderStatusHistory.Add(history);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Order status updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPut("orders/{id}/approve")]
        public async Task<IActionResult> ApproveOrder(int id)
        {
            if (!HasAdminOrderAccess())
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Only admins can approve orders." });

            try
            {
                var companyId = GetCompanyId();
                var order = await _context.Orders
                    .AsTracking()
                    .Include(o => o.OrderItems)
                    .Include(o => o.Customer)
                    .FirstOrDefaultAsync(o => o.OrderId == id && (companyId == null || o.CompanyId == companyId));

                if (order == null) return NotFound(new { success = false, message = "Order not found" });

                if (order.OrderStatus != "Pending")
                    return BadRequest(new { success = false, message = "Only pending orders can be approved" });

                var previousStatus = order.OrderStatus;
                order.OrderStatus = "Confirmed";
                order.ConfirmedAt = DateTime.UtcNow;
                order.UpdatedAt = DateTime.UtcNow;
                order.PaymentStatus = "Paid";
                order.PaidAmount = order.TotalAmount;

                // Create status history
                _context.OrderStatusHistory.Add(new OrderStatusHistory
                {
                    OrderId = id,
                    PreviousStatus = previousStatus,
                    NewStatus = "Confirmed",
                    Notes = "Order approved by admin",
                    ChangedAt = DateTime.UtcNow
                });

                // Auto-generate invoice if one doesn't already exist
                var existingInvoice = await _context.Invoices.FirstOrDefaultAsync(i => i.OrderId == id);
                if (existingInvoice == null)
                {
                    var invoiceCount = await _context.Invoices.CountAsync() + 1;
                    var invoiceNumber = $"INV-{DateTime.Now:yyyyMMdd}-{invoiceCount:D4}";

                    var invoice = new Invoice
                    {
                        InvoiceNumber = invoiceNumber,
                        OrderId = order.OrderId,
                        CustomerId = order.CustomerId,
                        CompanyId = order.CompanyId,
                        InvoiceDate = DateTime.UtcNow,
                        DueDate = DateTime.UtcNow.AddDays(30),
                        Subtotal = order.Subtotal,
                        DiscountAmount = order.DiscountAmount,
                        TaxAmount = order.TaxAmount,
                        ShippingAmount = order.ShippingAmount,
                        TotalAmount = order.TotalAmount,
                        PaidAmount = order.PaidAmount,
                        BalanceDue = order.TotalAmount - order.PaidAmount,
                        Status = order.PaymentStatus == "Paid" ? "Paid" : "Pending",
                        BillingName = order.Customer != null ? $"{order.Customer.FirstName} {order.Customer.LastName}" : "Customer",
                        BillingAddress = order.BillingAddress ?? order.ShippingAddress,
                        BillingCity = order.BillingCity ?? order.ShippingCity,
                        BillingState = order.BillingState ?? order.ShippingState,
                        BillingZipCode = order.BillingZipCode ?? order.ShippingZipCode,
                        BillingCountry = order.BillingCountry ?? order.ShippingCountry ?? "Philippines",
                        BillingEmail = order.Customer?.Email,
                        PaymentTerms = "Due on Receipt",
                        Notes = $"Auto-generated from approved Order #{order.OrderNumber}",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    foreach (var oi in order.OrderItems)
                    {
                        invoice.Items.Add(new InvoiceItem
                        {
                            ProductId = oi.ProductId,
                            Description = oi.ProductName,
                            Quantity = oi.Quantity,
                            UnitPrice = oi.UnitPrice,
                            TaxAmount = (oi.TotalPrice / 1.12m) * 0.12m,
                            TotalPrice = oi.TotalPrice
                        });
                    }

                    _context.Invoices.Add(invoice);
                }
                else
                {
                    SyncInvoiceFromOrderState(existingInvoice, order);
                }

                await _context.SaveChangesAsync();

                // Create PayMongo Checkout Session for unpaid orders
                string? checkoutUrl = null;
                string? checkoutSessionId = null;
                if (order.PaymentStatus != "Paid")
                {
                    try
                    {
                        var paymongoSecretKey = _configuration["PayMongo:SecretKey"] ?? "sk_test_SakyRyg4R6hXeni4x5EaNUow";
                        using var httpClient = new HttpClient();
                        var authToken = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{paymongoSecretKey}:"));
                        httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {authToken}");

                        var amountInCentavos = (int)(order.TotalAmount * 100);
                        var lineItems = order.OrderItems.Select(oi => new
                        {
                            name = oi.ProductName ?? "Product",
                            quantity = oi.Quantity,
                            amount = (int)(oi.UnitPrice * 100),
                            currency = "PHP",
                            description = $"SKU: {oi.ProductCode ?? "N/A"}"
                        }).ToArray();

                        var callbackUrl = $"{Request.Scheme}://{Request.Host}/CustomerPortal/PaymentCallback?orderId={order.OrderId}";

                        var checkoutRequest = new
                        {
                            data = new
                            {
                                attributes = new
                                {
                                    send_email_receipt = true,
                                    show_description = true,
                                    show_line_items = true,
                                    description = $"CompuGear Order {order.OrderNumber}",
                                    line_items = lineItems,
                                    payment_method_types = new[] { "gcash", "card", "grab_pay", "paymaya" },
                                    success_url = callbackUrl,
                                    cancel_url = $"{Request.Scheme}://{Request.Host}/CustomerPortal/Orders"
                                }
                            }
                        };

                        var content = new StringContent(
                            System.Text.Json.JsonSerializer.Serialize(checkoutRequest),
                            System.Text.Encoding.UTF8,
                            "application/json"
                        );

                        var response = await httpClient.PostAsync("https://api.paymongo.com/v1/checkout_sessions", content);
                        var responseContent = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            var json = System.Text.Json.JsonDocument.Parse(responseContent);
                            checkoutSessionId = json.RootElement.GetProperty("data").GetProperty("id").GetString();
                            checkoutUrl = json.RootElement.GetProperty("data").GetProperty("attributes").GetProperty("checkout_url").GetString();

                            // Save reference
                            order.PaymentReference = checkoutSessionId;
                            order.UpdatedAt = DateTime.UtcNow;
                            await _context.SaveChangesAsync();
                        }
                    }
                    catch (Exception payEx)
                    {
                        // PayMongo error is non-fatal — order is still approved
                        return Ok(new { 
                            success = true, 
                            message = $"Order approved and invoice generated. PayMongo checkout could not be created: {payEx.Message}",
                            paymongoError = true 
                        });
                    }
                }

                return Ok(new { 
                    success = true, 
                    message = order.PaymentStatus == "Paid" 
                        ? "Order approved and invoice generated successfully" 
                        : "Order approved! PayMongo checkout link generated.",
                    checkoutUrl,
                    checkoutSessionId,
                    orderNumber = order.OrderNumber,
                    isPaid = order.PaymentStatus == "Paid"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPut("orders/{id}/reject")]
        public async Task<IActionResult> RejectOrder(int id, [FromBody] StatusUpdateRequest? request)
        {
            if (!HasAdminOrderAccess())
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Only admins can reject orders." });

            try
            {
                var companyId = GetCompanyId();
                var order = await _context.Orders
                    .AsTracking()
                    .FirstOrDefaultAsync(o => o.OrderId == id && (companyId == null || o.CompanyId == companyId));
                if (order == null) return NotFound(new { success = false, message = "Order not found" });

                if (order.OrderStatus != "Pending")
                    return BadRequest(new { success = false, message = "Only pending orders can be rejected" });

                var previousStatus = order.OrderStatus;
                order.OrderStatus = "Cancelled";
                order.CancelledAt = DateTime.UtcNow;
                order.UpdatedAt = DateTime.UtcNow;

                // Restore stock
                var orderItems = await _context.OrderItems.Where(oi => oi.OrderId == id).ToListAsync();
                foreach (var item in orderItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity += item.Quantity;
                    }
                }

                // Create status history
                _context.OrderStatusHistory.Add(new OrderStatusHistory
                {
                    OrderId = id,
                    PreviousStatus = previousStatus,
                    NewStatus = "Cancelled",
                    Notes = request?.Notes ?? "Order rejected by admin",
                    ChangedAt = DateTime.UtcNow
                });

                // Void linked invoice if exists
                var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.OrderId == id);
                if (invoice != null)
                {
                    invoice.Status = "Void";
                    invoice.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Order rejected and stock restored" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpDelete("orders/{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            if (!HasAdminOrderAccess())
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Only admins can delete orders." });

            var companyId = GetCompanyId();
            var order = await _context.Orders.FindAsync(id);
            if (order == null || (companyId != null && order.CompanyId != companyId)) return NotFound();

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Order deleted successfully" });
        }

        #endregion

        #region Leads

        [HttpGet("leads")]
        public async Task<IActionResult> GetLeads()
        {
            try
            {
                var companyId = GetCompanyId();
                var leads = await _context.Leads
                    .Where(l => companyId == null || l.CompanyId == companyId)
                    .OrderByDescending(l => l.CreatedAt)
                    .ToListAsync();

                return Ok(leads);
            }
            catch (Exception)
            {
                return Ok(new List<object>());
            }
        }

        [HttpGet("leads/{id}")]
        public async Task<IActionResult> GetLead(int id)
        {
            try
            {
                var companyId = GetCompanyId();
                var lead = await _context.Leads.FirstOrDefaultAsync(l => l.LeadId == id && (companyId == null || l.CompanyId == companyId));
                if (lead == null) return NotFound();
                return Ok(lead);
            }
            catch (Exception)
            {
                return NotFound();
            }
        }

        [HttpPost("leads")]
        public async Task<IActionResult> CreateLead([FromBody] Lead lead)
        {
            try
            {
                var companyId = GetCompanyId();
                lead.CompanyId = companyId;
                lead.LeadCode = $"LEAD-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
                lead.CreatedAt = DateTime.UtcNow;
                lead.UpdatedAt = DateTime.UtcNow;

                _context.Leads.Add(lead);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Lead created successfully", data = lead });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("leads/{id}")]
        public async Task<IActionResult> UpdateLead(int id, [FromBody] Lead lead)
        {
            try
            {
                var companyId = GetCompanyId();
                var existing = await _context.Leads.FindAsync(id);
                if (existing == null || (companyId != null && existing.CompanyId != companyId)) return NotFound();

                existing.FirstName = lead.FirstName;
                existing.LastName = lead.LastName;
                existing.Email = lead.Email;
                existing.Phone = lead.Phone;
                existing.CompanyName = lead.CompanyName;
                existing.Source = lead.Source;
                existing.Status = lead.Status;
                existing.Priority = lead.Priority;
                existing.EstimatedValue = lead.EstimatedValue;
                existing.Notes = lead.Notes;
                existing.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Lead updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("leads/{id}/convert")]
        public async Task<IActionResult> ConvertLead(int id)
        {
            try
            {
                var companyId = GetCompanyId();
                var lead = await _context.Leads.FindAsync(id);
                if (lead == null || (companyId != null && lead.CompanyId != companyId)) return NotFound();

                // Create customer from lead
                var customer = new Customer
                {
                    CustomerCode = $"CUST-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}",
                    FirstName = lead.FirstName,
                    LastName = lead.LastName,
                    Email = lead.Email ?? "",
                    Phone = lead.Phone,
                    CompanyName = lead.CompanyName,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                customer.CompanyId = lead.CompanyId;

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                lead.IsConverted = true;
                lead.ConvertedCustomerId = customer.CustomerId;
                lead.ConvertedAt = DateTime.UtcNow;
                lead.Status = "Won";
                lead.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Lead converted to customer successfully", customerId = customer.CustomerId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpDelete("leads/{id}")]
        public async Task<IActionResult> DeleteLead(int id)
        {
            var companyId = GetCompanyId();
            var lead = await _context.Leads.FindAsync(id);
            if (lead == null || (companyId != null && lead.CompanyId != companyId)) return NotFound();

            _context.Leads.Remove(lead);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Lead deleted successfully" });
        }

        [HttpPut("leads/{id}/toggle-status")]
        public async Task<IActionResult> ToggleLeadStatus(int id)
        {
            try
            {
                var companyId = GetCompanyId();
                var lead = await _context.Leads.FindAsync(id);
                if (lead == null || (companyId != null && lead.CompanyId != companyId)) return NotFound();

                lead.Status = lead.Status == "Active" || lead.Status == "New" || lead.Status == "Qualified" || lead.Status == "Hot" ? "Inactive" : "Active";
                lead.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = $"Lead {(lead.Status == "Inactive" ? "deactivated" : "activated")} successfully", status = lead.Status });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Support Tickets

        [HttpGet("tickets")]
        public async Task<IActionResult> GetTickets()
        {
            try
            {
                var companyId = GetCompanyId();
                var tickets = await _context.SupportTickets
                    .Where(t => companyId == null || t.CompanyId == companyId)
                    .Include(t => t.Customer)
                    .Include(t => t.Category)
                    .OrderByDescending(t => t.CreatedAt)
                    .Select(t => new
                    {
                        t.TicketId,
                        t.TicketNumber,
                        t.CustomerId,
                        t.AssignedTo,
                        CustomerName = t.Customer != null ? t.Customer.FirstName + " " + t.Customer.LastName : t.ContactName,
                        t.ContactEmail,
                        t.CategoryId,
                        CategoryName = t.Category != null ? t.Category.CategoryName : "",
                        t.Subject,
                        t.Description,
                        t.Priority,
                        t.Status,
                        t.CreatedAt,
                        t.ResolvedAt,
                        t.ClosedAt
                    })
                    .ToListAsync();

                return Ok(tickets);
            }
            catch (Exception)
            {
                return Ok(new List<object>());
            }
        }

        [HttpGet("tickets/{id}")]
        public async Task<IActionResult> GetTicket(int id)
        {
            try
            {
                var companyId = GetCompanyId();
                var ticket = await _context.SupportTickets
                    .Include(t => t.Customer)
                    .Include(t => t.Category)
                    .Include(t => t.Messages)
                    .FirstOrDefaultAsync(t => t.TicketId == id && (companyId == null || t.CompanyId == companyId));

                if (ticket == null) return NotFound();
                return Ok(ticket);
            }
            catch (Exception)
            {
                return NotFound();
            }
        }

        [HttpPost("tickets")]
        public async Task<IActionResult> CreateTicket([FromBody] SupportTicket ticket)
        {
            try
            {
                var companyId = GetCompanyId();
                ticket.CompanyId = companyId;
                ticket.TicketNumber = $"TKT-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
                ticket.CreatedAt = DateTime.UtcNow;
                ticket.UpdatedAt = DateTime.UtcNow;
                ticket.Status = "Open";

                _context.SupportTickets.Add(ticket);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Ticket created successfully", data = ticket });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("tickets/{id}")]
        public async Task<IActionResult> UpdateTicket(int id, [FromBody] SupportTicket ticket)
        {
            try
            {
                var companyId = GetCompanyId();
                var existing = await _context.SupportTickets.FindAsync(id);
                if (existing == null || (companyId != null && existing.CompanyId != companyId)) return NotFound();

                existing.Subject = ticket.Subject;
                existing.Description = ticket.Description;
                existing.Priority = ticket.Priority;
                existing.Status = ticket.Status;
                existing.CategoryId = ticket.CategoryId;
                existing.AssignedTo = ticket.AssignedTo;
                existing.UpdatedAt = DateTime.UtcNow;

                if (ticket.Status == "Resolved" && !existing.ResolvedAt.HasValue)
                    existing.ResolvedAt = DateTime.UtcNow;
                if (ticket.Status == "Closed" && !existing.ClosedAt.HasValue)
                    existing.ClosedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Ticket updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("tickets/{id}/reply")]
        public async Task<IActionResult> ReplyToTicket(int id, [FromBody] TicketMessage message)
        {
            try
            {
                var companyId = GetCompanyId();
                var ticket = await _context.SupportTickets.FindAsync(id);
                if (ticket == null || (companyId != null && ticket.CompanyId != companyId)) return NotFound();

                message.TicketId = id;
                message.CreatedAt = DateTime.UtcNow;
                message.SenderType = "Staff";

                _context.TicketMessages.Add(message);

                ticket.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Reply sent successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        // Support Staff respond endpoint - sends response and optionally updates status
        [HttpPost("tickets/{id}/respond")]
        public async Task<IActionResult> RespondToTicket(int id, [FromBody] TicketResponseRequest request)
        {
            try
            {
                var companyId = GetCompanyId();
                var ticket = await _context.SupportTickets.FindAsync(id);
                if (ticket == null || (companyId != null && ticket.CompanyId != companyId)) return NotFound();

                // Get current user info from session
                var userId = HttpContext.Session.GetInt32("UserId");
                var userName = HttpContext.Session.GetString("UserName") ?? "Support Staff";

                // Create the message
                var ticketMessage = new TicketMessage
                {
                    TicketId = id,
                    Message = request.Message,
                    SenderType = "Staff",
                    SenderId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.TicketMessages.Add(ticketMessage);

                // Update status if provided
                if (!string.IsNullOrEmpty(request.Status))
                {
                    ticket.Status = request.Status;
                    if (request.Status == "Resolved" && !ticket.ResolvedAt.HasValue)
                        ticket.ResolvedAt = DateTime.UtcNow;
                    if (request.Status == "Closed" && !ticket.ClosedAt.HasValue)
                        ticket.ClosedAt = DateTime.UtcNow;
                }

                ticket.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Response sent successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        // Support Staff escalate ticket to admin
        [HttpPost("tickets/{id}/escalate")]
        public async Task<IActionResult> EscalateTicket(int id, [FromBody] TicketEscalationRequest request)
        {
            try
            {
                var companyId = GetCompanyId();
                var ticket = await _context.SupportTickets.FindAsync(id);
                if (ticket == null || (companyId != null && ticket.CompanyId != companyId)) return NotFound();

                var userId = HttpContext.Session.GetInt32("UserId");

                // Update ticket status to Pending Approval
                ticket.Status = "Pending Approval";
                ticket.UpdatedAt = DateTime.UtcNow;

                // Add an internal note/message about escalation
                var escalationNote = new TicketMessage
                {
                    TicketId = id,
                    Message = $"[ESCALATED TO ADMIN]\nReason: {request.Reason}\nNotes: {request.Notes ?? "N/A"}",
                    SenderType = "System",
                    SenderId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.TicketMessages.Add(escalationNote);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Ticket escalated to Admin for approval" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpDelete("tickets/{id}")]
        public async Task<IActionResult> DeleteTicket(int id)
        {
            var companyId = GetCompanyId();
            var ticket = await _context.SupportTickets.FindAsync(id);
            if (ticket == null || (companyId != null && ticket.CompanyId != companyId)) return NotFound();

            _context.SupportTickets.Remove(ticket);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Ticket deleted successfully" });
        }

        [HttpGet("ticket-categories")]
        public async Task<IActionResult> GetTicketCategories()
        {
            try
            {
                var categories = await _context.TicketCategories.Where(c => c.IsActive).ToListAsync();
                return Ok(categories);
            }
            catch (Exception)
            {
                return Ok(new List<object>());
            }
        }

        #endregion

        #region Users

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var companyId = GetCompanyId();
                var users = await _context.Users
                    .Include(u => u.Role)
                    .Where(u => (companyId == null || u.CompanyId == companyId) && (companyId == null || u.RoleId != 1))
                    .OrderByDescending(u => u.CreatedAt)
                    .Select(u => new
                    {
                        u.UserId,
                        u.Username,
                        u.Email,
                        u.FirstName,
                        u.LastName,
                        FullName = u.FirstName + " " + u.LastName,
                        u.Phone,
                        u.IsActive,
                        RoleName = u.Role != null ? u.Role.RoleName : "Staff",
                        u.RoleId,
                        u.LastLoginAt,
                        u.CreatedAt
                    })
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var companyId = GetCompanyId();
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == id && (companyId == null || u.CompanyId == companyId));
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] User user)
        {
            try
            {
                // Check if username or email already exists
                if (await _context.Users.AnyAsync(u => u.Username == user.Username))
                    return BadRequest(new { success = false, message = "Username already exists" });

                if (await _context.Users.AnyAsync(u => u.Email == user.Email))
                    return BadRequest(new { success = false, message = "Email already exists" });

                // Simple password hashing (in production use BCrypt or similar)
                if (!string.IsNullOrEmpty(user.Password))
                {
                    user.Salt = Guid.NewGuid().ToString("N").Substring(0, 16);
                    user.PasswordHash = Convert.ToBase64String(
                        System.Security.Cryptography.SHA256.HashData(
                            System.Text.Encoding.UTF8.GetBytes(user.Password + user.Salt)));
                }
                else
                {
                    return BadRequest(new { success = false, message = "Password is required" });
                }

                var companyId = GetCompanyId();
                user.CompanyId = companyId;

                // Set default role if not provided
                if (user.RoleId == 0)
                {
                    user.RoleId = 3; // Default to Sales Staff
                }

                user.CreatedAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "User created successfully", data = new { user.UserId, user.Username, user.Email, user.FullName } });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] User user)
        {
            try
            {
                var companyId = GetCompanyId();
                var existing = await _context.Users.FindAsync(id);
                if (existing == null || (companyId != null && existing.CompanyId != companyId)) return NotFound();

                existing.FirstName = user.FirstName;
                existing.LastName = user.LastName;
                existing.Email = user.Email;
                existing.Phone = user.Phone;
                existing.RoleId = user.RoleId;
                existing.IsActive = user.IsActive;
                existing.UpdatedAt = DateTime.UtcNow;

                // Update password if provided
                if (!string.IsNullOrEmpty(user.Password))
                {
                    existing.Salt = Guid.NewGuid().ToString("N").Substring(0, 16);
                    existing.PasswordHash = Convert.ToBase64String(
                        System.Security.Cryptography.SHA256.HashData(
                            System.Text.Encoding.UTF8.GetBytes(user.Password + existing.Salt)));
                    existing.PasswordChangedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "User updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPut("users/{id}/toggle-status")]
        public async Task<IActionResult> ToggleUserStatus(int id)
        {
            try
            {
                var companyId = GetCompanyId();
                var user = await _context.Users.FindAsync(id);
                if (user == null || (companyId != null && user.CompanyId != companyId)) return NotFound();

                user.IsActive = !user.IsActive;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = $"User {(user.IsActive ? "activated" : "deactivated")} successfully", isActive = user.IsActive });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var companyId = GetCompanyId();
            var user = await _context.Users.FindAsync(id);
            if (user == null || (companyId != null && user.CompanyId != companyId)) return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "User deleted successfully" });
        }

        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles()
        {
            try
            {
                var roles = await _context.Roles.ToListAsync();
                
                // If no roles exist, return default roles
                if (!roles.Any())
                {
                    return Ok(new[]
                    {
                        new { roleId = 1, roleName = "Super Admin", isActive = true },
                        new { roleId = 2, roleName = "Company Admin", isActive = true },
                        new { roleId = 3, roleName = "Sales Staff", isActive = true },
                        new { roleId = 4, roleName = "Customer Support Staff", isActive = true },
                        new { roleId = 5, roleName = "Marketing Staff", isActive = true },
                        new { roleId = 6, roleName = "Accounting & Billing Staff", isActive = true }
                    });
                }
                
                return Ok(roles);
            }
            catch (Exception)
            {
                // Return default roles if database error
                return Ok(new[]
                {
                    new { roleId = 1, roleName = "Super Admin", isActive = true },
                    new { roleId = 2, roleName = "Company Admin", isActive = true },
                    new { roleId = 3, roleName = "Sales Staff", isActive = true },
                    new { roleId = 4, roleName = "Customer Support Staff", isActive = true },
                    new { roleId = 5, roleName = "Marketing Staff", isActive = true },
                    new { roleId = 6, roleName = "Accounting & Billing Staff", isActive = true }
                });
            }
        }

        #endregion

        #region Billing - Invoices (Create/Update/Delete/PDF/Financial)

        [HttpPost("invoices")]
        public async Task<IActionResult> CreateInvoice([FromBody] Invoice invoice)
        {
            try
            {
                if (!HasFullBillingAccess())
                    return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Invoices are read-only. Update order status instead." });

                var companyId = GetCompanyId();
                invoice.CompanyId = companyId;
                invoice.InvoiceNumber = $"INV-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
                invoice.InvoiceDate = DateTime.UtcNow;
                invoice.CreatedAt = DateTime.UtcNow;
                invoice.UpdatedAt = DateTime.UtcNow;

                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Invoice created successfully", data = invoice });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("invoices/{id}")]
        public async Task<IActionResult> UpdateInvoice(int id, [FromBody] Invoice invoice)
        {
            try
            {
                if (!HasFullBillingAccess())
                    return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Invoices are read-only. Update order status instead." });

                var companyId = GetCompanyId();
                var existing = await _context.Invoices.FindAsync(id);
                if (existing == null || (companyId != null && existing.CompanyId != companyId)) return NotFound();

                existing.Status = invoice.Status;
                existing.DueDate = invoice.DueDate;
                existing.Notes = invoice.Notes;
                existing.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Invoice updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("invoices/{id}")]
        public async Task<IActionResult> DeleteInvoice(int id)
        {
            try
            {
                if (!HasFullBillingAccess())
                    return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Invoices are read-only. Update order status instead." });

                var companyId = GetCompanyId();
                var invoice = await _context.Invoices.FindAsync(id);
                if (invoice == null || (companyId != null && invoice.CompanyId != companyId)) return NotFound();

                _context.Invoices.Remove(invoice);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Invoice deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // GET: /api/invoices/{id}/pdf - Generate PDF-ready HTML for invoice
        [HttpGet("invoices/{id}/pdf")]
        public async Task<IActionResult> GetInvoicePdf(int id)
        {
            try
            {
                var companyId = GetCompanyId();

                if (id < 0)
                {
                    var orderId = -id;
                    var order = await _context.Orders
                        .Include(o => o.Customer)
                        .Include(o => o.OrderItems)
                        .FirstOrDefaultAsync(o => o.OrderId == orderId && (companyId == null || o.CompanyId == companyId));

                    if (order == null)
                        return NotFound(new { success = false, message = "Invoice not found" });

                    var orderPayments = await _context.Payments
                        .Where(p => p.OrderId == order.OrderId && (companyId == null || p.CompanyId == companyId))
                        .OrderBy(p => p.PaymentDate)
                        .Select(p => new
                        {
                            p.PaymentNumber,
                            p.PaymentDate,
                            p.Amount,
                            p.PaymentMethodType,
                            p.ReferenceNumber,
                            p.Status
                        })
                        .ToListAsync();

                    var derivedStatus = order.OrderStatus == "Confirmed" && order.PaymentStatus == "Paid"
                        ? "Paid/Confirmed"
                        : (order.PaidAmount >= order.TotalAmount ? "Paid" : (order.PaidAmount > 0 ? "Partial" : "Pending"));

                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            InvoiceId = -order.OrderId,
                            InvoiceNumber = "ORD-" + order.OrderNumber,
                            InvoiceDate = order.OrderDate,
                            DueDate = order.OrderDate,
                            Status = derivedStatus,
                            Subtotal = order.Subtotal,
                            DiscountAmount = order.DiscountAmount,
                            TaxAmount = order.TaxAmount,
                            ShippingAmount = order.ShippingAmount,
                            TotalAmount = order.TotalAmount,
                            PaidAmount = order.PaidAmount,
                            BalanceDue = Math.Max(0, order.TotalAmount - order.PaidAmount),
                            BillingName = order.Customer != null ? order.Customer.FirstName + " " + order.Customer.LastName : "Customer",
                            order.BillingAddress,
                            order.BillingCity,
                            order.BillingState,
                            order.BillingZipCode,
                            BillingCountry = order.BillingCountry ?? "Philippines",
                            BillingEmail = order.Customer?.Email,
                            PaymentTerms = "Order Based",
                            order.Notes,
                            OrderNumber = order.OrderNumber,
                            OrderDate = order.OrderDate,
                            OrderStatus = order.OrderStatus,
                            OrderPaymentStatus = order.PaymentStatus,
                            OrderPaymentMethod = order.PaymentMethod,
                            OrderTrackingNumber = order.TrackingNumber,
                            OrderShippingMethod = order.ShippingMethod,
                            OrderConfirmedAt = order.ConfirmedAt,
                            CustomerName = order.Customer != null ? order.Customer.FirstName + " " + order.Customer.LastName : "Customer",
                            CustomerEmail = order.Customer?.Email,
                            CustomerPhone = order.Customer?.Phone,
                            Items = order.OrderItems.Select(item => new
                            {
                                Description = item.ProductName,
                                item.Quantity,
                                item.UnitPrice,
                                DiscountAmount = item.DiscountAmount,
                                TaxAmount = item.TaxAmount,
                                TotalPrice = item.TotalPrice,
                                ProductCode = item.ProductCode
                            }),
                            Payments = orderPayments
                        }
                    });
                }

                var invoice = await _context.Invoices
                    .Include(i => i.Customer)
                    .Include(i => i.Items)
                        .ThenInclude(item => item.Product)
                    .Include(i => i.Order)
                    .FirstOrDefaultAsync(i => i.InvoiceId == id && (companyId == null || i.CompanyId == companyId));

                if (invoice == null) return NotFound(new { success = false, message = "Invoice not found" });

                // Get payments for this invoice
                var payments = await _context.Payments
                    .Where(p => p.InvoiceId == id && (companyId == null || p.CompanyId == companyId))
                    .OrderBy(p => p.PaymentDate)
                    .Select(p => new
                    {
                        p.PaymentNumber,
                        p.PaymentDate,
                        p.Amount,
                        p.PaymentMethodType,
                        p.ReferenceNumber,
                        p.Status
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        invoice.InvoiceId,
                        invoice.InvoiceNumber,
                        invoice.InvoiceDate,
                        invoice.DueDate,
                        Status = invoice.Order != null && invoice.Order.OrderStatus == "Confirmed" && invoice.Order.PaymentStatus == "Paid"
                            ? "Paid/Confirmed"
                            : invoice.Status,
                        invoice.Subtotal,
                        invoice.DiscountAmount,
                        invoice.TaxAmount,
                        invoice.ShippingAmount,
                        invoice.TotalAmount,
                        PaidAmount = invoice.Order != null && invoice.Order.OrderStatus == "Confirmed" && invoice.Order.PaymentStatus == "Paid"
                            ? invoice.TotalAmount
                            : invoice.PaidAmount,
                        BalanceDue = invoice.Order != null && invoice.Order.OrderStatus == "Confirmed" && invoice.Order.PaymentStatus == "Paid"
                            ? 0
                            : Math.Max(0, invoice.TotalAmount - invoice.PaidAmount),
                        invoice.BillingName,
                        invoice.BillingAddress,
                        invoice.BillingCity,
                        invoice.BillingState,
                        invoice.BillingZipCode,
                        invoice.BillingCountry,
                        invoice.BillingEmail,
                        invoice.PaymentTerms,
                        invoice.Notes,
                        OrderNumber = invoice.Order?.OrderNumber,
                        OrderDate = invoice.Order != null ? invoice.Order.OrderDate : (DateTime?)null,
                        OrderStatus = invoice.Order?.OrderStatus,
                        OrderPaymentStatus = invoice.Order?.PaymentStatus,
                        OrderPaymentMethod = invoice.Order?.PaymentMethod,
                        OrderTrackingNumber = invoice.Order?.TrackingNumber,
                        OrderShippingMethod = invoice.Order?.ShippingMethod,
                        OrderConfirmedAt = invoice.Order?.ConfirmedAt,
                        CustomerName = invoice.Customer != null
                            ? invoice.Customer.FirstName + " " + invoice.Customer.LastName
                            : invoice.BillingName,
                        CustomerEmail = invoice.Customer?.Email ?? invoice.BillingEmail,
                        CustomerPhone = invoice.Customer?.Phone,
                        Items = invoice.Items.Select(item => new
                        {
                            item.Description,
                            item.Quantity,
                            item.UnitPrice,
                            item.DiscountAmount,
                            item.TaxAmount,
                            item.TotalPrice,
                            ProductCode = item.Product?.ProductCode
                        }),
                        Payments = payments
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // GET: /api/reports/financial - Financial report data
        [HttpGet("reports/financial")]
        public async Task<IActionResult> GetFinancialReport(string period = "month")
        {
            try
            {
                var now = DateTime.UtcNow;
                DateTime startDate = period switch
                {
                    "week" => now.AddDays(-7),
                    "year" => new DateTime(now.Year, 1, 1),
                    _ => new DateTime(now.Year, now.Month, 1) // month
                };

                var companyId = GetCompanyId();

                var invoices = await _context.Invoices
                    .Where(i => i.InvoiceDate >= startDate && (companyId == null || i.CompanyId == companyId))
                    .ToListAsync();

                var payments = await _context.Payments
                    .Where(p => p.PaymentDate >= startDate && p.Status == "Completed" && (companyId == null || p.CompanyId == companyId))
                    .ToListAsync();

                var orders = await _context.Orders
                    .Where(o => o.OrderDate >= startDate && (companyId == null || o.CompanyId == companyId))
                    .ToListAsync();

                // Monthly breakdown (12 months)
                var monthlyRevenue = new decimal[12];
                var monthlyInvoiced = new decimal[12];
                var monthlyCollected = new decimal[12];

                foreach (var o in await _context.Orders.Where(o => o.OrderDate.Year == now.Year && (companyId == null || o.CompanyId == companyId)).ToListAsync())
                    monthlyRevenue[o.OrderDate.Month - 1] += o.TotalAmount;

                foreach (var i in await _context.Invoices.Where(i => i.InvoiceDate.Year == now.Year && (companyId == null || i.CompanyId == companyId)).ToListAsync())
                    monthlyInvoiced[i.InvoiceDate.Month - 1] += i.TotalAmount;

                foreach (var p in await _context.Payments.Where(p => p.PaymentDate.Year == now.Year && p.Status == "Completed" && (companyId == null || p.CompanyId == companyId)).ToListAsync())
                    monthlyCollected[p.PaymentDate.Month - 1] += p.Amount;

                // Payment method breakdown
                var paymentMethods = payments
                    .GroupBy(p => p.PaymentMethodType)
                    .Select(g => new { Method = g.Key, Amount = g.Sum(p => p.Amount), Count = g.Count() })
                    .ToList();

                // Invoice status breakdown
                var allInvoices = await _context.Invoices
                    .Include(i => i.Order)
                    .Where(i => companyId == null || i.CompanyId == companyId)
                    .ToListAsync();

                var normalizedInvoices = allInvoices.Select(i => new
                {
                    EffectiveStatus = i.Order != null && i.Order.OrderStatus == "Confirmed" && i.Order.PaymentStatus == "Paid"
                        ? "Paid/Confirmed"
                        : (i.PaidAmount >= i.TotalAmount
                            ? "Paid"
                            : (i.PaidAmount > 0 && i.Status != "Cancelled" && i.Status != "Void" ? "Partial" : i.Status)),
                    i.TotalAmount,
                    EffectiveBalance = i.Order != null && i.Order.OrderStatus == "Confirmed" && i.Order.PaymentStatus == "Paid"
                        ? 0
                        : Math.Max(0, i.TotalAmount - i.PaidAmount)
                }).ToList();

                var invoiceStatusBreakdown = normalizedInvoices
                    .GroupBy(i => i.EffectiveStatus)
                    .Select(g => new { Status = g.Key, Count = g.Count(), Amount = g.Sum(i => i.TotalAmount) })
                    .ToList();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        totalRevenue = orders.Sum(o => o.TotalAmount),
                        totalInvoiced = invoices.Sum(i => i.TotalAmount),
                        totalCollected = payments.Sum(p => p.Amount),
                        outstanding = normalizedInvoices.Where(i => i.EffectiveStatus != "Paid" && i.EffectiveStatus != "Paid/Confirmed" && i.EffectiveStatus != "Cancelled").Sum(i => i.EffectiveBalance),
                        invoiceCount = normalizedInvoices.Count,
                        paymentCount = payments.Count,
                        monthlyRevenue,
                        monthlyInvoiced,
                        monthlyCollected,
                        paymentMethods,
                        invoiceStatusBreakdown,
                        topCustomers = await _context.Customers
                            .Where(c => companyId == null || c.CompanyId == companyId)
                            .OrderByDescending(c => c.TotalSpent)
                            .Take(5)
                            .Select(c => new { c.CustomerId, Name = c.FirstName + " " + c.LastName, c.TotalSpent, c.TotalOrders })
                            .ToListAsync()
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Billing - Payments (Create)

        [HttpPost("payments")]
        public async Task<IActionResult> CreatePayment([FromBody] Payment payment)
        {
            try
            {
                var companyId = GetCompanyId();

                Invoice? invoice = null;
                Order? order = null;

                if (payment.InvoiceId.HasValue)
                {
                    invoice = await _context.Invoices
                        .FirstOrDefaultAsync(i => i.InvoiceId == payment.InvoiceId.Value &&
                                                  (companyId == null || i.CompanyId == companyId));

                    if (invoice == null)
                        return BadRequest(new { success = false, message = "Linked invoice not found" });

                    payment.CustomerId = payment.CustomerId > 0 ? payment.CustomerId : invoice.CustomerId;
                    payment.OrderId ??= invoice.OrderId;
                    payment.CompanyId = companyId ?? invoice.CompanyId;
                }

                if (payment.OrderId.HasValue)
                {
                    order = await _context.Orders
                        .FirstOrDefaultAsync(o => o.OrderId == payment.OrderId.Value &&
                                                  (companyId == null || o.CompanyId == companyId));

                    if (order != null)
                    {
                        payment.CustomerId = payment.CustomerId > 0 ? payment.CustomerId : order.CustomerId;
                        payment.CompanyId ??= companyId ?? order.CompanyId;
                    }
                }

                if (payment.CustomerId <= 0)
                    return BadRequest(new { success = false, message = "Customer is required for payment" });

                payment.CompanyId ??= companyId;
                payment.PaymentNumber = $"PAY-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
                if (payment.PaymentDate == default)
                    payment.PaymentDate = DateTime.UtcNow;
                if (string.IsNullOrWhiteSpace(payment.Status))
                    payment.Status = "Completed";
                payment.CreatedAt = DateTime.UtcNow;

                _context.Payments.Add(payment);

                // Update invoice paid amount
                if (invoice != null)
                {
                    invoice.PaidAmount += payment.Amount;
                    invoice.PaidAmount = Math.Min(invoice.PaidAmount, invoice.TotalAmount);
                    invoice.BalanceDue = Math.Max(0, invoice.TotalAmount - invoice.PaidAmount);

                    if (invoice.PaidAmount >= invoice.TotalAmount)
                    {
                        invoice.Status = "Paid";
                        invoice.PaidAt = DateTime.UtcNow;
                    }
                    else if (invoice.PaidAmount > 0)
                        invoice.Status = "Partial";
                }

                if (order == null && payment.OrderId.HasValue)
                {
                    order = await _context.Orders
                        .FirstOrDefaultAsync(o => o.OrderId == payment.OrderId.Value &&
                                                  (companyId == null || o.CompanyId == companyId));
                }

                if (order != null)
                {
                    order.PaidAmount += payment.Amount;
                    order.PaidAmount = Math.Min(order.PaidAmount, order.TotalAmount);
                    order.PaymentStatus = order.PaidAmount >= order.TotalAmount ? "Paid" : "Partial";

                    if (order.OrderStatus == "Pending" && order.PaymentStatus == "Paid")
                    {
                        order.OrderStatus = "Confirmed";
                        order.ConfirmedAt = DateTime.UtcNow;
                    }

                    order.UpdatedAt = DateTime.UtcNow;

                    var linkedInvoice = invoice ?? await _context.Invoices
                        .FirstOrDefaultAsync(i => i.OrderId == order.OrderId && (companyId == null || i.CompanyId == companyId));
                    if (linkedInvoice != null)
                    {
                        SyncInvoiceFromOrderState(linkedInvoice, order);
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Payment recorded successfully", data = payment });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Supplier Products

        [HttpGet("suppliers/{id}/products")]
        public async Task<IActionResult> GetSupplierProducts(int id)
        {
            try
            {
                var companyId = GetCompanyId();
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Where(p => p.SupplierId == id && (companyId == null || p.CompanyId == companyId))
                    .OrderBy(p => p.ProductName)
                    .Select(p => new
                    {
                        p.ProductId,
                        p.ProductCode,
                        p.SKU,
                        p.ProductName,
                        p.ShortDescription,
                        CategoryName = p.Category != null ? p.Category.CategoryName : "",
                        BrandName = p.Brand != null ? p.Brand.BrandName : "",
                        p.CostPrice,
                        p.SellingPrice,
                        p.StockQuantity,
                        p.ReorderLevel,
                        p.Status,
                        p.MainImageUrl
                    })
                    .ToListAsync();

                return Ok(products);
            }
            catch (Exception)
            {
                return Ok(new List<object>());
            }
        }

        #endregion

        #region Stock Adjustments

        [HttpPost("stock-adjustments")]
        public async Task<IActionResult> CreateStockAdjustment([FromBody] StockAdjustmentRequest request)
        {
            try
            {
                var companyId = GetCompanyId();
                var product = await _context.Products
                    .AsTracking()
                    .FirstOrDefaultAsync(p => p.ProductId == request.ProductId && (companyId == null || p.CompanyId == companyId));
                if (product == null)
                    return NotFound(new { success = false, message = "Product not found" });

                var previousStock = product.StockQuantity;
                var adjustmentQuantity = request.AdjustmentType == "Add" ? request.Quantity : -request.Quantity;
                var newStock = previousStock + adjustmentQuantity;

                if (newStock < 0)
                    return BadRequest(new { success = false, message = "Stock cannot be negative" });

                product.StockQuantity = newStock;
                product.UpdatedAt = DateTime.UtcNow;

                // Create inventory transaction
                var transaction = new InventoryTransaction
                {
                    ProductId = request.ProductId,
                    TransactionType = request.AdjustmentType == "Add" ? "Stock In" : "Stock Out",
                    Quantity = adjustmentQuantity,
                    PreviousStock = previousStock,
                    NewStock = newStock,
                    UnitCost = product.CostPrice,
                    TotalCost = Math.Abs(adjustmentQuantity) * product.CostPrice,
                    ReferenceType = "Stock Adjustment",
                    Notes = request.Notes,
                    TransactionDate = DateTime.UtcNow
                };

                _context.InventoryTransactions.Add(transaction);
                await _context.SaveChangesAsync();

                return Ok(new { 
                    success = true, 
                    message = $"Stock adjusted successfully. New stock: {newStock}",
                    data = new {
                        productId = product.ProductId,
                        productName = product.ProductName,
                        previousStock,
                        adjustmentQuantity,
                        newStock
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpGet("stock-adjustments")]
        public async Task<IActionResult> GetStockAdjustments()
        {
            try
            {
                var companyId = GetCompanyId();
                var adjustments = await _context.InventoryTransactions
                    .Include(t => t.Product)
                    .Where(t => t.ReferenceType == "Stock Adjustment" && (companyId == null || t.Product.CompanyId == companyId))
                    .OrderByDescending(t => t.TransactionDate)
                    .Select(t => new
                    {
                        t.TransactionId,
                        t.ProductId,
                        ProductName = t.Product.ProductName,
                        ProductImage = t.Product.MainImageUrl,
                        t.TransactionType,
                        t.Quantity,
                        t.PreviousStock,
                        t.NewStock,
                        t.UnitCost,
                        t.TotalCost,
                        t.Notes,
                        t.TransactionDate
                    })
                    .ToListAsync();

                return Ok(adjustments);
            }
            catch (Exception)
            {
                return Ok(new List<object>());
            }
        }

        #endregion

        #region Dashboard Stats

        [HttpGet("dashboard/stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var today = DateTime.Today;
            var thisMonth = new DateTime(today.Year, today.Month, 1);

            var companyId = GetCompanyId();

            var stats = new
            {
                TotalCustomers = await _context.Customers.CountAsync(c => companyId == null || c.CompanyId == companyId),
                TotalOrders = await _context.Orders.CountAsync(o => companyId == null || o.CompanyId == companyId),
                TotalRevenue = await _context.Orders.Where(o => o.PaymentStatus == "Paid" && (companyId == null || o.CompanyId == companyId)).SumAsync(o => o.TotalAmount),
                PendingOrders = await _context.Orders.CountAsync(o => o.OrderStatus == "Pending" && (companyId == null || o.CompanyId == companyId)),
                OpenTickets = await _context.SupportTickets.CountAsync(t => (t.Status == "Open" || t.Status == "In Progress") && (companyId == null || t.CompanyId == companyId)),
                LowStockProducts = await _context.Products.CountAsync(p => p.StockQuantity <= p.ReorderLevel && (companyId == null || p.CompanyId == companyId)),
                ActiveCampaigns = await _context.Campaigns.CountAsync(c => c.Status == "Active" && (companyId == null || c.CompanyId == companyId)),
                MonthlyRevenue = await _context.Orders
                    .Where(o => o.OrderDate >= thisMonth && o.PaymentStatus == "Paid" && (companyId == null || o.CompanyId == companyId))
                    .SumAsync(o => o.TotalAmount)
            };

            return Ok(stats);
        }

        #endregion

        #region Image Upload

        [HttpPost("upload-image")]
        public async Task<IActionResult> UploadImage(IFormFile file, [FromQuery] string folder = "products")
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { success = false, message = "No file uploaded" });

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                    return BadRequest(new { success = false, message = "Invalid file type. Only images are allowed." });

                // Validate file size (max 5MB)
                if (file.Length > 5 * 1024 * 1024)
                    return BadRequest(new { success = false, message = "File size must be less than 5MB" });

                // Create folder path
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", folder);
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                // Generate unique filename
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Return the URL path
                var imageUrl = $"/images/{folder}/{uniqueFileName}";
                return Ok(new { success = true, message = "Image uploaded successfully", imageUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "Failed to upload image: " + ex.Message });
            }
        }

        [HttpDelete("delete-image")]
        public IActionResult DeleteImage([FromQuery] string imageUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(imageUrl))
                    return BadRequest(new { success = false, message = "No image URL provided" });

                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", imageUrl.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    return Ok(new { success = true, message = "Image deleted successfully" });
                }

                return NotFound(new { success = false, message = "Image not found" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "Failed to delete image: " + ex.Message });
            }
        }

        #endregion

        #region Live Chat Support

        // Get current logged in user info
        [HttpGet("GetCurrentUser")]
        public IActionResult GetCurrentUser()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");
            var userName = HttpContext.Session.GetString("UserName");
            var fullName = HttpContext.Session.GetString("FullName");

            if (userId == null)
                return Ok(new { success = false, message = "Not logged in" });

            return Ok(new { 
                success = true, 
                data = new { 
                    userId = userId, 
                    roleId = roleId, 
                    userName = userName, 
                    fullName = fullName 
                } 
            });
        }

        // Get all active chat sessions (for support staff)
        [HttpGet("GetActiveChatSessions")]
        public async Task<IActionResult> GetActiveChatSessions()
        {
            try
            {
                var currentUserId = HttpContext.Session.GetInt32("UserId");
                
                var sessions = await _context.ChatSessions
                    .Include(s => s.Customer)
                    .Where(s => s.Status == "Active" || s.Status == "Transferred" || s.Status == "Pending")
                    .OrderByDescending(s => s.StartedAt)
                    .Select(s => new
                    {
                        s.SessionId,
                        CustomerName = s.Customer != null ? s.Customer.FullName : "Guest",
                        CustomerEmail = s.Customer != null ? s.Customer.Email : "",
                        s.CustomerId,
                        s.Status,
                        s.TotalMessages,
                        s.StartedAt,
                        s.AgentId,
                        LastMessageAt = _context.ChatMessages
                            .Where(m => m.SessionId == s.SessionId)
                            .OrderByDescending(m => m.CreatedAt)
                            .Select(m => m.CreatedAt)
                            .FirstOrDefault(),
                        UnreadCount = _context.ChatMessages
                            .Count(m => m.SessionId == s.SessionId && m.SenderType == "Customer" && !m.IsRead)
                    })
                    .ToListAsync();

                return Ok(new { success = true, data = sessions });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message, data = new List<object>() });
            }
        }

        // Get pending agent requests (customers waiting for live agent)
        [HttpGet("GetPendingAgentRequests")]
        public async Task<IActionResult> GetPendingAgentRequests()
        {
            try
            {
                var pendingRequests = await _context.ChatSessions
                    .Include(s => s.Customer)
                    .Where(s => s.Status == "Pending" || s.Status == "Transferred")
                    .OrderBy(s => s.StartedAt)
                    .Select(s => new
                    {
                        s.SessionId,
                        CustomerName = s.Customer != null ? s.Customer.FullName : "Guest",
                        CustomerEmail = s.Customer != null ? s.Customer.Email : "",
                        s.CustomerId,
                        s.StartedAt,
                        WaitingMinutes = (int)(DateTime.UtcNow - s.StartedAt).TotalMinutes
                    })
                    .ToListAsync();

                return Ok(new { success = true, data = pendingRequests });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message, data = new List<object>() });
            }
        }

        // Get chat messages for a session
        [HttpGet("GetChatMessages")]
        public async Task<IActionResult> GetChatMessages([FromQuery] int sessionId)
        {
            try
            {
                var messages = await _context.ChatMessages
                    .Where(m => m.SessionId == sessionId)
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => new
                    {
                        m.MessageId,
                        m.SenderType,
                        m.Message,
                        m.MessageType,
                        m.CreatedAt
                    })
                    .ToListAsync();

                // Mark messages as read
                var unreadMessages = await _context.ChatMessages
                    .Where(m => m.SessionId == sessionId && m.SenderType == "Customer" && !m.IsRead)
                    .ToListAsync();
                
                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true;
                }
                await _context.SaveChangesAsync();

                return Ok(new { success = true, data = messages });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message, data = new List<object>() });
            }
        }

        // Accept a pending chat request
        [HttpPost("AcceptChat")]
        public async Task<IActionResult> AcceptChat([FromBody] ChatSessionRequest request)
        {
            try
            {
                var agentId = HttpContext.Session.GetInt32("UserId");
                var agentName = HttpContext.Session.GetString("FullName") ?? "Support Agent";

                var session = await _context.ChatSessions
                    .AsTracking()
                    .FirstOrDefaultAsync(s => s.SessionId == request.SessionId);
                if (session == null)
                    return Ok(new { success = false, message = "Chat session not found" });

                session.AgentId = agentId;
                session.Status = "Active";

                // Add system message
                var systemMessage = new ChatMessage
                {
                    SessionId = session.SessionId,
                    SenderType = "System",
                    Message = $"{agentName} has joined the chat. How can we assist you today?",
                    MessageType = "Text",
                    CreatedAt = DateTime.UtcNow
                };
                _context.ChatMessages.Add(systemMessage);

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Chat accepted", agentName });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        // Decline a pending chat request
        [HttpPost("DeclineChat")]
        public async Task<IActionResult> DeclineChat([FromBody] ChatSessionRequest request)
        {
            try
            {
                var session = await _context.ChatSessions
                    .AsTracking()
                    .FirstOrDefaultAsync(s => s.SessionId == request.SessionId);
                if (session == null)
                    return Ok(new { success = false, message = "Chat session not found" });

                // Add system message
                var systemMessage = new ChatMessage
                {
                    SessionId = session.SessionId,
                    SenderType = "System",
                    Message = "All agents are currently busy. Please try again later or submit a support ticket.",
                    MessageType = "Text",
                    CreatedAt = DateTime.UtcNow
                };
                _context.ChatMessages.Add(systemMessage);

                session.Status = "Ended";
                session.EndedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Chat declined" });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        // Send message from agent
        [HttpPost("SendAgentChatMessage")]
        public async Task<IActionResult> SendAgentChatMessage([FromBody] AgentChatMessageRequest request)
        {
            try
            {
                var agentId = HttpContext.Session.GetInt32("UserId") ?? 0;

                var session = await _context.ChatSessions
                    .AsTracking()
                    .FirstOrDefaultAsync(s => s.SessionId == request.SessionId);
                if (session == null)
                    return Ok(new { success = false, message = "Chat session not found" });

                var message = new ChatMessage
                {
                    SessionId = request.SessionId,
                    SenderType = "Agent",
                    SenderId = agentId,
                    Message = request.Message,
                    MessageType = "Text",
                    CreatedAt = DateTime.UtcNow
                };
                _context.ChatMessages.Add(message);

                session.TotalMessages += 1;

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Message sent" });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        // End a chat session
        [HttpPost("EndChatSession")]
        public async Task<IActionResult> EndChatSession([FromBody] ChatSessionRequest request)
        {
            try
            {
                var agentName = HttpContext.Session.GetString("FullName") ?? "Support Agent";

                var session = await _context.ChatSessions
                    .AsTracking()
                    .FirstOrDefaultAsync(s => s.SessionId == request.SessionId);
                if (session == null)
                    return Ok(new { success = false, message = "Chat session not found" });

                // Add system message
                var systemMessage = new ChatMessage
                {
                    SessionId = session.SessionId,
                    SenderType = "System",
                    Message = $"Chat ended by {agentName}. Thank you for contacting CompuGear Support!",
                    MessageType = "Text",
                    CreatedAt = DateTime.UtcNow
                };
                _context.ChatMessages.Add(systemMessage);

                session.Status = "Ended";
                session.EndedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Chat ended" });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        // Transfer chat to another agent
        [HttpPost("TransferChat")]
        public async Task<IActionResult> TransferChat([FromBody] TransferChatRequest request)
        {
            try
            {
                var fromAgentId = HttpContext.Session.GetInt32("UserId") ?? 0;
                var fromAgentName = HttpContext.Session.GetString("FullName") ?? "Support Agent";

                var session = await _context.ChatSessions
                    .AsTracking()
                    .FirstOrDefaultAsync(s => s.SessionId == request.SessionId);
                if (session == null)
                    return Ok(new { success = false, message = "Chat session not found" });

                var toAgent = await _context.Users.FindAsync(request.ToAgentId);
                if (toAgent == null)
                    return Ok(new { success = false, message = "Target agent not found" });

                // Create transfer record
                var transfer = new ChatTransfer
                {
                    ChatSessionId = session.SessionId,
                    FromUserId = fromAgentId,
                    ToUserId = request.ToAgentId,
                    Reason = request.Reason,
                    TransferredAt = DateTime.UtcNow
                };
                _context.ChatTransfers.Add(transfer);

                // Update session
                session.AgentId = request.ToAgentId;
                session.Status = "Transferred";

                // Add system message
                var toAgentName = $"{toAgent.FirstName} {toAgent.LastName}".Trim();
                var systemMessage = new ChatMessage
                {
                    SessionId = session.SessionId,
                    SenderType = "System",
                    Message = $"Chat transferred from {fromAgentName} to {toAgentName}." + 
                              (!string.IsNullOrEmpty(request.Reason) ? $" Reason: {request.Reason}" : ""),
                    MessageType = "Text",
                    CreatedAt = DateTime.UtcNow
                };
                _context.ChatMessages.Add(systemMessage);

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = $"Chat transferred to {toAgentName}" });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        // Customer requests live agent
        [HttpPost("RequestLiveAgent")]
        public async Task<IActionResult> RequestLiveAgent([FromBody] RequestLiveAgentRequest request)
        {
            try
            {
                var customerId = HttpContext.Session.GetInt32("CustomerId");

                // Find existing pending session or create new one
                var session = await _context.ChatSessions
                    .AsTracking()
                    .FirstOrDefaultAsync(s => s.CustomerId == customerId && 
                        (s.Status == "Active" || s.Status == "Pending"));

                if (session == null)
                {
                    session = new ChatSession
                    {
                        CustomerId = customerId,
                        VisitorId = request.VisitorId ?? Guid.NewGuid().ToString(),
                        SessionToken = Guid.NewGuid().ToString(),
                        Status = "Pending",
                        StartedAt = DateTime.UtcNow,
                        Source = "Website"
                    };
                    _context.ChatSessions.Add(session);
                }
                else
                {
                    session.Status = "Pending";
                }

                // Add system message
                var systemMessage = new ChatMessage
                {
                    SessionId = session.SessionId,
                    SenderType = "System",
                    Message = "You have requested to speak with a live agent. Please wait while we connect you...",
                    MessageType = "Text",
                    CreatedAt = DateTime.UtcNow
                };
                
                await _context.SaveChangesAsync();
                
                // Add system message after session is saved (to get SessionId)
                systemMessage.SessionId = session.SessionId;
                _context.ChatMessages.Add(systemMessage);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Agent request submitted", sessionId = session.SessionId });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        // Customer sends chat message
        [HttpPost("CustomerSendChatMessage")]
        public async Task<IActionResult> CustomerSendChatMessage([FromBody] CustomerChatMessageRequest request)
        {
            try
            {
                var customerId = HttpContext.Session.GetInt32("CustomerId");

                var session = await _context.ChatSessions
                    .AsTracking()
                    .FirstOrDefaultAsync(s => s.SessionId == request.SessionId);
                if (session == null)
                    return Ok(new { success = false, message = "Chat session not found" });

                // Prevent customer from sending messages until agent accepts
                if (session.Status == "Pending")
                    return Ok(new { success = false, message = "Please wait for an agent to accept the chat before sending messages.", waitingForAgent = true });

                var message = new ChatMessage
                {
                    SessionId = request.SessionId,
                    SenderType = "Customer",
                    SenderId = customerId,
                    Message = request.Message,
                    MessageType = "Text",
                    CreatedAt = DateTime.UtcNow
                };
                _context.ChatMessages.Add(message);

                session.TotalMessages += 1;

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Message sent" });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        // Get chat updates for customer
        [HttpGet("GetCustomerChatUpdates")]
        public async Task<IActionResult> GetCustomerChatUpdates([FromQuery] int sessionId, [FromQuery] int? lastMessageId)
        {
            try
            {
                var session = await _context.ChatSessions
                    .Where(s => s.SessionId == sessionId)
                    .Select(s => new { s.Status, s.AgentId })
                    .FirstOrDefaultAsync();

                if (session == null)
                    return Ok(new { success = false, message = "Session not found" });

                var messagesQuery = _context.ChatMessages
                    .Where(m => m.SessionId == sessionId);

                if (lastMessageId.HasValue && lastMessageId > 0)
                {
                    messagesQuery = messagesQuery.Where(m => m.MessageId > lastMessageId);
                }

                var messages = await messagesQuery
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => new
                    {
                        m.MessageId,
                        m.SenderType,
                        m.Message,
                        m.CreatedAt
                    })
                    .ToListAsync();

                // Get agent name if assigned
                string? agentName = null;
                if (session.AgentId.HasValue)
                {
                    var agent = await _context.Users
                        .Where(u => u.UserId == session.AgentId)
                        .Select(u => u.FirstName + " " + u.LastName)
                        .FirstOrDefaultAsync();
                    agentName = agent;
                }

                return Ok(new { 
                    success = true, 
                    data = new {
                        status = session.Status,
                        agentName = agentName,
                        messages = messages
                    }
                });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        // Customer ends their chat session
        [HttpPost("CustomerEndChat")]
        public async Task<IActionResult> CustomerEndChat([FromBody] ChatSessionRequest request)
        {
            try
            {
                var session = await _context.ChatSessions
                    .AsTracking()
                    .FirstOrDefaultAsync(s => s.SessionId == request.SessionId);
                if (session == null)
                    return Ok(new { success = false, message = "Chat session not found" });

                // Add system message
                var systemMessage = new ChatMessage
                {
                    SessionId = session.SessionId,
                    SenderType = "System",
                    Message = "Customer ended the chat. Thank you for contacting CompuGear Support!",
                    MessageType = "Text",
                    CreatedAt = DateTime.UtcNow
                };
                _context.ChatMessages.Add(systemMessage);

                session.Status = "Ended";
                session.EndedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Chat ended" });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("knowledge-categories")]
        public async Task<IActionResult> GetKnowledgeCategoriesApi()
        {
            try
            {
                var categories = await _context.KnowledgeCategories
                    .AsNoTracking()
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.DisplayOrder)
                    .ThenBy(c => c.CategoryName)
                    .Select(c => new
                    {
                        c.CategoryId,
                        c.CategoryName,
                        c.Description,
                        c.DisplayOrder,
                        c.IsActive
                    })
                    .ToListAsync();

                return Ok(categories);
            }
            catch
            {
                return Ok(new List<object>());
            }
        }

        [HttpGet("knowledge-articles")]
        public async Task<IActionResult> GetKnowledgeArticlesApi([FromQuery] int? categoryId = null, [FromQuery] string? search = null, [FromQuery] string? status = null)
        {
            try
            {
                var query = _context.KnowledgeArticles
                    .AsNoTracking()
                    .Include(a => a.Category)
                    .AsQueryable();

                if (categoryId.HasValue)
                    query = query.Where(a => a.CategoryId == categoryId.Value);

                if (!string.IsNullOrWhiteSpace(status))
                    query = query.Where(a => a.Status == status);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(a =>
                        a.Title.Contains(search) ||
                        (a.Content != null && a.Content.Contains(search)) ||
                        (a.Tags != null && a.Tags.Contains(search))
                    );
                }

                var articles = await query
                    .OrderByDescending(a => a.UpdatedAt)
                    .Select(a => new
                    {
                        a.ArticleId,
                        a.CategoryId,
                        CategoryName = a.Category != null ? a.Category.CategoryName : null,
                        a.Title,
                        a.Content,
                        a.Summary,
                        a.Tags,
                        a.ViewCount,
                        a.HelpfulCount,
                        a.NotHelpfulCount,
                        a.Status,
                        a.CreatedAt,
                        a.UpdatedAt,
                        a.CreatedBy,
                        a.UpdatedBy
                    })
                    .ToListAsync();

                return Ok(articles);
            }
            catch
            {
                return Ok(new List<object>());
            }
        }

        [HttpGet("knowledge-articles/{id}")]
        public async Task<IActionResult> GetKnowledgeArticleApi(int id)
        {
            try
            {
                var article = await _context.KnowledgeArticles
                    .AsTracking()
                    .FirstOrDefaultAsync(a => a.ArticleId == id);

                if (article == null)
                    return NotFound(new { success = false, message = "Article not found" });

                article.ViewCount += 1;
                article.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    article.ArticleId,
                    article.CategoryId,
                    article.Title,
                    article.Content,
                    article.Summary,
                    article.Tags,
                    article.ViewCount,
                    article.HelpfulCount,
                    article.NotHelpfulCount,
                    article.Status,
                    article.CreatedAt,
                    article.UpdatedAt,
                    article.CreatedBy,
                    article.UpdatedBy
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("knowledge-articles")]
        public async Task<IActionResult> CreateKnowledgeArticleApi([FromBody] KnowledgeArticleCreateRequest request)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");

                var article = new KnowledgeArticle
                {
                    CategoryId = request.CategoryId,
                    Title = request.Title,
                    Content = request.Content,
                    Summary = request.Summary,
                    Tags = request.Tags,
                    Status = string.IsNullOrWhiteSpace(request.Status) ? "Pending Approval" : request.Status,
                    CreatedBy = userId,
                    UpdatedBy = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.KnowledgeArticles.Add(article);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Knowledge article submitted", articleId = article.ArticleId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Company Module Subscription (for sidebar filtering)

        /// <summary>
        /// Returns the module codes the logged-in company is subscribed to.
        /// Used by _Layout.cshtml to show/hide sidebar menu items.
        /// </summary>
        [HttpGet("my-modules")]
        public async Task<IActionResult> GetMyModules()
        {
            try
            {
                var roleId = HttpContext.Session.GetInt32("RoleId");
                // Super Admin sees everything
                if (roleId == 1)
                {
                    var allModules = await _context.ERPModules
                        .Where(m => m.IsActive)
                        .Select(m => m.ModuleCode)
                        .ToListAsync();
                    return Ok(new { success = true, modules = allModules });
                }

                var companyId = HttpContext.Session.GetInt32("CompanyId");
                if (companyId == null)
                {
                    // No company → show nothing (or default set)
                    return Ok(new { success = true, modules = new List<string>() });
                }

                var subscribedModules = await _context.CompanyModuleAccess
                    .Include(a => a.Module)
                    .Where(a => a.CompanyId == companyId && a.IsEnabled)
                    .Select(a => a.Module.ModuleCode)
                    .ToListAsync();

                // If explicit role-module access exists, return intersection of company subscription + role access
                if (roleId.HasValue && roleId != 1 && roleId != 2)
                {
                    var roleAllowedModules = await _context.RoleModuleAccess
                        .Where(r => r.CompanyId == companyId && r.RoleId == roleId.Value && r.HasAccess)
                        .Select(r => r.ModuleCode)
                        .Distinct()
                        .ToListAsync();

                    if (roleAllowedModules.Any())
                    {
                        var allowedSet = roleAllowedModules.ToHashSet(StringComparer.OrdinalIgnoreCase);
                        subscribedModules = subscribedModules
                            .Where(m => allowedSet.Contains(m))
                            .ToList();
                    }
                }

                return Ok(new { success = true, modules = subscribedModules });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Role-Based Access Control

        [HttpGet("role-access")]
        public async Task<IActionResult> GetRoleAccess()
        {
            try
            {
                var companyId = GetCompanyId();
                if (companyId == null)
                {
                    // Super Admin: get companyId from query if provided
                    var qCompanyId = HttpContext.Request.Query["companyId"].FirstOrDefault();
                    if (!string.IsNullOrEmpty(qCompanyId) && int.TryParse(qCompanyId, out var cid))
                        companyId = cid;
                    else
                        return BadRequest(new { success = false, message = "CompanyId is required" });
                }

                var access = await _context.RoleModuleAccess
                    .Where(r => r.CompanyId == companyId)
                    .Include(r => r.Role)
                    .Select(r => new
                    {
                        r.Id,
                        r.CompanyId,
                        r.RoleId,
                        RoleName = r.Role != null ? r.Role.RoleName : "",
                        r.ModuleCode,
                        r.HasAccess
                    })
                    .ToListAsync();

                // Get available roles (exclude Super Admin and Customer)
                var roles = await _context.Roles
                    .Where(r => r.RoleId != 1 && r.RoleId != 7)
                    .OrderBy(r => r.RoleId)
                    .Select(r => new { r.RoleId, r.RoleName })
                    .ToListAsync();

                var modules = new[]
                {
                    new { Code = "SALES", Name = "Sales" },
                    new { Code = "CUSTOMERS", Name = "Customers" },
                    new { Code = "INVENTORY", Name = "Inventory" },
                    new { Code = "BILLING", Name = "Billing" },
                    new { Code = "MARKETING", Name = "Marketing" },
                    new { Code = "SUPPORT", Name = "Support" }
                };

                return Ok(new { success = true, data = access, roles, modules });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("role-access")]
        public async Task<IActionResult> SaveRoleAccess([FromBody] RoleAccessSaveRequest request)
        {
            try
            {
                var companyId = GetCompanyId();
                if (companyId == null)
                {
                    if (request.CompanyId.HasValue)
                        companyId = request.CompanyId.Value;
                    else
                        return BadRequest(new { success = false, message = "CompanyId is required" });
                }

                // Remove existing entries for this company
                var existing = await _context.RoleModuleAccess
                    .Where(r => r.CompanyId == companyId)
                    .ToListAsync();
                _context.RoleModuleAccess.RemoveRange(existing);

                // Add new entries
                foreach (var item in request.AccessList)
                {
                    _context.RoleModuleAccess.Add(new RoleModuleAccess
                    {
                        CompanyId = companyId.Value,
                        RoleId = item.RoleId,
                        ModuleCode = item.ModuleCode,
                        HasAccess = item.HasAccess,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Role access settings saved successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        #endregion

        #region ===== INVOICES =====

        [HttpGet("invoices")]
        public async Task<IActionResult> GetInvoices()
        {
            try
            {
                var companyId = GetCompanyId();
                var dbInvoices = await _context.Invoices
                    .Include(i => i.Customer)
                    .Include(i => i.Order)
                    .Include(i => i.Items)
                    .Where(i => companyId == null || i.CompanyId == companyId)
                    .OrderByDescending(i => i.CreatedAt)
                    .Select(i => new
                    {
                        i.InvoiceId,
                        i.InvoiceNumber,
                        i.OrderId,
                        OrderNumber = i.Order != null ? i.Order.OrderNumber : null,
                        i.CustomerId,
                        CustomerName = i.Customer != null ? i.Customer.FirstName + " " + i.Customer.LastName : "N/A",
                        i.InvoiceDate,
                        i.DueDate,
                        i.Subtotal,
                        i.DiscountAmount,
                        i.TaxAmount,
                        i.ShippingAmount,
                        i.TotalAmount,
                        PaidAmount = i.Order != null && i.Order.OrderStatus == "Confirmed" && i.Order.PaymentStatus == "Paid"
                            ? i.TotalAmount
                            : i.PaidAmount,
                        BalanceDue = i.Order != null && i.Order.OrderStatus == "Confirmed" && i.Order.PaymentStatus == "Paid"
                            ? 0
                            : Math.Max(0, i.TotalAmount - i.PaidAmount),
                        Status = i.Order != null && i.Order.OrderStatus == "Confirmed" && i.Order.PaymentStatus == "Paid"
                            ? "Paid/Confirmed"
                            : (i.PaidAmount >= i.TotalAmount
                                ? "Paid"
                                : (i.PaidAmount > 0 && i.Status != "Cancelled" && i.Status != "Void" ? "Partial" : i.Status)),
                        i.BillingName,
                        i.BillingAddress,
                        i.BillingCity,
                        i.BillingCountry,
                        i.PaymentTerms,
                        i.Notes,
                        i.SentAt,
                        i.PaidAt,
                        i.CreatedAt,
                        Items = i.Items.Select(item => new
                        {
                            item.ItemId,
                            item.ProductId,
                            item.Description,
                            item.Quantity,
                            item.UnitPrice,
                            item.DiscountAmount,
                            item.TaxAmount,
                            item.TotalPrice
                        })
                    })
                    .ToListAsync();

                var mappedOrderIds = dbInvoices
                    .Where(i => i.OrderId.HasValue)
                    .Select(i => i.OrderId!.Value)
                    .Distinct()
                    .ToHashSet();

                var derivedOrders = await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderItems)
                    .Where(o => (companyId == null || o.CompanyId == companyId) &&
                                (o.OrderStatus == "Pending" || o.OrderStatus == "Confirmed") &&
                                !mappedOrderIds.Contains(o.OrderId))
                    .Select(o => new
                    {
                        InvoiceId = -o.OrderId,
                        InvoiceNumber = "ORD-" + o.OrderNumber,
                        OrderId = (int?)o.OrderId,
                        OrderNumber = o.OrderNumber,
                        CustomerId = o.CustomerId,
                        CustomerName = o.Customer != null ? o.Customer.FirstName + " " + o.Customer.LastName : "N/A",
                        InvoiceDate = o.OrderDate,
                        DueDate = o.OrderDate,
                        Subtotal = o.Subtotal,
                        DiscountAmount = o.DiscountAmount,
                        TaxAmount = o.TaxAmount,
                        ShippingAmount = o.ShippingAmount,
                        TotalAmount = o.TotalAmount,
                        PaidAmount = o.PaidAmount,
                        BalanceDue = Math.Max(0, o.TotalAmount - o.PaidAmount),
                        Status = o.OrderStatus == "Confirmed" && o.PaymentStatus == "Paid"
                            ? "Paid/Confirmed"
                            : (o.PaidAmount >= o.TotalAmount ? "Paid" : (o.PaidAmount > 0 ? "Partial" : "Pending")),
                        BillingName = o.Customer != null ? o.Customer.FirstName + " " + o.Customer.LastName : "N/A",
                        BillingAddress = o.BillingAddress,
                        BillingCity = o.BillingCity,
                        BillingCountry = o.BillingCountry,
                        PaymentTerms = (string?)"Order Based",
                        Notes = o.Notes,
                        SentAt = (DateTime?)null,
                        PaidAt = (DateTime?)null,
                        CreatedAt = o.CreatedAt,
                        Items = o.OrderItems.Select(item => new
                        {
                            ItemId = -item.OrderItemId,
                            item.ProductId,
                            Description = item.ProductName,
                            item.Quantity,
                            item.UnitPrice,
                            item.DiscountAmount,
                            item.TaxAmount,
                            TotalPrice = item.TotalPrice
                        })
                    })
                    .ToListAsync();

                var invoices = dbInvoices
                    .Cast<object>()
                    .Concat(derivedOrders.Cast<object>())
                    .ToList();

                return Ok(new { success = true, data = invoices });
            }
            catch (Exception)
            {
                return Ok(new { success = true, data = new List<object>() });
            }
        }

        [HttpGet("invoices/{id}")]
        public async Task<IActionResult> GetInvoice(int id)
        {
            if (id < 0)
            {
                var companyIdFromSession = GetCompanyId();
                var orderId = -id;
                var order = await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderItems)
                    .Where(o => companyIdFromSession == null || o.CompanyId == companyIdFromSession)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null)
                    return NotFound(new { success = false, message = "Invoice not found" });

                var derivedInvoice = new
                {
                    InvoiceId = -order.OrderId,
                    InvoiceNumber = "ORD-" + order.OrderNumber,
                    OrderId = (int?)order.OrderId,
                    OrderNumber = order.OrderNumber,
                    CustomerId = order.CustomerId,
                    CustomerName = order.Customer != null ? order.Customer.FirstName + " " + order.Customer.LastName : "N/A",
                    CustomerEmail = order.Customer != null ? order.Customer.Email : "",
                    InvoiceDate = order.OrderDate,
                    DueDate = order.OrderDate,
                    Subtotal = order.Subtotal,
                    DiscountAmount = order.DiscountAmount,
                    TaxAmount = order.TaxAmount,
                    ShippingAmount = order.ShippingAmount,
                    TotalAmount = order.TotalAmount,
                    PaidAmount = order.PaidAmount,
                    BalanceDue = Math.Max(0, order.TotalAmount - order.PaidAmount),
                    Status = order.OrderStatus == "Confirmed" && order.PaymentStatus == "Paid"
                        ? "Paid/Confirmed"
                        : (order.PaidAmount >= order.TotalAmount ? "Paid" : (order.PaidAmount > 0 ? "Partial" : "Pending")),
                    BillingName = order.Customer != null ? order.Customer.FirstName + " " + order.Customer.LastName : "N/A",
                    BillingAddress = order.BillingAddress,
                    BillingCity = order.BillingCity,
                    BillingState = order.BillingState,
                    BillingZipCode = order.BillingZipCode,
                    BillingCountry = order.BillingCountry,
                    BillingEmail = order.Customer?.Email,
                    PaymentTerms = "Order Based",
                    order.Notes,
                    InternalNotes = (string?)null,
                    SentAt = (DateTime?)null,
                    PaidAt = (DateTime?)null,
                    CreatedAt = order.CreatedAt,
                    Items = order.OrderItems.Select(item => new
                    {
                        ItemId = -item.OrderItemId,
                        item.ProductId,
                        Description = item.ProductName,
                        item.Quantity,
                        item.UnitPrice,
                        item.DiscountAmount,
                        item.TaxAmount,
                        TotalPrice = item.TotalPrice
                    })
                };

                return Ok(new { success = true, data = derivedInvoice });
            }

            var companyId = GetCompanyId();
            var invoice = await _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Order)
                .Include(i => i.Items)
                .Where(i => companyId == null || i.CompanyId == companyId)
                .Where(i => i.InvoiceId == id)
                .Select(i => new
                {
                    i.InvoiceId,
                    i.InvoiceNumber,
                    i.OrderId,
                    OrderNumber = i.Order != null ? i.Order.OrderNumber : null,
                    i.CustomerId,
                    CustomerName = i.Customer != null ? i.Customer.FirstName + " " + i.Customer.LastName : "N/A",
                    CustomerEmail = i.Customer != null ? i.Customer.Email : "",
                    i.InvoiceDate,
                    i.DueDate,
                    i.Subtotal,
                    i.DiscountAmount,
                    i.TaxAmount,
                    i.ShippingAmount,
                    i.TotalAmount,
                    PaidAmount = i.Order != null && i.Order.OrderStatus == "Confirmed" && i.Order.PaymentStatus == "Paid"
                        ? i.TotalAmount
                        : i.PaidAmount,
                    BalanceDue = i.Order != null && i.Order.OrderStatus == "Confirmed" && i.Order.PaymentStatus == "Paid"
                        ? 0
                        : Math.Max(0, i.TotalAmount - i.PaidAmount),
                    Status = i.Order != null && i.Order.OrderStatus == "Confirmed" && i.Order.PaymentStatus == "Paid"
                        ? "Paid/Confirmed"
                        : (i.PaidAmount >= i.TotalAmount
                            ? "Paid"
                            : (i.PaidAmount > 0 && i.Status != "Cancelled" && i.Status != "Void" ? "Partial" : i.Status)),
                    i.BillingName,
                    i.BillingAddress,
                    i.BillingCity,
                    i.BillingState,
                    i.BillingZipCode,
                    i.BillingCountry,
                    i.BillingEmail,
                    i.PaymentTerms,
                    i.Notes,
                    i.InternalNotes,
                    i.SentAt,
                    i.PaidAt,
                    i.CreatedAt,
                    Items = i.Items.Select(item => new
                    {
                        item.ItemId,
                        item.ProductId,
                        item.Description,
                        item.Quantity,
                        item.UnitPrice,
                        item.DiscountAmount,
                        item.TaxAmount,
                        item.TotalPrice
                    })
                })
                .FirstOrDefaultAsync();

            if (invoice == null)
                return NotFound(new { success = false, message = "Invoice not found" });

            return Ok(new { success = true, data = invoice });
        }

        [HttpPut("invoices/{id}/status")]
        public async Task<IActionResult> UpdateInvoiceStatus(int id, [FromBody] InvoiceStatusModel model)
        {
            if (!HasFullBillingAccess())
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Invoices are read-only. Update order status instead." });

            var companyId = GetCompanyId();
            var invoice = await _context.Invoices
                .Where(i => companyId == null || i.CompanyId == companyId)
                .FirstOrDefaultAsync(i => i.InvoiceId == id);

            if (invoice == null)
                return NotFound(new { success = false, message = "Invoice not found" });

            invoice.Status = model.Status;
            invoice.UpdatedAt = DateTime.UtcNow;
            if (model.Status == "Sent") invoice.SentAt = DateTime.UtcNow;
            if (model.Status == "Paid")
            {
                invoice.PaidAt = DateTime.UtcNow;
                invoice.PaidAmount = invoice.TotalAmount;
                invoice.BalanceDue = 0;
            }
            else if (model.Status == "Pending" || model.Status == "Draft")
            {
                invoice.PaidAmount = 0;
                invoice.BalanceDue = invoice.TotalAmount;
                invoice.PaidAt = null;
            }
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Invoice status updated" });
        }

        #endregion

        #region ===== PAYMENTS =====

        [HttpGet("payments")]
        public async Task<IActionResult> GetPayments()
        {
            try
            {
                var companyId = GetCompanyId();
                var recordedPayments = await _context.Payments
                    .Include(p => p.Customer)
                    .Include(p => p.Invoice)
                    .Include(p => p.Order)
                    .Where(p => companyId == null || p.CompanyId == companyId || p.CompanyId == null)
                    .Select(p => new
                    {
                        p.PaymentId,
                        p.PaymentNumber,
                        p.InvoiceId,
                        InvoiceNumber = p.Invoice != null ? p.Invoice.InvoiceNumber : null,
                        InvoiceSubtotal = p.Invoice != null ? p.Invoice.Subtotal : 0,
                        InvoiceTaxAmount = p.Invoice != null ? p.Invoice.TaxAmount : 0,
                        InvoiceDiscountAmount = p.Invoice != null ? p.Invoice.DiscountAmount : 0,
                        InvoiceShippingAmount = p.Invoice != null ? p.Invoice.ShippingAmount : 0,
                        InvoiceTotalAmount = p.Invoice != null ? p.Invoice.TotalAmount : 0,
                        p.OrderId,
                        OrderNumber = p.Order != null ? p.Order.OrderNumber : null,
                        p.CustomerId,
                        CustomerName = p.Customer != null ? p.Customer.FirstName + " " + p.Customer.LastName : "N/A",
                        p.PaymentDate,
                        p.Amount,
                        p.PaymentMethodType,
                        p.Status,
                        p.TransactionId,
                        p.ReferenceNumber,
                        p.Currency,
                        p.Notes,
                        p.FailureReason,
                        p.ProcessedAt,
                        p.CreatedAt,
                        IsDerived = false
                    })
                    .ToListAsync();

                var recordedOrderIds = recordedPayments
                    .Where(p => p.OrderId.HasValue)
                    .Select(p => p.OrderId!.Value)
                    .Distinct()
                    .ToHashSet();

                var recordedInvoiceIds = recordedPayments
                    .Where(p => p.InvoiceId.HasValue)
                    .Select(p => p.InvoiceId!.Value)
                    .Distinct()
                    .ToHashSet();

                var ordersForDerivation = await _context.Orders
                    .Include(o => o.Customer)
                    .Where(o => (companyId == null || o.CompanyId == companyId || o.CompanyId == null) &&
                                o.OrderStatus != "Cancelled" &&
                                o.OrderStatus != "Rejected" &&
                                !recordedOrderIds.Contains(o.OrderId) &&
                                (!string.IsNullOrEmpty(o.PaymentMethod) ||
                                 !string.IsNullOrEmpty(o.PaymentReference) ||
                                 o.PaidAmount > 0 ||
                                 o.PaymentStatus == "Paid" ||
                                 o.PaymentStatus == "Pending"))
                    .Select(o => new
                    {
                        o.OrderId,
                        o.OrderNumber,
                        o.CustomerId,
                        CustomerName = o.Customer != null ? o.Customer.FirstName + " " + o.Customer.LastName : "N/A",
                        o.ConfirmedAt,
                        o.UpdatedAt,
                        o.CreatedAt,
                        o.PaymentMethod,
                        o.PaymentReference,
                        o.PaymentStatus,
                        o.PaidAmount,
                        o.TotalAmount
                    })
                    .ToListAsync();

                var orderIds = ordersForDerivation.Select(o => o.OrderId).Distinct().ToList();
                var invoiceByOrderId = await _context.Invoices
                    .Where(i => orderIds.Contains(i.OrderId ?? 0) && (companyId == null || i.CompanyId == companyId || i.CompanyId == null))
                    .Select(i => new
                    {
                        i.InvoiceId,
                        i.OrderId,
                        i.InvoiceNumber,
                        i.Subtotal,
                        i.TaxAmount,
                        i.DiscountAmount,
                        i.ShippingAmount,
                        i.TotalAmount
                    })
                    .ToListAsync();

                var invoiceByOrder = invoiceByOrderId
                    .Where(i => i.OrderId.HasValue)
                    .GroupBy(i => i.OrderId!.Value)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.InvoiceId).First());

                var derivedFromOrders = ordersForDerivation
                    .Select(o => new
                    {
                        PaymentId = -o.OrderId,
                        PaymentNumber = "AUTO-" + o.OrderNumber,
                        InvoiceId = invoiceByOrder.ContainsKey(o.OrderId) ? (int?)invoiceByOrder[o.OrderId].InvoiceId : null,
                        InvoiceNumber = invoiceByOrder.ContainsKey(o.OrderId) ? invoiceByOrder[o.OrderId].InvoiceNumber : null,
                        InvoiceSubtotal = invoiceByOrder.ContainsKey(o.OrderId) ? invoiceByOrder[o.OrderId].Subtotal : 0,
                        InvoiceTaxAmount = invoiceByOrder.ContainsKey(o.OrderId) ? invoiceByOrder[o.OrderId].TaxAmount : 0,
                        InvoiceDiscountAmount = invoiceByOrder.ContainsKey(o.OrderId) ? invoiceByOrder[o.OrderId].DiscountAmount : 0,
                        InvoiceShippingAmount = invoiceByOrder.ContainsKey(o.OrderId) ? invoiceByOrder[o.OrderId].ShippingAmount : 0,
                        InvoiceTotalAmount = invoiceByOrder.ContainsKey(o.OrderId) ? invoiceByOrder[o.OrderId].TotalAmount : o.TotalAmount,
                        OrderId = (int?)o.OrderId,
                        OrderNumber = (string?)o.OrderNumber,
                        CustomerId = o.CustomerId,
                        CustomerName = o.CustomerName,
                        PaymentDate = o.ConfirmedAt ?? o.UpdatedAt,
                        Amount = o.PaidAmount > 0 ? o.PaidAmount : o.TotalAmount,
                        PaymentMethodType = o.PaymentMethod ?? "Order Confirmation",
                        Status = o.PaymentStatus == "Paid" ? "Completed" : "Pending",
                        TransactionId = o.PaymentReference,
                        ReferenceNumber = o.PaymentReference,
                        Currency = "PHP",
                        Notes = (string?)"Auto-derived from customer order",
                        FailureReason = (string?)null,
                        ProcessedAt = o.ConfirmedAt,
                        CreatedAt = o.CreatedAt,
                        IsDerived = true
                    })
                    .ToList();

                foreach (var item in derivedFromOrders)
                {
                    if (item.InvoiceId.HasValue)
                        recordedInvoiceIds.Add(item.InvoiceId.Value);
                }

                const int derivedInvoiceOffset = 1000000;
                var derivedFromInvoices = await _context.Invoices
                    .Include(i => i.Customer)
                    .Include(i => i.Order)
                    .Where(i => (companyId == null || i.CompanyId == companyId || i.CompanyId == null) &&
                                !recordedInvoiceIds.Contains(i.InvoiceId) &&
                                i.Status != "Cancelled" &&
                                i.Status != "Void" &&
                                (i.PaidAmount > 0 || i.TotalAmount > 0))
                    .Select(i => new
                    {
                        PaymentId = -(derivedInvoiceOffset + i.InvoiceId),
                        PaymentNumber = "AUTO-" + i.InvoiceNumber,
                        InvoiceId = (int?)i.InvoiceId,
                        InvoiceNumber = i.InvoiceNumber,
                        InvoiceSubtotal = i.Subtotal,
                        InvoiceTaxAmount = i.TaxAmount,
                        InvoiceDiscountAmount = i.DiscountAmount,
                        InvoiceShippingAmount = i.ShippingAmount,
                        InvoiceTotalAmount = i.TotalAmount,
                        OrderId = i.OrderId,
                        OrderNumber = i.Order != null ? i.Order.OrderNumber : null,
                        CustomerId = i.CustomerId,
                        CustomerName = i.Customer != null ? i.Customer.FirstName + " " + i.Customer.LastName : "N/A",
                        PaymentDate = i.PaidAt ?? i.SentAt ?? i.InvoiceDate,
                        Amount = i.PaidAmount > 0 ? i.PaidAmount : i.TotalAmount,
                        PaymentMethodType = i.Order != null ? i.Order.PaymentMethod : "Invoice Record",
                        Status = (i.PaidAmount > 0 || i.Status == "Paid") ? "Completed" : "Pending",
                        TransactionId = i.Order != null ? i.Order.PaymentReference : null,
                        ReferenceNumber = i.Order != null ? i.Order.PaymentReference : null,
                        Currency = "PHP",
                        Notes = (string?)"Auto-derived from customer invoice",
                        FailureReason = (string?)null,
                        ProcessedAt = i.PaidAt,
                        CreatedAt = i.CreatedAt,
                        IsDerived = true
                    })
                    .ToListAsync();

                var payments = recordedPayments
                    .Concat(derivedFromOrders)
                    .Concat(derivedFromInvoices)
                    .OrderByDescending(p => p.PaymentDate)
                    .ThenByDescending(p => p.CreatedAt)
                    .ToList();

                return Ok(new { success = true, data = payments });
            }
            catch (Exception)
            {
                return Ok(new { success = true, data = new List<object>() });
            }
        }

        [HttpGet("payments/{id}")]
        public async Task<IActionResult> GetPayment(int id)
        {
            const int derivedInvoiceOffset = 1000000;

            if (id <= -derivedInvoiceOffset)
            {
                var companyIdFromSession = GetCompanyId();
                var derivedInvoiceId = (-id) - derivedInvoiceOffset;

                var invoice = await _context.Invoices
                    .Include(i => i.Customer)
                    .Include(i => i.Order)
                    .FirstOrDefaultAsync(i => i.InvoiceId == derivedInvoiceId &&
                                              (companyIdFromSession == null || i.CompanyId == companyIdFromSession || i.CompanyId == null));

                if (invoice == null)
                    return NotFound(new { success = false, message = "Payment not found" });

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        PaymentId = -(derivedInvoiceOffset + invoice.InvoiceId),
                        PaymentNumber = "AUTO-" + invoice.InvoiceNumber,
                        InvoiceId = invoice.InvoiceId,
                        InvoiceNumber = invoice.InvoiceNumber,
                        InvoiceSubtotal = invoice.Subtotal,
                        InvoiceTaxAmount = invoice.TaxAmount,
                        InvoiceDiscountAmount = invoice.DiscountAmount,
                        InvoiceShippingAmount = invoice.ShippingAmount,
                        InvoiceTotalAmount = invoice.TotalAmount,
                        OrderId = invoice.OrderId,
                        OrderNumber = invoice.Order?.OrderNumber,
                        CustomerId = invoice.CustomerId,
                        CustomerName = invoice.Customer != null ? invoice.Customer.FirstName + " " + invoice.Customer.LastName : "N/A",
                        PaymentDate = invoice.PaidAt ?? invoice.SentAt ?? invoice.InvoiceDate,
                        Amount = invoice.PaidAmount > 0 ? invoice.PaidAmount : invoice.TotalAmount,
                        PaymentMethodType = invoice.Order?.PaymentMethod ?? "Invoice Record",
                        Status = (invoice.PaidAmount > 0 || invoice.Status == "Paid") ? "Completed" : "Pending",
                        TransactionId = invoice.Order?.PaymentReference,
                        ReferenceNumber = invoice.Order?.PaymentReference,
                        Currency = "PHP",
                        Notes = "Auto-derived from customer invoice",
                        FailureReason = (string?)null,
                        ProcessedAt = invoice.PaidAt,
                        CreatedAt = invoice.CreatedAt,
                        Refunds = new List<object>()
                    }
                });
            }

            if (id < 0)
            {
                var companyIdFromSession = GetCompanyId();
                var derivedOrderId = -id;

                var order = await _context.Orders
                    .Include(o => o.Customer)
                    .FirstOrDefaultAsync(o => o.OrderId == derivedOrderId &&
                                              (companyIdFromSession == null || o.CompanyId == companyIdFromSession || o.CompanyId == null));

                if (order == null)
                    return NotFound(new { success = false, message = "Payment not found" });

                var linkedInvoice = await _context.Invoices
                    .FirstOrDefaultAsync(i => i.OrderId == order.OrderId &&
                                              (companyIdFromSession == null || i.CompanyId == companyIdFromSession || i.CompanyId == null));

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        PaymentId = -order.OrderId,
                        PaymentNumber = "AUTO-" + order.OrderNumber,
                        InvoiceId = linkedInvoice?.InvoiceId,
                        InvoiceNumber = linkedInvoice?.InvoiceNumber,
                        InvoiceSubtotal = linkedInvoice?.Subtotal ?? 0,
                        InvoiceTaxAmount = linkedInvoice?.TaxAmount ?? 0,
                        InvoiceDiscountAmount = linkedInvoice?.DiscountAmount ?? 0,
                        InvoiceShippingAmount = linkedInvoice?.ShippingAmount ?? 0,
                        InvoiceTotalAmount = linkedInvoice?.TotalAmount ?? order.TotalAmount,
                        OrderId = order.OrderId,
                        OrderNumber = order.OrderNumber,
                        CustomerId = order.CustomerId,
                        CustomerName = order.Customer != null ? order.Customer.FirstName + " " + order.Customer.LastName : "N/A",
                        PaymentDate = order.ConfirmedAt ?? order.UpdatedAt,
                        Amount = order.PaidAmount,
                        PaymentMethodType = order.PaymentMethod ?? "Order Confirmation",
                        Status = order.PaymentStatus == "Paid" ? "Completed" : "Pending",
                        TransactionId = order.PaymentReference,
                        ReferenceNumber = order.PaymentReference,
                        Currency = "PHP",
                        Notes = "Auto-derived from customer order",
                        FailureReason = (string?)null,
                        ProcessedAt = order.ConfirmedAt,
                        CreatedAt = order.CreatedAt,
                        Refunds = new List<object>()
                    }
                });
            }

            var companyId = GetCompanyId();
            var payment = await _context.Payments
                .Include(p => p.Customer)
                .Include(p => p.Invoice)
                .Include(p => p.Order)
                .Include(p => p.Refunds)
                .Where(p => companyId == null || p.CompanyId == companyId)
                .Where(p => p.PaymentId == id)
                .Select(p => new
                {
                    p.PaymentId,
                    p.PaymentNumber,
                    p.InvoiceId,
                    InvoiceNumber = p.Invoice != null ? p.Invoice.InvoiceNumber : null,
                    InvoiceSubtotal = p.Invoice != null ? p.Invoice.Subtotal : 0,
                    InvoiceTaxAmount = p.Invoice != null ? p.Invoice.TaxAmount : 0,
                    InvoiceDiscountAmount = p.Invoice != null ? p.Invoice.DiscountAmount : 0,
                    InvoiceShippingAmount = p.Invoice != null ? p.Invoice.ShippingAmount : 0,
                    InvoiceTotalAmount = p.Invoice != null ? p.Invoice.TotalAmount : 0,
                    p.OrderId,
                    OrderNumber = p.Order != null ? p.Order.OrderNumber : null,
                    p.CustomerId,
                    CustomerName = p.Customer != null ? p.Customer.FirstName + " " + p.Customer.LastName : "N/A",
                    p.PaymentDate,
                    p.Amount,
                    p.PaymentMethodType,
                    p.Status,
                    p.TransactionId,
                    p.ReferenceNumber,
                    p.Currency,
                    p.Notes,
                    p.FailureReason,
                    p.ProcessedAt,
                    p.CreatedAt,
                    Refunds = p.Refunds.Select(r => new
                    {
                        r.RefundId,
                        r.RefundNumber,
                        r.Amount,
                        r.Reason,
                        r.Status,
                        r.RefundMethod,
                        r.RequestedAt
                    })
                })
                .FirstOrDefaultAsync();

            if (payment == null)
                return NotFound(new { success = false, message = "Payment not found" });

            return Ok(new { success = true, data = payment });
        }

        #endregion

        #region ===== REFUNDS =====

        [HttpGet("refunds")]
        public async Task<IActionResult> GetRefunds()
        {
            var companyId = GetCompanyId();
            var refunds = await _context.Refunds
                .Include(r => r.Payment)
                .Include(r => r.Customer)
                .Include(r => r.Order)
                .Where(r => companyId == null || r.CompanyId == companyId)
                .OrderByDescending(r => r.RequestedAt)
                .Select(r => new
                {
                    r.RefundId,
                    r.RefundNumber,
                    r.PaymentId,
                    PaymentNumber = r.Payment != null ? r.Payment.PaymentNumber : null,
                    r.OrderId,
                    OrderNumber = r.Order != null ? r.Order.OrderNumber : null,
                    r.CustomerId,
                    CustomerName = r.Customer != null ? r.Customer.FirstName + " " + r.Customer.LastName : "N/A",
                    r.Amount,
                    r.Reason,
                    r.Status,
                    r.RefundMethod,
                    r.RequestedAt,
                    r.ApprovedAt,
                    r.ProcessedAt
                })
                .ToListAsync();

            return Ok(new { success = true, data = refunds });
        }

        [HttpPut("refunds/{id}/status")]
        public async Task<IActionResult> UpdateRefundStatus(int id, [FromBody] StatusUpdateRequest model)
        {
            var companyId = GetCompanyId();
            var refund = await _context.Refunds
                .Where(r => companyId == null || r.CompanyId == companyId)
                .FirstOrDefaultAsync(r => r.RefundId == id);

            if (refund == null)
                return NotFound(new { success = false, message = "Refund not found" });

            var userId = HttpContext.Session.GetInt32("UserId");
            refund.Status = model.Status;
            if (model.Status == "Approved") { refund.ApprovedAt = DateTime.UtcNow; refund.ApprovedBy = userId; }
            if (model.Status == "Processed") { refund.ProcessedAt = DateTime.UtcNow; refund.ProcessedBy = userId; }
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Refund status updated" });
        }

        #endregion

        #region ===== SUPPLIERS =====

        [HttpGet("suppliers")]
        public async Task<IActionResult> GetSuppliers()
        {
            try
            {
                var companyId = GetCompanyId();
                var suppliers = await _context.Suppliers
                    .Where(s => companyId == null || s.CompanyId == companyId)
                    .OrderBy(s => s.SupplierName)
                    .Select(s => new
                    {
                        s.SupplierId,
                        s.SupplierCode,
                        s.SupplierName,
                        s.ContactPerson,
                        s.Email,
                        s.Phone,
                        s.Address,
                        s.City,
                        s.Country,
                        s.Website,
                        s.PaymentTerms,
                        s.Status,
                        s.Rating,
                        s.Notes,
                        s.CreatedAt,
                        PurchaseOrderCount = s.PurchaseOrders.Count()
                    })
                    .ToListAsync();

                return Ok(suppliers);
            }
            catch (Exception)
            {
                return Ok(new List<object>());
            }
        }

        [HttpGet("suppliers/{id}")]
        public async Task<IActionResult> GetSupplier(int id)
        {
            var companyId = GetCompanyId();
            var supplier = await _context.Suppliers
                .Include(s => s.PurchaseOrders)
                .Where(s => companyId == null || s.CompanyId == companyId)
                .Where(s => s.SupplierId == id)
                .Select(s => new
                {
                    s.SupplierId,
                    s.SupplierCode,
                    s.SupplierName,
                    s.ContactPerson,
                    s.Email,
                    s.Phone,
                    s.Address,
                    s.City,
                    s.Country,
                    s.Website,
                    s.PaymentTerms,
                    s.Status,
                    s.Rating,
                    s.Notes,
                    s.CreatedAt,
                    PurchaseOrders = s.PurchaseOrders.Select(po => new
                    {
                        po.PurchaseOrderId,
                        po.OrderDate,
                        po.ExpectedDeliveryDate,
                        po.Status,
                        po.TotalAmount
                    })
                })
                .FirstOrDefaultAsync();

            if (supplier == null)
                return NotFound(new { success = false, message = "Supplier not found" });

            return Ok(new { success = true, data = supplier });
        }

        [HttpPost("suppliers")]
        public async Task<IActionResult> CreateSupplier([FromBody] Supplier model)
        {
            try
            {
                var companyId = GetCompanyId();
                model.CompanyId = companyId;
                model.CreatedAt = DateTime.UtcNow;
                model.UpdatedAt = DateTime.UtcNow;

                // Generate supplier code
                var lastCode = await _context.Suppliers
                    .Where(s => s.SupplierCode != null)
                    .OrderByDescending(s => s.SupplierCode)
                    .Select(s => s.SupplierCode)
                    .FirstOrDefaultAsync();
                int nextNum = 1;
                if (lastCode != null && lastCode.StartsWith("SUP-"))
                    int.TryParse(lastCode.Replace("SUP-", ""), out nextNum);
                model.SupplierCode = $"SUP-{(nextNum + 1):D3}";

                _context.Suppliers.Add(model);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Supplier created", data = new { model.SupplierId } });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPut("suppliers/{id}")]
        public async Task<IActionResult> UpdateSupplier(int id, [FromBody] Supplier model)
        {
            var companyId = GetCompanyId();
            var supplier = await _context.Suppliers
                .Where(s => companyId == null || s.CompanyId == companyId)
                .FirstOrDefaultAsync(s => s.SupplierId == id);

            if (supplier == null)
                return NotFound(new { success = false, message = "Supplier not found" });

            supplier.SupplierName = model.SupplierName;
            supplier.ContactPerson = model.ContactPerson;
            supplier.Email = model.Email;
            supplier.Phone = model.Phone;
            supplier.Address = model.Address;
            supplier.City = model.City;
            supplier.Country = model.Country;
            supplier.Website = model.Website;
            supplier.PaymentTerms = model.PaymentTerms;
            supplier.Status = model.Status;
            supplier.Rating = model.Rating;
            supplier.Notes = model.Notes;
            supplier.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Supplier updated" });
        }

        [HttpPut("suppliers/{id}/toggle-status")]
        public async Task<IActionResult> ToggleSupplierStatus(int id)
        {
            var companyId = GetCompanyId();
            var supplier = await _context.Suppliers
                .Where(s => companyId == null || s.CompanyId == companyId)
                .FirstOrDefaultAsync(s => s.SupplierId == id);

            if (supplier == null)
                return NotFound(new { success = false, message = "Supplier not found" });

            supplier.Status = string.Equals(supplier.Status, "Active", StringComparison.OrdinalIgnoreCase) ? "Inactive" : "Active";
            supplier.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = $"Supplier {(supplier.Status == "Active" ? "activated" : "deactivated")}", status = supplier.Status });
        }

        #endregion

        #region ===== INVENTORY TRANSACTIONS =====

        [HttpGet("inventory-transactions")]
        public async Task<IActionResult> GetInventoryTransactions([FromQuery] int? productId)
        {
            var query = _context.InventoryTransactions
                .Include(t => t.Product)
                .Include(t => t.CreatedByUser)
                .AsQueryable();

            if (productId.HasValue)
                query = query.Where(t => t.ProductId == productId.Value);

            // Filter by company via product
            var companyId = GetCompanyId();
            if (companyId != null)
                query = query.Where(t => t.Product != null && t.Product.CompanyId == companyId);

            var transactions = await query
                .OrderByDescending(t => t.TransactionDate)
                .Select(t => new
                {
                    t.TransactionId,
                    t.ProductId,
                    ProductName = t.Product != null ? t.Product.ProductName : "N/A",
                    ProductCode = t.Product != null ? t.Product.ProductCode : "",
                    t.TransactionType,
                    t.Quantity,
                    t.PreviousStock,
                    t.NewStock,
                    t.UnitCost,
                    t.TotalCost,
                    t.ReferenceType,
                    t.ReferenceId,
                    t.Notes,
                    t.TransactionDate,
                    CreatedBy = t.CreatedByUser != null ? t.CreatedByUser.FirstName + " " + t.CreatedByUser.LastName : "System"
                })
                .Take(100)
                .ToListAsync();

            return Ok(new { success = true, data = transactions });
        }

        #endregion

        #region ===== ACTIVITY LOGS =====

        [HttpGet("activity-logs")]
        public async Task<IActionResult> GetActivityLogs([FromQuery] string? module, [FromQuery] string? userType, [FromQuery] int? limit)
        {
            var query = GetScopedActivityLogQuery();

            if (!string.IsNullOrEmpty(module))
                query = query.Where(a => a.Module == module);

            query = ApplyUserTypeFilter(query, userType);

            var logs = await query
                .OrderByDescending(a => a.CreatedAt)
                .Take(limit ?? 50)
                .Select(a => new
                {
                    a.LogId,
                    a.UserId,
                    a.UserName,
                    a.Action,
                    a.Module,
                    a.EntityType,
                    a.EntityId,
                    a.Description,
                    UserType = !a.UserId.HasValue
                        ? "System"
                        : (_context.Users.Where(u => u.UserId == a.UserId)
                            .Select(u => u.RoleId)
                            .FirstOrDefault() == 7 ? "Customer" : "Staff"),
                    a.CreatedAt
                })
                .ToListAsync();

            return Ok(new { success = true, data = logs });
        }

        #endregion

        #region Audit Trail & Activity Logs

        [HttpGet("audit-logs")]
        public async Task<IActionResult> GetAuditLogs([FromQuery] string? module, [FromQuery] string? action, [FromQuery] string? userType,
            [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var query = GetScopedActivityLogQuery();

                if (!string.IsNullOrEmpty(module))
                    query = query.Where(a => a.Module == module);

                if (!string.IsNullOrEmpty(action))
                    query = query.Where(a => a.Action == action);

                query = ApplyUserTypeFilter(query, userType);

                if (startDate.HasValue)
                    query = query.Where(a => a.CreatedAt >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(a => a.CreatedAt <= endDate.Value.AddDays(1));

                var totalCount = await query.CountAsync();
                var logs = await query
                    .OrderByDescending(a => a.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(a => new
                    {
                        a.LogId,
                        a.UserId,
                        a.UserName,
                        a.Action,
                        a.Module,
                        a.EntityType,
                        a.EntityId,
                        a.Description,
                        UserType = !a.UserId.HasValue
                            ? "System"
                            : (_context.Users.Where(u => u.UserId == a.UserId)
                                .Select(u => u.RoleId)
                                .FirstOrDefault() == 7 ? "Customer" : "Staff"),
                        a.IPAddress,
                        a.CreatedAt
                    })
                    .ToListAsync();

                return Ok(new { 
                    success = true, 
                    data = logs, 
                    totalCount, 
                    page, 
                    pageSize,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("audit-logs/modules")]
        public async Task<IActionResult> GetAuditModules()
        {
            var modules = await GetScopedActivityLogQuery()
                .Select(a => a.Module)
                .Distinct()
                .OrderBy(m => m)
                .ToListAsync();
            return Ok(modules);
        }

        [HttpGet("audit-logs/actions")]
        public async Task<IActionResult> GetAuditActions()
        {
            var actions = await GetScopedActivityLogQuery()
                .Select(a => a.Action)
                .Distinct()
                .OrderBy(a => a)
                .ToListAsync();
            return Ok(actions);
        }

        #endregion

        #region Export APIs

        [HttpGet("export/orders")]
        public async Task<IActionResult> ExportOrders([FromQuery] string format = "csv")
        {
            var companyId = GetCompanyId();
            var orders = await _context.Orders
                .Where(o => companyId == null || o.CompanyId == companyId)
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            if (format == "csv")
            {
                var csv = new StringBuilder();
                csv.AppendLine("Order #,Customer,Date,Items,Subtotal,Discount,Tax,Shipping,Total,Status,Payment Status,Payment Method");
                foreach (var o in orders)
                {
                    csv.AppendLine($"\"{o.OrderNumber}\",\"{o.Customer?.FirstName} {o.Customer?.LastName}\",\"{o.OrderDate:yyyy-MM-dd}\",{o.OrderItems.Count},{o.Subtotal:F2},{o.DiscountAmount:F2},{o.TaxAmount:F2},{o.ShippingAmount:F2},{o.TotalAmount:F2},\"{o.OrderStatus}\",\"{o.PaymentStatus}\",\"{o.PaymentMethod}\"");
                }
                return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"orders_{DateTime.Now:yyyyMMdd}.csv");
            }

            return Ok(orders);
        }

        [HttpGet("export/products")]
        public async Task<IActionResult> ExportProducts([FromQuery] string format = "csv")
        {
            var companyId = GetCompanyId();
            var products = await _context.Products
                .Where(p => companyId == null || p.CompanyId == companyId)
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .OrderBy(p => p.ProductName)
                .ToListAsync();

            if (format == "csv")
            {
                var csv = new StringBuilder();
                csv.AppendLine("SKU,Product Name,Category,Brand,Cost Price,Selling Price,Stock,Reorder Level,Status");
                foreach (var p in products)
                {
                    csv.AppendLine($"\"{p.SKU}\",\"{p.ProductName}\",\"{p.Category?.CategoryName}\",\"{p.Brand?.BrandName}\",{p.CostPrice:F2},{p.SellingPrice:F2},{p.StockQuantity},{p.ReorderLevel},\"{p.Status}\"");
                }
                return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"products_{DateTime.Now:yyyyMMdd}.csv");
            }

            return Ok(products);
        }

        [HttpGet("export/customers")]
        public async Task<IActionResult> ExportCustomers([FromQuery] string format = "csv")
        {
            var companyId = GetCompanyId();
            var customers = await _context.Customers
                .Where(c => companyId == null || c.CompanyId == companyId)
                .Include(c => c.Category)
                .OrderBy(c => c.LastName)
                .ToListAsync();

            if (format == "csv")
            {
                var csv = new StringBuilder();
                csv.AppendLine("Customer #,First Name,Last Name,Email,Phone,Category,Total Orders,Total Spent,Status,Created");
                foreach (var c in customers)
                {
                    csv.AppendLine($"\"{c.CustomerCode}\",\"{c.FirstName}\",\"{c.LastName}\",\"{c.Email}\",\"{c.Phone}\",\"{c.Category?.CategoryName}\",{c.TotalOrders},{c.TotalSpent:F2},\"{c.Status}\",\"{c.CreatedAt:yyyy-MM-dd}\"");
                }
                return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"customers_{DateTime.Now:yyyyMMdd}.csv");
            }

            return Ok(customers);
        }

        [HttpGet("export/invoices")]
        public async Task<IActionResult> ExportInvoices([FromQuery] string format = "csv")
        {
            var companyId = GetCompanyId();
            var invoices = await _context.Invoices
                .Where(i => companyId == null || i.CompanyId == companyId)
                .Include(i => i.Customer)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();

            if (format == "csv")
            {
                var csv = new StringBuilder();
                csv.AppendLine("Invoice #,Customer,Date,Due Date,Subtotal,Tax,Discount,Total,Paid,Balance,Status");
                foreach (var i in invoices)
                {
                    csv.AppendLine($"\"{i.InvoiceNumber}\",\"{i.Customer?.FirstName} {i.Customer?.LastName}\",\"{i.InvoiceDate:yyyy-MM-dd}\",\"{i.DueDate:yyyy-MM-dd}\",{i.Subtotal:F2},{i.TaxAmount:F2},{i.DiscountAmount:F2},{i.TotalAmount:F2},{i.PaidAmount:F2},{i.BalanceDue:F2},\"{i.Status}\"");
                }
                return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"invoices_{DateTime.Now:yyyyMMdd}.csv");
            }

            return Ok(invoices);
        }

        [HttpGet("export/audit-logs")]
        public async Task<IActionResult> ExportAuditLogs([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null, [FromQuery] string? userType = null, [FromQuery] string format = "csv")
        {
            var query = GetScopedActivityLogQuery();

            query = ApplyUserTypeFilter(query, userType);

            if (startDate.HasValue)
                query = query.Where(a => a.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.CreatedAt <= endDate.Value.AddDays(1));

            var logs = await query
                .OrderByDescending(a => a.CreatedAt)
                .Take(10000) // Limit to 10k records
                .ToListAsync();

            if (format == "csv")
            {
                var userIds = logs.Where(a => a.UserId.HasValue).Select(a => a.UserId!.Value).Distinct().ToList();
                var roleMap = await _context.Users
                    .Where(u => userIds.Contains(u.UserId))
                    .Select(u => new { u.UserId, u.RoleId })
                    .ToDictionaryAsync(u => u.UserId, u => u.RoleId);

                var csv = new StringBuilder();
                csv.AppendLine("Date/Time,User,User Type,Action,Module,Entity Type,Entity ID,Description,IP Address");
                foreach (var a in logs)
                {
                    var type = !a.UserId.HasValue
                        ? "System"
                        : (roleMap.TryGetValue(a.UserId.Value, out var roleId) && roleId == 7 ? "Customer" : "Staff");
                    csv.AppendLine($"\"{a.CreatedAt:yyyy-MM-dd HH:mm:ss}\",\"{a.UserName}\",\"{type}\",\"{a.Action}\",\"{a.Module}\",\"{a.EntityType}\",{a.EntityId},\"{a.Description?.Replace("\"", "\"\"")}\",\"{a.IPAddress}\"");
                }
                return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"audit_logs_{DateTime.Now:yyyyMMdd}.csv");
            }

            return Ok(logs);
        }

        #endregion

        #region Dashboard Analytics

        [HttpGet("analytics/dashboard")]
        public async Task<IActionResult> GetDashboardAnalytics()
        {
            try
            {
                var companyId = GetCompanyId();
                var now = DateTime.UtcNow;
                var thisMonth = new DateTime(now.Year, now.Month, 1);
                var lastMonth = thisMonth.AddMonths(-1);
                var thisYear = new DateTime(now.Year, 1, 1);

                // Revenue metrics
                var totalRevenue = await _context.Orders
                    .Where(o => o.PaymentStatus == "Paid" && (companyId == null || o.CompanyId == companyId))
                    .SumAsync(o => o.TotalAmount);

                var monthlyRevenue = await _context.Orders
                    .Where(o => o.OrderDate >= thisMonth && o.PaymentStatus == "Paid" && (companyId == null || o.CompanyId == companyId))
                    .SumAsync(o => o.TotalAmount);

                var lastMonthRevenue = await _context.Orders
                    .Where(o => o.OrderDate >= lastMonth && o.OrderDate < thisMonth && o.PaymentStatus == "Paid" && (companyId == null || o.CompanyId == companyId))
                    .SumAsync(o => o.TotalAmount);

                // Order metrics
                var totalOrders = await _context.Orders.Where(o => companyId == null || o.CompanyId == companyId).CountAsync();
                var monthlyOrders = await _context.Orders.Where(o => o.OrderDate >= thisMonth && (companyId == null || o.CompanyId == companyId)).CountAsync();
                var pendingOrders = await _context.Orders.Where(o => o.OrderStatus == "Pending" && (companyId == null || o.CompanyId == companyId)).CountAsync();

                // Customer metrics
                var totalCustomers = await _context.Customers.Where(c => companyId == null || c.CompanyId == companyId).CountAsync();
                var newCustomersThisMonth = await _context.Customers.Where(c => c.CreatedAt >= thisMonth && (companyId == null || c.CompanyId == companyId)).CountAsync();

                // Product metrics
                var totalProducts = await _context.Products.Where(p => companyId == null || p.CompanyId == companyId).CountAsync();
                var lowStockProducts = await _context.Products.Where(p => p.StockQuantity <= p.ReorderLevel && (companyId == null || p.CompanyId == companyId)).CountAsync();
                var outOfStockProducts = await _context.Products.Where(p => p.StockQuantity == 0 && (companyId == null || p.CompanyId == companyId)).CountAsync();

                // Support metrics  
                var openTickets = await _context.SupportTickets.Where(t => t.Status != "Closed" && t.Status != "Resolved" && (companyId == null || t.CompanyId == companyId)).CountAsync();

                // Monthly sales data for chart
                var monthlySales = await _context.Orders
                    .Where(o => o.OrderDate >= thisYear && o.PaymentStatus == "Paid" && (companyId == null || o.CompanyId == companyId))
                    .GroupBy(o => o.OrderDate.Month)
                    .Select(g => new { Month = g.Key, Revenue = g.Sum(o => o.TotalAmount), Orders = g.Count() })
                    .ToListAsync();

                // Top products
                var topProducts = await _context.OrderItems
                    .Include(oi => oi.Order)
                    .Where(oi => oi.Order.OrderDate >= thisMonth && (companyId == null || oi.Order.CompanyId == companyId))
                    .GroupBy(oi => new { oi.ProductId, oi.ProductName })
                    .Select(g => new { g.Key.ProductName, Quantity = g.Sum(x => x.Quantity), Revenue = g.Sum(x => x.TotalPrice) })
                    .OrderByDescending(x => x.Revenue)
                    .Take(5)
                    .ToListAsync();

                // Recent orders
                var recentOrders = await _context.Orders
                    .Where(o => companyId == null || o.CompanyId == companyId)
                    .Include(o => o.Customer)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .Select(o => new { o.OrderNumber, CustomerName = o.Customer != null ? o.Customer.FirstName + " " + o.Customer.LastName : "Guest", o.TotalAmount, o.OrderStatus, o.OrderDate })
                    .ToListAsync();

                var revenueGrowth = lastMonthRevenue > 0 ? ((monthlyRevenue - lastMonthRevenue) / lastMonthRevenue) * 100 : 0;

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        revenue = new { total = totalRevenue, monthly = monthlyRevenue, lastMonth = lastMonthRevenue, growth = revenueGrowth },
                        orders = new { total = totalOrders, monthly = monthlyOrders, pending = pendingOrders },
                        customers = new { total = totalCustomers, newThisMonth = newCustomersThisMonth },
                        products = new { total = totalProducts, lowStock = lowStockProducts, outOfStock = outOfStockProducts },
                        support = new { openTickets },
                        charts = new { monthlySales, topProducts },
                        recentOrders
                    }
                });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Bulk Operations

        [HttpPost("bulk/products/update-status")]
        public async Task<IActionResult> BulkUpdateProductStatus([FromBody] BulkStatusUpdate request)
        {
            try
            {
                var companyId = GetCompanyId();
                var products = await _context.Products
                    .Where(p => request.Ids.Contains(p.ProductId) && (companyId == null || p.CompanyId == companyId))
                    .ToListAsync();

                foreach (var product in products)
                {
                    product.Status = request.Status;
                    product.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                await _auditService.LogAsync("Bulk Update", "Products", "Product", null, $"Bulk updated {products.Count} products status to {request.Status}");

                return Ok(new { success = true, message = $"{products.Count} products updated", count = products.Count });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("bulk/products/delete")]
        public async Task<IActionResult> BulkDeleteProducts([FromBody] BulkDeleteRequest request)
        {
            try
            {
                var companyId = GetCompanyId();
                var products = await _context.Products
                    .Where(p => request.Ids.Contains(p.ProductId) && (companyId == null || p.CompanyId == companyId))
                    .ToListAsync();

                _context.Products.RemoveRange(products);
                await _context.SaveChangesAsync();
                await _auditService.LogAsync("Bulk Delete", "Products", "Product", null, $"Bulk deleted {products.Count} products");

                return Ok(new { success = true, message = $"{products.Count} products deleted", count = products.Count });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("bulk/orders/update-status")]
        public async Task<IActionResult> BulkUpdateOrderStatus([FromBody] BulkStatusUpdate request)
        {
            if (!HasAdminOrderAccess())
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Only admins can bulk update orders." });

            try
            {
                var companyId = GetCompanyId();
                var orders = await _context.Orders
                    .Where(o => request.Ids.Contains(o.OrderId) && (companyId == null || o.CompanyId == companyId))
                    .ToListAsync();

                foreach (var order in orders)
                {
                    order.OrderStatus = request.Status;
                    order.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                await _auditService.LogAsync("Bulk Update", "Orders", "Order", null, $"Bulk updated {orders.Count} orders status to {request.Status}");

                return Ok(new { success = true, message = $"{orders.Count} orders updated", count = orders.Count });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("bulk/customers/update-status")]
        public async Task<IActionResult> BulkUpdateCustomerStatus([FromBody] BulkStatusUpdate request)
        {
            try
            {
                var companyId = GetCompanyId();
                var customers = await _context.Customers
                    .Where(c => request.Ids.Contains(c.CustomerId) && (companyId == null || c.CompanyId == companyId))
                    .ToListAsync();

                foreach (var customer in customers)
                {
                    customer.Status = request.Status;
                    customer.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                await _auditService.LogAsync("Bulk Update", "Customers", "Customer", null, $"Bulk updated {customers.Count} customers status to {request.Status}");

                return Ok(new { success = true, message = $"{customers.Count} customers updated", count = customers.Count });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("bulk/invoices/update-status")]
        public async Task<IActionResult> BulkUpdateInvoiceStatus([FromBody] BulkStatusUpdate request)
        {
            try
            {
                if (!HasFullBillingAccess())
                    return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Invoices are read-only. Update order status instead." });

                var companyId = GetCompanyId();
                var invoices = await _context.Invoices
                    .Where(i => request.Ids.Contains(i.InvoiceId) && (companyId == null || i.CompanyId == companyId))
                    .ToListAsync();

                foreach (var invoice in invoices)
                {
                    invoice.Status = request.Status;
                    invoice.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                await _auditService.LogAsync("Bulk Update", "Invoices", "Invoice", null, $"Bulk updated {invoices.Count} invoices status to {request.Status}");

                return Ok(new { success = true, message = $"{invoices.Count} invoices updated", count = invoices.Count });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Advanced Search

        [HttpGet("search/global")]
        public async Task<IActionResult> GlobalSearch([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return Ok(new { success = true, data = new { products = new List<object>(), customers = new List<object>(), orders = new List<object>() } });

            try
            {
                var companyId = GetCompanyId();
                var searchTerm = q.ToLower();

                // Search products
                var products = await _context.Products
                    .Where(p => (companyId == null || p.CompanyId == companyId) &&
                        (p.ProductName.ToLower().Contains(searchTerm) || 
                         (p.SKU != null && p.SKU.ToLower().Contains(searchTerm)) ||
                         (p.ShortDescription != null && p.ShortDescription.ToLower().Contains(searchTerm))))
                    .Take(10)
                    .Select(p => new { p.ProductId, p.ProductName, p.SKU, p.SellingPrice, ImageUrl = p.MainImageUrl, Type = "Product" })
                    .ToListAsync();

                // Search customers
                var customers = await _context.Customers
                    .Where(c => (companyId == null || c.CompanyId == companyId) &&
                        (c.FirstName.ToLower().Contains(searchTerm) ||
                         c.LastName.ToLower().Contains(searchTerm) ||
                         c.Email.ToLower().Contains(searchTerm) ||
                         (c.CustomerCode != null && c.CustomerCode.ToLower().Contains(searchTerm))))
                    .Take(10)
                    .Select(c => new { c.CustomerId, Name = c.FirstName + " " + c.LastName, c.Email, CustomerNumber = c.CustomerCode, Type = "Customer" })
                    .ToListAsync();

                // Search orders
                var orders = await _context.Orders
                    .Where(o => (companyId == null || o.CompanyId == companyId) &&
                        (o.OrderNumber.ToLower().Contains(searchTerm)))
                    .Take(10)
                    .Select(o => new { o.OrderId, o.OrderNumber, o.TotalAmount, o.OrderStatus, o.OrderDate, Type = "Order" })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = new { products, customers, orders },
                    totalCount = products.Count + customers.Count + orders.Count
                });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("search/products")]
        public async Task<IActionResult> SearchProducts([FromQuery] string? q, [FromQuery] int? categoryId, [FromQuery] int? brandId,
            [FromQuery] decimal? minPrice, [FromQuery] decimal? maxPrice, [FromQuery] string? status, 
            [FromQuery] bool? inStock, [FromQuery] string? sortBy, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var companyId = GetCompanyId();
                var query = _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Where(p => companyId == null || p.CompanyId == companyId);

                // Apply filters
                if (!string.IsNullOrEmpty(q))
                {
                    var searchTerm = q.ToLower();
                    query = query.Where(p => p.ProductName.ToLower().Contains(searchTerm) || 
                                            (p.SKU != null && p.SKU.ToLower().Contains(searchTerm)) ||
                                            (p.ShortDescription != null && p.ShortDescription.ToLower().Contains(searchTerm)));
                }

                if (categoryId.HasValue)
                    query = query.Where(p => p.CategoryId == categoryId);

                if (brandId.HasValue)
                    query = query.Where(p => p.BrandId == brandId);

                if (minPrice.HasValue)
                    query = query.Where(p => p.SellingPrice >= minPrice);

                if (maxPrice.HasValue)
                    query = query.Where(p => p.SellingPrice <= maxPrice);

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(p => p.Status == status);

                if (inStock == true)
                    query = query.Where(p => p.StockQuantity > 0);

                // Apply sorting
                query = sortBy switch
                {
                    "name_asc" => query.OrderBy(p => p.ProductName),
                    "name_desc" => query.OrderByDescending(p => p.ProductName),
                    "price_asc" => query.OrderBy(p => p.SellingPrice),
                    "price_desc" => query.OrderByDescending(p => p.SellingPrice),
                    "stock_asc" => query.OrderBy(p => p.StockQuantity),
                    "stock_desc" => query.OrderByDescending(p => p.StockQuantity),
                    "newest" => query.OrderByDescending(p => p.CreatedAt),
                    _ => query.OrderBy(p => p.ProductName)
                };

                var totalCount = await query.CountAsync();
                var products = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new
                    {
                        p.ProductId,
                        p.SKU,
                        p.ProductName,
                        CategoryName = p.Category != null ? p.Category.CategoryName : null,
                        BrandName = p.Brand != null ? p.Brand.BrandName : null,
                        p.CostPrice,
                        p.SellingPrice,
                        p.StockQuantity,
                        p.ReorderLevel,
                        p.Status,
                        ImageUrl = p.MainImageUrl
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = products,
                    totalCount,
                    page,
                    pageSize,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("search/orders")]
        public async Task<IActionResult> SearchOrders([FromQuery] string? q, [FromQuery] string? status,
            [FromQuery] string? paymentStatus, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate,
            [FromQuery] decimal? minAmount, [FromQuery] decimal? maxAmount, [FromQuery] string? sortBy,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var companyId = GetCompanyId();
                var query = _context.Orders
                    .Include(o => o.Customer)
                    .Where(o => companyId == null || o.CompanyId == companyId);

                // Apply filters
                if (!string.IsNullOrEmpty(q))
                {
                    var searchTerm = q.ToLower();
                    query = query.Where(o => o.OrderNumber.ToLower().Contains(searchTerm) ||
                                            (o.Customer != null && (o.Customer.FirstName.ToLower().Contains(searchTerm) ||
                                            o.Customer.LastName.ToLower().Contains(searchTerm))));
                }

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(o => o.OrderStatus == status);

                if (!string.IsNullOrEmpty(paymentStatus))
                    query = query.Where(o => o.PaymentStatus == paymentStatus);

                if (startDate.HasValue)
                    query = query.Where(o => o.OrderDate >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(o => o.OrderDate <= endDate.Value.AddDays(1));

                if (minAmount.HasValue)
                    query = query.Where(o => o.TotalAmount >= minAmount);

                if (maxAmount.HasValue)
                    query = query.Where(o => o.TotalAmount <= maxAmount);

                // Apply sorting
                query = sortBy switch
                {
                    "date_asc" => query.OrderBy(o => o.OrderDate),
                    "date_desc" => query.OrderByDescending(o => o.OrderDate),
                    "amount_asc" => query.OrderBy(o => o.TotalAmount),
                    "amount_desc" => query.OrderByDescending(o => o.TotalAmount),
                    "newest" => query.OrderByDescending(o => o.OrderDate),
                    _ => query.OrderByDescending(o => o.OrderDate)
                };

                var totalCount = await query.CountAsync();
                var orders = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(o => new
                    {
                        o.OrderId,
                        o.OrderNumber,
                        CustomerName = o.Customer != null ? o.Customer.FirstName + " " + o.Customer.LastName : "Guest",
                        o.OrderDate,
                        o.TotalAmount,
                        o.OrderStatus,
                        o.PaymentStatus,
                        o.PaymentMethod
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = orders,
                    totalCount,
                    page,
                    pageSize,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        #endregion
    }

    // Role Access DTOs
    public class RoleAccessSaveRequest
    {
        public int? CompanyId { get; set; }
        public List<RoleAccessItem> AccessList { get; set; } = new();
    }

    public class RoleAccessItem
    {
        public int RoleId { get; set; }
        public string ModuleCode { get; set; } = string.Empty;
        public bool HasAccess { get; set; }
    }

    // Request DTOs
    public class InvoiceStatusModel
    {
        public string Status { get; set; } = string.Empty;
    }

    public class StockUpdateRequest
    {
        public int NewQuantity { get; set; }
        public string? TransactionType { get; set; }
        public string? Notes { get; set; }
    }

    public class StatusUpdateRequest
    {
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class StatusUpdateDto
    {
        public bool IsActive { get; set; }
    }

    public class StockAdjustmentRequest
    {
        public int ProductId { get; set; }
        public string AdjustmentType { get; set; } = "Add"; // "Add" or "Deduct"
        public int Quantity { get; set; }
        public string? Notes { get; set; }
    }

    public class StockAlertRequest
    {
        public int ProductId { get; set; }
        public string AlertType { get; set; } = "Low Stock";
        public int CurrentStock { get; set; }
        public int ThresholdLevel { get; set; } = 15;
    }

    public class PurchaseOrderRequest
    {
        public int SupplierId { get; set; }
        public string OrderDate { get; set; } = string.Empty;
        public string? ExpectedDelivery { get; set; }
        public string? Notes { get; set; }
        public List<PurchaseOrderItemRequest> Items { get; set; } = new();
    }

    public class PurchaseOrderItemRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    // Live Chat Request DTOs
    public class ChatSessionRequest
    {
        public int SessionId { get; set; }
    }

    public class TransferChatRequest
    {
        public int SessionId { get; set; }
        public int ToAgentId { get; set; }
        public string? Reason { get; set; }
    }

    public class AgentChatMessageRequest
    {
        public int SessionId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class CustomerChatMessageRequest
    {
        public int SessionId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class RequestLiveAgentRequest
    {
        public string? VisitorId { get; set; }
    }

    public class KnowledgeArticleCreateRequest
    {
        public int CategoryId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public string? Tags { get; set; }
        public string? Status { get; set; }
    }

    public class TicketResponseRequest
    {
        public string Message { get; set; } = string.Empty;
        public string? Status { get; set; }
        public string? InternalNotes { get; set; }
    }

    public class TicketEscalationRequest
    {
        public string Reason { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    // Bulk Operation DTOs
    public class BulkStatusUpdate
    {
        public List<int> Ids { get; set; } = new();
        public string Status { get; set; } = string.Empty;
    }

    public class BulkDeleteRequest
    {
        public List<int> Ids { get; set; } = new();
    }

}
