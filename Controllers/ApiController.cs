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
                existing.CostPrice = product.CostPrice;
                existing.SellingPrice = product.SellingPrice;
                existing.CompareAtPrice = product.CompareAtPrice;
                existing.StockQuantity = product.StockQuantity;
                existing.ReorderLevel = product.ReorderLevel;
                existing.Status = product.Status;
                existing.IsFeatured = product.IsFeatured;
                existing.IsOnSale = product.IsOnSale;
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
}
