using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CompuGear.Data;
using CompuGear.Models;

namespace CompuGear.Controllers
{
    [Route("api")]
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly CompuGearDbContext _context;

        public ApiController(CompuGearDbContext context)
        {
            _context = context;
        }

        #region Marketing - Campaigns

        [HttpGet("campaigns")]
        public async Task<IActionResult> GetCampaigns()
        {
            try
            {
                var campaigns = await _context.Campaigns
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
            var campaign = await _context.Campaigns.FindAsync(id);
            if (campaign == null) return NotFound();
            return Ok(campaign);
        }

        [HttpPost("campaigns")]
        public async Task<IActionResult> CreateCampaign([FromBody] Campaign campaign)
        {
            try
            {
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
            try
            {
                var existing = await _context.Campaigns.FindAsync(id);
                if (existing == null) return NotFound();

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
            var campaign = await _context.Campaigns.FindAsync(id);
            if (campaign == null) return NotFound();

            _context.Campaigns.Remove(campaign);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Campaign deleted successfully" });
        }

        #endregion

        #region Marketing - Promotions

        [HttpGet("promotions")]
        public async Task<IActionResult> GetPromotions()
        {
            try
            {
                var promotions = await _context.Promotions
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
            try
            {
                var now = DateTime.Now;
                var promotions = await _context.Promotions
                    .Where(p => p.IsActive && p.StartDate <= now && p.EndDate >= now)
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
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null) return NotFound();
            return Ok(promotion);
        }

        [HttpPost("promotions")]
        public async Task<IActionResult> CreatePromotion([FromBody] Promotion promotion)
        {
            try
            {
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
            try
            {
                var existing = await _context.Promotions.FindAsync(id);
                if (existing == null) return NotFound();

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
            try
            {
                var promotion = await _context.Promotions.FindAsync(id);
                if (promotion == null) return NotFound();

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

        [HttpDelete("promotions/{id}")]
        public async Task<IActionResult> DeletePromotion(int id)
        {
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null) return NotFound();

            _context.Promotions.Remove(promotion);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Promotion deleted successfully" });
        }

        #endregion

        #region Customers

        [HttpGet("customers")]
        public async Task<IActionResult> GetCustomers()
        {
            try
            {
                var customers = await _context.Customers
                    .Include(c => c.Category)
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
                var customer = await _context.Customers
                    .Include(c => c.Category)
                    .FirstOrDefaultAsync(c => c.CustomerId == id);
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
                var existing = await _context.Customers.FindAsync(id);
                if (existing == null) return NotFound();

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
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound();

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Customer deleted successfully" });
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
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.Supplier)
                    .OrderByDescending(p => p.CreatedAt)
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
                var product = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .FirstOrDefaultAsync(p => p.ProductId == id);
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
                var existing = await _context.Products.FindAsync(id);
                if (existing == null) return NotFound();

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

        [HttpPut("products/{id}/stock")]
        public async Task<IActionResult> UpdateStock(int id, [FromBody] StockUpdateRequest request)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
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
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

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
                var category = await _context.ProductCategories.FindAsync(id);
                if (category == null)
                    return NotFound(new { success = false, message = "Category not found" });

                category.IsActive = status.IsActive;
                category.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = $"Category {(status.IsActive ? "activated" : "deactivated")} successfully" });
            }
            catch (Exception ex)
            {
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
                var alerts = await _context.StockAlerts
                    .Include(a => a.Product)
                    .Where(a => !a.IsResolved)
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
                var orders = await _context.PurchaseOrders
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

        [HttpPost("purchase-orders")]
        public async Task<IActionResult> CreatePurchaseOrder([FromBody] PurchaseOrderRequest request)
        {
            try
            {
                var purchaseOrder = new PurchaseOrder
                {
                    SupplierId = request.SupplierId,
                    OrderDate = DateTime.Parse(request.OrderDate),
                    ExpectedDeliveryDate = !string.IsNullOrEmpty(request.ExpectedDelivery) ? DateTime.Parse(request.ExpectedDelivery) : null,
                    Status = "Pending",
                    Notes = request.Notes,
                    CreatedAt = DateTime.UtcNow
                };

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
                var order = await _context.PurchaseOrders.FindAsync(id);
                if (order == null)
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
                var order = await _context.PurchaseOrders.FindAsync(id);
                if (order == null)
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
                var order = await _context.PurchaseOrders
                    .Include(po => po.Items)
                    .FirstOrDefaultAsync(po => po.PurchaseOrderId == id);

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
                var orders = await _context.Orders
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
                        o.PaymentMethod,
                        o.ShippingCity,
                        ItemCount = o.OrderItems.Count,
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
                var order = await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderItems)
                        .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(o => o.OrderId == id);

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
            try
            {
                var existing = await _context.Orders.FindAsync(id);
                if (existing == null) return NotFound();

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
            try
            {
                var order = await _context.Orders.FindAsync(id);
                if (order == null) return NotFound();

                var previousStatus = order.OrderStatus;
                order.OrderStatus = request.Status;
                order.UpdatedAt = DateTime.UtcNow;

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

        [HttpDelete("orders/{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

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
                var leads = await _context.Leads
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
                var lead = await _context.Leads.FindAsync(id);
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
                var existing = await _context.Leads.FindAsync(id);
                if (existing == null) return NotFound();

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
                var lead = await _context.Leads.FindAsync(id);
                if (lead == null) return NotFound();

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
            var lead = await _context.Leads.FindAsync(id);
            if (lead == null) return NotFound();

            _context.Leads.Remove(lead);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Lead deleted successfully" });
        }

        #endregion

        #region Support Tickets

        [HttpGet("tickets")]
        public async Task<IActionResult> GetTickets()
        {
            try
            {
                var tickets = await _context.SupportTickets
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
                var ticket = await _context.SupportTickets
                    .Include(t => t.Customer)
                    .Include(t => t.Category)
                    .Include(t => t.Messages)
                    .FirstOrDefaultAsync(t => t.TicketId == id);

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
                var existing = await _context.SupportTickets.FindAsync(id);
                if (existing == null) return NotFound();

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
                var ticket = await _context.SupportTickets.FindAsync(id);
                if (ticket == null) return NotFound();

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
                var ticket = await _context.SupportTickets.FindAsync(id);
                if (ticket == null) return NotFound();

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
                var ticket = await _context.SupportTickets.FindAsync(id);
                if (ticket == null) return NotFound();

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
            var ticket = await _context.SupportTickets.FindAsync(id);
            if (ticket == null) return NotFound();

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
                var users = await _context.Users
                    .Include(u => u.Role)
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
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == id);
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
                var existing = await _context.Users.FindAsync(id);
                if (existing == null) return NotFound();

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
                var user = await _context.Users.FindAsync(id);
                if (user == null) return NotFound();

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
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

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

        #region Billing - Invoices

        [HttpGet("invoices")]
        public async Task<IActionResult> GetInvoices()
        {
            try
            {
                var invoices = await _context.Invoices
                    .Include(i => i.Customer)
                    .OrderByDescending(i => i.CreatedAt)
                    .Select(i => new
                    {
                        i.InvoiceId,
                        i.InvoiceNumber,
                        i.CustomerId,
                        CustomerName = i.Customer != null ? i.Customer.FirstName + " " + i.Customer.LastName : "",
                        i.InvoiceDate,
                        i.DueDate,
                        i.Status,
                        i.Subtotal,
                        i.TaxAmount,
                        i.TotalAmount,
                        i.PaidAmount,
                        Balance = i.TotalAmount - i.PaidAmount,
                        i.Notes,
                        i.CreatedAt
                    })
                    .ToListAsync();

                return Ok(invoices);
            }
            catch (Exception)
            {
                return Ok(new List<object>());
            }
        }

        [HttpGet("invoices/{id}")]
        public async Task<IActionResult> GetInvoice(int id)
        {
            try
            {
                var invoice = await _context.Invoices
                    .Include(i => i.Customer)
                    .Include(i => i.Items)
                    .FirstOrDefaultAsync(i => i.InvoiceId == id);

                if (invoice == null) return NotFound();
                return Ok(invoice);
            }
            catch (Exception)
            {
                return NotFound();
            }
        }

        [HttpPost("invoices")]
        public async Task<IActionResult> CreateInvoice([FromBody] Invoice invoice)
        {
            try
            {
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
                var existing = await _context.Invoices.FindAsync(id);
                if (existing == null) return NotFound();

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
                var invoice = await _context.Invoices.FindAsync(id);
                if (invoice == null) return NotFound();

                _context.Invoices.Remove(invoice);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Invoice deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Billing - Payments

        [HttpGet("payments")]
        public async Task<IActionResult> GetPayments()
        {
            try
            {
                var payments = await _context.Payments
                    .Include(p => p.Customer)
                    .Include(p => p.Invoice)
                    .OrderByDescending(p => p.PaymentDate)
                    .Select(p => new
                    {
                        p.PaymentId,
                        p.PaymentNumber,
                        p.CustomerId,
                        p.InvoiceId,
                        CustomerName = p.Customer != null ? p.Customer.FirstName + " " + p.Customer.LastName : "",
                        InvoiceNumber = p.Invoice != null ? p.Invoice.InvoiceNumber : "",
                        p.PaymentDate,
                        p.Amount,
                        PaymentMethod = p.PaymentMethodType,
                        p.Status,
                        TransactionReference = p.ReferenceNumber,
                        p.Notes,
                        p.CreatedAt
                    })
                    .ToListAsync();

                return Ok(payments);
            }
            catch (Exception)
            {
                return Ok(new List<object>());
            }
        }

        [HttpPost("payments")]
        public async Task<IActionResult> CreatePayment([FromBody] Payment payment)
        {
            try
            {
                payment.PaymentNumber = $"PAY-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
                payment.PaymentDate = DateTime.UtcNow;
                payment.CreatedAt = DateTime.UtcNow;

                _context.Payments.Add(payment);

                // Update invoice paid amount
                if (payment.InvoiceId.HasValue)
                {
                    var invoice = await _context.Invoices.FindAsync(payment.InvoiceId.Value);
                    if (invoice != null)
                    {
                        invoice.PaidAmount += payment.Amount;
                        if (invoice.PaidAmount >= invoice.TotalAmount)
                            invoice.Status = "Paid";
                        else if (invoice.PaidAmount > 0)
                            invoice.Status = "Partial";
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

        #region Suppliers

        [HttpGet("suppliers")]
        public async Task<IActionResult> GetSuppliers()
        {
            try
            {
                var suppliers = await _context.Suppliers
                    .OrderBy(s => s.SupplierName)
                    .Select(s => new
                    {
                        s.SupplierId,
                        s.SupplierCode,
                        s.SupplierName,
                        s.ContactPerson,
                        s.Email,
                        s.Phone,
                        s.Status
                    })
                    .ToListAsync();

                return Ok(suppliers);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("suppliers/{id}")]
        public async Task<IActionResult> GetSupplier(int id)
        {
            try
            {
                var supplier = await _context.Suppliers.FindAsync(id);
                if (supplier == null) return NotFound();
                return Ok(supplier);
            }
            catch (Exception)
            {
                return NotFound();
            }
        }

        [HttpGet("suppliers/{id}/products")]
        public async Task<IActionResult> GetSupplierProducts(int id)
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Where(p => p.SupplierId == id)
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

        [HttpPost("suppliers")]
        public async Task<IActionResult> CreateSupplier([FromBody] Supplier supplier)
        {
            try
            {
                supplier.SupplierCode = $"SUP-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
                supplier.CreatedAt = DateTime.UtcNow;
                supplier.UpdatedAt = DateTime.UtcNow;

                _context.Suppliers.Add(supplier);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Supplier created successfully", data = supplier });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPut("suppliers/{id}")]
        public async Task<IActionResult> UpdateSupplier(int id, [FromBody] Supplier supplier)
        {
            try
            {
                var existing = await _context.Suppliers.FindAsync(id);
                if (existing == null) return NotFound();

                existing.SupplierName = supplier.SupplierName;
                existing.ContactPerson = supplier.ContactPerson;
                existing.Email = supplier.Email;
                existing.Phone = supplier.Phone;
                existing.Address = supplier.Address;
                existing.City = supplier.City;
                existing.Country = supplier.Country;
                existing.Website = supplier.Website;
                existing.PaymentTerms = supplier.PaymentTerms;
                existing.Status = supplier.Status;
                existing.Rating = supplier.Rating;
                existing.Notes = supplier.Notes;
                existing.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Supplier updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        #endregion

        #region Stock Adjustments

        [HttpPost("stock-adjustments")]
        public async Task<IActionResult> CreateStockAdjustment([FromBody] StockAdjustmentRequest request)
        {
            try
            {
                var product = await _context.Products.FindAsync(request.ProductId);
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
                var adjustments = await _context.InventoryTransactions
                    .Include(t => t.Product)
                    .Where(t => t.ReferenceType == "Stock Adjustment")
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

            var stats = new
            {
                TotalCustomers = await _context.Customers.CountAsync(),
                TotalOrders = await _context.Orders.CountAsync(),
                TotalRevenue = await _context.Orders.Where(o => o.PaymentStatus == "Paid").SumAsync(o => o.TotalAmount),
                PendingOrders = await _context.Orders.CountAsync(o => o.OrderStatus == "Pending"),
                OpenTickets = await _context.SupportTickets.CountAsync(t => t.Status == "Open" || t.Status == "In Progress"),
                LowStockProducts = await _context.Products.CountAsync(p => p.StockQuantity <= p.ReorderLevel),
                ActiveCampaigns = await _context.Campaigns.CountAsync(c => c.Status == "Active"),
                MonthlyRevenue = await _context.Orders
                    .Where(o => o.OrderDate >= thisMonth && o.PaymentStatus == "Paid")
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

                var session = await _context.ChatSessions.FindAsync(request.SessionId);
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

                return Ok(new { success = true, message = "Chat accepted" });
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
                var session = await _context.ChatSessions.FindAsync(request.SessionId);
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

                var session = await _context.ChatSessions.FindAsync(request.SessionId);
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

                var session = await _context.ChatSessions.FindAsync(request.SessionId);
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

        // Customer requests live agent
        [HttpPost("RequestLiveAgent")]
        public async Task<IActionResult> RequestLiveAgent([FromBody] RequestLiveAgentRequest request)
        {
            try
            {
                var customerId = HttpContext.Session.GetInt32("CustomerId");

                // Find existing pending session or create new one
                var session = await _context.ChatSessions
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

                var session = await _context.ChatSessions.FindAsync(request.SessionId);
                if (session == null)
                    return Ok(new { success = false, message = "Chat session not found" });

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
                var session = await _context.ChatSessions.FindAsync(request.SessionId);
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

        #endregion
    }

    // Request DTOs
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

}
