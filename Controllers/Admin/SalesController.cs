using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using CompuGear.Data;
using CompuGear.Models;
using CompuGear.Services;

namespace CompuGear.Controllers
{
    /// <summary>
    /// Sales Controller for Admin - Uses Views/Admin/Sales folder
    /// RoleId: 1 - Super Admin, 2 - Company Admin
    /// </summary>
    public class SalesController : Controller
    {
        private readonly CompuGearDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IAuditService _auditService;

        public SalesController(CompuGearDbContext context, IConfiguration configuration, IAuditService auditService)
        {
            _context = context;
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

        private int? GetCompanyId()
        {
            var roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId == 1) return null;
            return HttpContext.Session.GetInt32("CompanyId");
        }

        private int? GetRoleId()
        {
            return HttpContext.Session.GetInt32("RoleId");
        }

        private bool HasAdminOrderAccess()
        {
            var roleId = GetRoleId();
            return roleId == 1 || roleId == 2;
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

        #region View Actions

        public IActionResult Orders()
        {
            return View("~/Views/Admin/Sales/Orders.cshtml");
        }

        public IActionResult Leads()
        {
            return View("~/Views/Admin/Sales/Leads.cshtml");
        }

        public IActionResult LeadsArchive()
        {
            return View("~/Views/Admin/Sales/LeadsArchive.cshtml");
        }

        public IActionResult Reports()
        {
            return View("~/Views/Admin/Sales/Reports.cshtml");
        }

        #endregion

        #region Orders API

        [HttpGet]
        [Route("api/orders")]
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

        [HttpGet]
        [Route("api/orders/{id}")]
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

        [HttpPost]
        [Route("api/orders")]
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

        [HttpPut]
        [Route("api/orders/{id}")]
        public async Task<IActionResult> UpdateOrder(int id, [FromBody] Order order)
        {
            if (!HasAdminOrderAccess())
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Only admins can update orders." });

            try
            {
                var companyId = GetCompanyId();
                var existing = await _context.Orders.FindAsync(id);
                if (existing == null) return NotFound();
                if (companyId != null && existing.CompanyId != null && existing.CompanyId != companyId) return NotFound();

                if (existing.CompanyId == null && companyId != null)
                    existing.CompanyId = companyId;

                existing.OrderStatus = order.OrderStatus;
                existing.PaymentStatus = order.PaymentStatus;
                existing.PaymentMethod = order.PaymentMethod;
                existing.ShippingMethod = order.ShippingMethod;
                existing.TrackingNumber = order.TrackingNumber;
                existing.Notes = order.Notes;
                existing.UpdatedAt = DateTime.UtcNow;

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

        [HttpPut]
        [Route("api/orders/{id}/status")]
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

                if (request.Status == "Confirmed" && !order.ConfirmedAt.HasValue)
                    order.ConfirmedAt = DateTime.UtcNow;
                if (request.Status == "Shipped" && !order.ShippedAt.HasValue)
                    order.ShippedAt = DateTime.UtcNow;
                if (request.Status == "Delivered" && !order.DeliveredAt.HasValue)
                    order.DeliveredAt = DateTime.UtcNow;
                if (request.Status == "Cancelled" && !order.CancelledAt.HasValue)
                    order.CancelledAt = DateTime.UtcNow;

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

        [HttpPut]
        [Route("api/orders/{id}/approve")]
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

                _context.OrderStatusHistory.Add(new OrderStatusHistory
                {
                    OrderId = id,
                    PreviousStatus = previousStatus,
                    NewStatus = "Confirmed",
                    Notes = "Order approved by admin",
                    ChangedAt = DateTime.UtcNow
                });

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

                            order.PaymentReference = checkoutSessionId;
                            order.UpdatedAt = DateTime.UtcNow;
                            await _context.SaveChangesAsync();
                        }
                    }
                    catch (Exception payEx)
                    {
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

        [HttpPut]
        [Route("api/orders/{id}/reject")]
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

                var orderItems = await _context.OrderItems.Where(oi => oi.OrderId == id).ToListAsync();
                foreach (var item in orderItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity += item.Quantity;
                    }
                }

                _context.OrderStatusHistory.Add(new OrderStatusHistory
                {
                    OrderId = id,
                    PreviousStatus = previousStatus,
                    NewStatus = "Cancelled",
                    Notes = request?.Notes ?? "Order rejected by admin",
                    ChangedAt = DateTime.UtcNow
                });

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

        [HttpDelete]
        [Route("api/orders/{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            if (!HasAdminOrderAccess())
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Only admins can delete orders." });

            var companyId = GetCompanyId();
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();
            if (companyId != null && order.CompanyId != null && order.CompanyId != companyId) return NotFound();

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Order deleted successfully" });
        }

        #endregion

        #region Leads API

        [HttpGet]
        [Route("api/leads")]
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

        [HttpGet]
        [Route("api/leads/{id}")]
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

        [HttpPost]
        [Route("api/leads")]
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

        [HttpPut]
        [Route("api/leads/{id}")]
        public async Task<IActionResult> UpdateLead(int id, [FromBody] Lead lead)
        {
            try
            {
                var companyId = GetCompanyId();
                var existing = await _context.Leads.FindAsync(id);
                if (existing == null) return NotFound();
                if (companyId != null && existing.CompanyId != null && existing.CompanyId != companyId) return NotFound();

                if (existing.CompanyId == null && companyId != null)
                    existing.CompanyId = companyId;

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

        [HttpPut]
        [Route("api/leads/{id}/convert")]
        public async Task<IActionResult> ConvertLead(int id)
        {
            try
            {
                var companyId = GetCompanyId();
                var lead = await _context.Leads.FindAsync(id);
                if (lead == null) return NotFound();
                if (companyId != null && lead.CompanyId != null && lead.CompanyId != companyId) return NotFound();

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

        [HttpDelete]
        [Route("api/leads/{id}")]
        public async Task<IActionResult> DeleteLead(int id)
        {
            var companyId = GetCompanyId();
            var lead = await _context.Leads.FindAsync(id);
            if (lead == null) return NotFound();
            if (companyId != null && lead.CompanyId != null && lead.CompanyId != companyId) return NotFound();

            _context.Leads.Remove(lead);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Lead deleted successfully" });
        }

        [HttpPut]
        [Route("api/leads/{id}/toggle-status")]
        public async Task<IActionResult> ToggleLeadStatus(int id)
        {
            try
            {
                var companyId = GetCompanyId();
                var lead = await _context.Leads.FindAsync(id);
                if (lead == null) return NotFound();
                if (companyId != null && lead.CompanyId != null && lead.CompanyId != companyId) return NotFound();

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
    }
}
