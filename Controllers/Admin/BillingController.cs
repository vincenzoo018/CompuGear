using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using CompuGear.Data;
using CompuGear.Models;
using CompuGear.Services;

namespace CompuGear.Controllers
{
    /// <summary>
    /// Billing Controller for Admin - Uses Views/Admin/Billing folder
    /// RoleId: 1 - Super Admin, 2 - Company Admin
    /// </summary>
    public class BillingController : Controller
    {
        private readonly CompuGearDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IAuditService _auditService;

        public BillingController(CompuGearDbContext context, IConfiguration configuration, IAuditService auditService)
        {
            _context = context;
            _configuration = configuration;
            _auditService = auditService;
        }

        // Authorization check - Admins for views, all authenticated staff for API
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            var roleId = HttpContext.Session.GetInt32("RoleId");
            var isApiRequest = HttpContext.Request.Path.StartsWithSegments("/api");

            if (roleId == null)
            {
                context.Result = isApiRequest
                    ? new JsonResult(new { success = false, message = "Not authenticated" }) { StatusCode = 401 }
                    : RedirectToAction("Login", "Auth");
                return;
            }

            // API endpoints: allow all authenticated staff + admin roles (data is company-scoped)
            if (isApiRequest) return;

            // View endpoints: admin only
            if (roleId != 1 && roleId != 2)
            {
                context.Result = RedirectToAction("Login", "Auth");
            }
        }

        #region Helpers

        private int? GetCompanyId()
        {
            var roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId == 1) return null;
            return HttpContext.Session.GetInt32("CompanyId");
        }

        private bool HasFullBillingAccess()
        {
            return false;
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
                if (!invoice.PaidAt.HasValue) invoice.PaidAt = DateTime.UtcNow;
            }
            else
            {
                invoice.PaidAmount = Math.Min(invoice.PaidAmount, invoice.TotalAmount);
                invoice.BalanceDue = Math.Max(0, invoice.TotalAmount - invoice.PaidAmount);
                if (invoice.PaidAmount >= invoice.TotalAmount)
                {
                    invoice.Status = "Paid";
                    if (!invoice.PaidAt.HasValue) invoice.PaidAt = DateTime.UtcNow;
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

        #endregion

        #region Views

        public IActionResult Invoices()
        {
            return View("~/Views/Admin/Billing/Invoices.cshtml");
        }

        public IActionResult Payments()
        {
            return View("~/Views/Admin/Billing/Payments.cshtml");
        }

        public IActionResult Summary()
        {
            return View("~/Views/Admin/Billing/Summary.cshtml");
        }

        public IActionResult Records()
        {
            return View("~/Views/Admin/Billing/Records.cshtml");
        }

        #endregion

        #region Billing - Invoices (Create/Update/Delete/PDF/Financial)

        [HttpPost]
        [Route("api/invoices")]
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

        [HttpPut]
        [Route("api/invoices/{id}")]
        public async Task<IActionResult> UpdateInvoice(int id, [FromBody] Invoice invoice)
        {
            try
            {
                if (!HasFullBillingAccess())
                    return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Invoices are read-only. Update order status instead." });

                var companyId = GetCompanyId();
                var existing = await _context.Invoices.FindAsync(id);
                if (existing == null) return NotFound();
                if (companyId != null && existing.CompanyId != null && existing.CompanyId != companyId) return NotFound();

                // Assign CompanyId if not set (legacy data migration)
                if (existing.CompanyId == null && companyId != null)
                    existing.CompanyId = companyId;

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

        [HttpDelete]
        [Route("api/invoices/{id}")]
        public async Task<IActionResult> DeleteInvoice(int id)
        {
            try
            {
                if (!HasFullBillingAccess())
                    return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Invoices are read-only. Update order status instead." });

                var companyId = GetCompanyId();
                var invoice = await _context.Invoices.FindAsync(id);
                if (invoice == null) return NotFound();
                if (companyId != null && invoice.CompanyId != null && invoice.CompanyId != companyId) return NotFound();

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
        [HttpGet]
        [Route("api/invoices/{id}/pdf")]
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
        [HttpGet]
        [Route("api/reports/financial")]
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

        [HttpPost]
        [Route("api/payments")]
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

        #region ===== INVOICES =====

        [HttpGet]
        [Route("api/invoices")]
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

        [HttpGet]
        [Route("api/invoices/{id}")]
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

        [HttpPut]
        [Route("api/invoices/{id}/status")]
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

        [HttpGet]
        [Route("api/payments")]
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

        [HttpGet]
        [Route("api/payments/{id}")]
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

        [HttpGet]
        [Route("api/refunds")]
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

        [HttpPut]
        [Route("api/refunds/{id}/status")]
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
    }
}
