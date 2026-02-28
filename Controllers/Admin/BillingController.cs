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

        public IActionResult ChartOfAccounts()
        {
            return View("~/Views/Admin/Billing/ChartOfAccounts.cshtml");
        }

        public IActionResult JournalEntries()
        {
            return View("~/Views/Admin/Billing/JournalEntries.cshtml");
        }

        public IActionResult GeneralLedger()
        {
            return View("~/Views/Admin/Billing/GeneralLedger.cshtml");
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
                        InvoiceNumber = (string?)i.InvoiceNumber,
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
                        PaymentMethodType = (i.Order != null ? i.Order.PaymentMethod : "Invoice Record") ?? "Invoice Record",
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

        #region ===== CHART OF ACCOUNTS =====

        /// <summary>
        /// Helper: Build account code → accountId lookup from DB
        /// </summary>
        private async Task<Dictionary<string, int>> GetAccountCodeMap(int? companyId)
        {
            return await _context.ChartOfAccounts
                .Where(a => !a.IsArchived && (companyId == null || a.CompanyId == companyId))
                .ToDictionaryAsync(a => a.AccountCode, a => a.AccountId);
        }

        /// <summary>
        /// Helper: Compute live balances for every COA account from GL records + system-derived transactions
        /// </summary>
        private async Task<Dictionary<int, (decimal debit, decimal credit)>> ComputeAccountBalances(int? companyId)
        {
            var balances = new Dictionary<int, (decimal debit, decimal credit)>();

            void AddBal(int acctId, decimal dr, decimal cr)
            {
                if (balances.ContainsKey(acctId))
                    balances[acctId] = (balances[acctId].debit + dr, balances[acctId].credit + cr);
                else
                    balances[acctId] = (dr, cr);
            }

            // 1) Manual GL entries (from posted journal entries)
            var glEntries = await _context.GeneralLedger
                .Where(g => !g.IsArchived && (companyId == null || g.CompanyId == companyId))
                .GroupBy(g => g.AccountId)
                .Select(g => new { AccountId = g.Key, Debit = g.Sum(x => x.DebitAmount), Credit = g.Sum(x => x.CreditAmount) })
                .ToListAsync();
            foreach (var g in glEntries) AddBal(g.AccountId, g.Debit, g.Credit);

            var map = await GetAccountCodeMap(companyId);
            int Acct(string code) => map.ContainsKey(code) ? map[code] : 0;

            // 2) Invoices (Paid/Partial) → Debit 1010 AR, Credit 4000 Revenue, Credit 2020 Tax Payable
            var invoices = await _context.Invoices
                .Where(i => (companyId == null || i.CompanyId == companyId) && i.Status != "Cancelled" && i.Status != "Void" && i.Status != "Draft")
                .Select(i => new { i.Subtotal, i.TaxAmount, i.DiscountAmount, i.ShippingAmount, i.TotalAmount })
                .ToListAsync();
            foreach (var inv in invoices)
            {
                if (Acct("1010") > 0) AddBal(Acct("1010"), inv.TotalAmount, 0); // DR Accounts Receivable
                if (Acct("4000") > 0) AddBal(Acct("4000"), 0, inv.Subtotal - inv.DiscountAmount); // CR Sales Revenue
                if (inv.TaxAmount > 0 && Acct("2020") > 0) AddBal(Acct("2020"), 0, inv.TaxAmount); // CR Sales Tax Payable
                if (inv.ShippingAmount > 0 && Acct("4020") > 0) AddBal(Acct("4020"), 0, inv.ShippingAmount); // CR Other Income (shipping)
            }

            // 3) Payments (Completed) → Debit 1000 Cash, Credit 1010 AR
            var payments = await _context.Payments
                .Where(p => (companyId == null || p.CompanyId == companyId) && p.Status == "Completed")
                .Select(p => new { p.Amount })
                .ToListAsync();
            foreach (var pay in payments)
            {
                if (Acct("1000") > 0) AddBal(Acct("1000"), pay.Amount, 0); // DR Cash
                if (Acct("1010") > 0) AddBal(Acct("1010"), 0, pay.Amount); // CR Accounts Receivable
            }

            // 4) Refunds (Processed) → Debit 4000 Revenue, Credit 1000 Cash
            var refunds = await _context.Refunds
                .Where(r => (companyId == null || r.CompanyId == companyId) && r.Status == "Processed")
                .Select(r => new { r.Amount })
                .ToListAsync();
            foreach (var ref_ in refunds)
            {
                if (Acct("4000") > 0) AddBal(Acct("4000"), ref_.Amount, 0); // DR Sales Revenue (reversal)
                if (Acct("1000") > 0) AddBal(Acct("1000"), 0, ref_.Amount); // CR Cash
            }

            // 5) Orders with COGS (Confirmed/Delivered) → Debit 5000 COGS, Credit 1020 Inventory
            var orderItems = await _context.Orders
                .Where(o => (companyId == null || o.CompanyId == companyId) && (o.OrderStatus == "Confirmed" || o.OrderStatus == "Delivered" || o.OrderStatus == "Shipped"))
                .SelectMany(o => o.OrderItems)
                .Include(oi => oi.Product)
                .Where(oi => oi.Product != null)
                .Select(oi => new { CostTotal = oi.Product!.CostPrice * oi.Quantity })
                .ToListAsync();
            foreach (var oi in orderItems)
            {
                if (oi.CostTotal > 0)
                {
                    if (Acct("5000") > 0) AddBal(Acct("5000"), oi.CostTotal, 0); // DR Cost of Goods Sold
                    if (Acct("1020") > 0) AddBal(Acct("1020"), 0, oi.CostTotal); // CR Inventory
                }
            }

            // 6) Tax Calculations → already covered through invoices above (TaxAmount)
            //    We also pull TaxCalculation records for any additional tax entries
            var taxCalcs = await _context.TaxCalculations
                .Where(t => (companyId == null || t.CompanyId == companyId))
                .GroupBy(t => 1)
                .Select(g => new { TotalTax = g.Sum(t => t.TaxAmount) })
                .FirstOrDefaultAsync();
            // Tax calculations are already included via invoice tax amounts - no double counting

            return balances;
        }

        [HttpGet]
        [Route("api/chart-of-accounts")]
        public async Task<IActionResult> GetChartOfAccounts()
        {
            var companyId = GetCompanyId();
            var accounts = await _context.ChartOfAccounts
                .Where(a => !a.IsArchived && (companyId == null || a.CompanyId == companyId))
                .OrderBy(a => a.AccountCode)
                .Select(a => new
                {
                    a.AccountId,
                    a.AccountCode,
                    a.AccountName,
                    a.AccountType,
                    a.ParentAccountId,
                    a.Description,
                    a.NormalBalance,
                    a.IsActive,
                    a.IsArchived,
                    a.CreatedAt,
                    a.UpdatedAt
                })
                .ToListAsync();

            // Compute live balances from system data
            var balances = await ComputeAccountBalances(companyId);

            var result = accounts.Select(a =>
            {
                var bal = balances.ContainsKey(a.AccountId) ? balances[a.AccountId] : (debit: 0m, credit: 0m);
                var netBalance = bal.debit - bal.credit;
                return new
                {
                    a.AccountId,
                    a.AccountCode,
                    a.AccountName,
                    a.AccountType,
                    a.ParentAccountId,
                    a.Description,
                    a.NormalBalance,
                    a.IsActive,
                    a.IsArchived,
                    a.CreatedAt,
                    a.UpdatedAt,
                    TotalDebit = bal.debit,
                    TotalCredit = bal.credit,
                    Balance = netBalance,
                    // Formatted balance uses normal balance side
                    DisplayBalance = a.NormalBalance == "Credit" ? bal.credit - bal.debit : netBalance
                };
            }).ToList();

            return Ok(new { success = true, data = result });
        }

        [HttpGet]
        [Route("api/chart-of-accounts/{id}")]
        public async Task<IActionResult> GetChartOfAccountById(int id)
        {
            var companyId = GetCompanyId();
            var account = await _context.ChartOfAccounts
                .Where(a => a.AccountId == id && (companyId == null || a.CompanyId == companyId))
                .FirstOrDefaultAsync();

            if (account == null) return NotFound(new { success = false, message = "Account not found" });

            return Ok(new { success = true, data = account });
        }

        [HttpPost]
        [Route("api/chart-of-accounts")]
        public async Task<IActionResult> CreateChartOfAccount([FromBody] ChartOfAccount account)
        {
            try
            {
                var companyId = GetCompanyId();
                account.CompanyId = companyId ?? 1;
                account.CreatedAt = DateTime.UtcNow;
                account.UpdatedAt = DateTime.UtcNow;
                account.CreatedBy = HttpContext.Session.GetInt32("UserId");

                _context.ChartOfAccounts.Add(account);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Account created successfully", data = account });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut]
        [Route("api/chart-of-accounts/{id}")]
        public async Task<IActionResult> UpdateChartOfAccount(int id, [FromBody] ChartOfAccount account)
        {
            try
            {
                var companyId = GetCompanyId();
                var existing = await _context.ChartOfAccounts
                    .FirstOrDefaultAsync(a => a.AccountId == id && (companyId == null || a.CompanyId == companyId));

                if (existing == null) return NotFound(new { success = false, message = "Account not found" });

                existing.AccountCode = account.AccountCode;
                existing.AccountName = account.AccountName;
                existing.AccountType = account.AccountType;
                existing.ParentAccountId = account.ParentAccountId;
                existing.Description = account.Description;
                existing.NormalBalance = account.NormalBalance;
                existing.IsActive = account.IsActive;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedBy = HttpContext.Session.GetInt32("UserId");

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Account updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut]
        [Route("api/chart-of-accounts/{id}/archive")]
        public async Task<IActionResult> ArchiveChartOfAccount(int id)
        {
            var companyId = GetCompanyId();
            var account = await _context.ChartOfAccounts
                .FirstOrDefaultAsync(a => a.AccountId == id && (companyId == null || a.CompanyId == companyId));

            if (account == null) return NotFound(new { success = false, message = "Account not found" });

            account.IsArchived = !account.IsArchived;
            account.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = account.IsArchived ? "Account archived" : "Account restored" });
        }

        #endregion

        #region ===== JOURNAL ENTRIES =====

        [HttpGet]
        [Route("api/journal-entries")]
        public async Task<IActionResult> GetJournalEntries()
        {
            var companyId = GetCompanyId();

            // 1) Manual journal entries
            var manualEntries = await _context.JournalEntries
                .Include(j => j.Lines).ThenInclude(l => l.Account)
                .Where(j => !j.IsArchived && (companyId == null || j.CompanyId == companyId))
                .OrderByDescending(j => j.EntryDate)
                .Select(j => new
                {
                    j.EntryId,
                    j.EntryNumber,
                    j.EntryDate,
                    j.Description,
                    j.Reference,
                    j.Status,
                    j.TotalDebit,
                    j.TotalCredit,
                    j.IsArchived,
                    j.Notes,
                    j.PostedAt,
                    j.CreatedAt,
                    Source = "Manual",
                    SourceType = "Journal Entry",
                    Lines = j.Lines.Select(l => new
                    {
                        l.LineId,
                        l.AccountId,
                        AccountCode = l.Account.AccountCode,
                        AccountName = l.Account.AccountName,
                        l.Description,
                        l.DebitAmount,
                        l.CreditAmount
                    })
                })
                .ToListAsync();

            // 2) System-derived entries from Invoices (non-cancelled, non-draft)
            var map = await GetAccountCodeMap(companyId);
            int Acct(string code) => map.ContainsKey(code) ? map[code] : 0;

            var invoiceEntries = await _context.Invoices
                .Include(i => i.Customer)
                .Where(i => (companyId == null || i.CompanyId == companyId) && i.Status != "Cancelled" && i.Status != "Void" && i.Status != "Draft")
                .OrderByDescending(i => i.InvoiceDate)
                .Select(i => new
                {
                    i.InvoiceId,
                    i.InvoiceNumber,
                    i.InvoiceDate,
                    CustomerName = i.Customer != null ? i.Customer.FirstName + " " + i.Customer.LastName : "N/A",
                    i.Subtotal,
                    i.TaxAmount,
                    i.DiscountAmount,
                    i.ShippingAmount,
                    i.TotalAmount,
                    i.Status,
                    i.CreatedAt
                })
                .ToListAsync();

            var invoiceDerived = invoiceEntries.Select(inv => new
            {
                EntryId = -inv.InvoiceId,
                EntryNumber = $"SYS-INV-{inv.InvoiceNumber}",
                EntryDate = inv.InvoiceDate,
                Description = $"Invoice {inv.InvoiceNumber} - {inv.CustomerName}",
                Reference = inv.InvoiceNumber,
                Status = "Posted",
                TotalDebit = inv.TotalAmount,
                TotalCredit = inv.TotalAmount,
                IsArchived = false,
                Notes = $"Auto-generated from Invoice. Status: {inv.Status}",
                PostedAt = (DateTime?)inv.InvoiceDate,
                CreatedAt = inv.CreatedAt,
                Source = "System",
                SourceType = "Invoice",
                Lines = new[]
                {
                    new { LineId = 0, AccountId = Acct("1010"), AccountCode = "1010", AccountName = "Accounts Receivable", Description = $"Invoice {inv.InvoiceNumber}", DebitAmount = inv.TotalAmount, CreditAmount = 0m },
                    new { LineId = 0, AccountId = Acct("4000"), AccountCode = "4000", AccountName = "Sales Revenue", Description = $"Revenue from {inv.InvoiceNumber}", DebitAmount = 0m, CreditAmount = inv.Subtotal - inv.DiscountAmount + inv.ShippingAmount },
                    new { LineId = 0, AccountId = Acct("2020"), AccountCode = "2020", AccountName = "Sales Tax Payable", Description = $"Tax on {inv.InvoiceNumber}", DebitAmount = 0m, CreditAmount = inv.TaxAmount }
                }.Where(l => l.AccountId > 0 && (l.DebitAmount > 0 || l.CreditAmount > 0)).ToList()
            }).ToList();

            // 3) System-derived from Payments (Completed)
            var paymentEntries = await _context.Payments
                .Include(p => p.Customer)
                .Where(p => (companyId == null || p.CompanyId == companyId) && p.Status == "Completed")
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => new
                {
                    p.PaymentId,
                    p.PaymentNumber,
                    p.PaymentDate,
                    p.Amount,
                    p.PaymentMethodType,
                    CustomerName = p.Customer != null ? p.Customer.FirstName + " " + p.Customer.LastName : "N/A",
                    p.ReferenceNumber,
                    p.CreatedAt
                })
                .ToListAsync();

            var paymentDerived = paymentEntries.Select(pay => new
            {
                EntryId = -(100000 + pay.PaymentId),
                EntryNumber = $"SYS-PAY-{pay.PaymentNumber}",
                EntryDate = pay.PaymentDate,
                Description = $"Payment {pay.PaymentNumber} from {pay.CustomerName} ({pay.PaymentMethodType})",
                Reference = pay.PaymentNumber,
                Status = "Posted",
                TotalDebit = pay.Amount,
                TotalCredit = pay.Amount,
                IsArchived = false,
                Notes = $"Auto-generated from Payment. Method: {pay.PaymentMethodType}",
                PostedAt = (DateTime?)pay.PaymentDate,
                CreatedAt = pay.CreatedAt,
                Source = "System",
                SourceType = "Payment",
                Lines = new[]
                {
                    new { LineId = 0, AccountId = Acct("1000"), AccountCode = "1000", AccountName = "Cash", Description = $"Payment {pay.PaymentNumber}", DebitAmount = pay.Amount, CreditAmount = 0m },
                    new { LineId = 0, AccountId = Acct("1010"), AccountCode = "1010", AccountName = "Accounts Receivable", Description = $"Payment received {pay.PaymentNumber}", DebitAmount = 0m, CreditAmount = pay.Amount }
                }.Where(l => l.AccountId > 0).ToList()
            }).ToList();

            // 4) System-derived from Refunds (Processed)
            var refundEntries = await _context.Refunds
                .Include(r => r.Customer)
                .Where(r => (companyId == null || r.CompanyId == companyId) && r.Status == "Processed")
                .OrderByDescending(r => r.ProcessedAt)
                .Select(r => new
                {
                    r.RefundId,
                    r.RefundNumber,
                    r.Amount,
                    r.Reason,
                    r.ProcessedAt,
                    CustomerName = r.Customer != null ? r.Customer.FirstName + " " + r.Customer.LastName : "N/A",
                    r.RequestedAt
                })
                .ToListAsync();

            var refundDerived = refundEntries.Select(ref_ => new
            {
                EntryId = -(200000 + ref_.RefundId),
                EntryNumber = $"SYS-REF-{ref_.RefundNumber}",
                EntryDate = ref_.ProcessedAt ?? ref_.RequestedAt,
                Description = $"Refund {ref_.RefundNumber} to {ref_.CustomerName}",
                Reference = ref_.RefundNumber,
                Status = "Posted",
                TotalDebit = ref_.Amount,
                TotalCredit = ref_.Amount,
                IsArchived = false,
                Notes = $"Auto-generated from Refund. Reason: {ref_.Reason}",
                PostedAt = ref_.ProcessedAt,
                CreatedAt = ref_.RequestedAt,
                Source = "System",
                SourceType = "Refund",
                Lines = new[]
                {
                    new { LineId = 0, AccountId = Acct("4000"), AccountCode = "4000", AccountName = "Sales Revenue", Description = $"Refund {ref_.RefundNumber}", DebitAmount = ref_.Amount, CreditAmount = 0m },
                    new { LineId = 0, AccountId = Acct("1000"), AccountCode = "1000", AccountName = "Cash", Description = $"Refund payment {ref_.RefundNumber}", DebitAmount = 0m, CreditAmount = ref_.Amount }
                }.Where(l => l.AccountId > 0).ToList()
            }).ToList();

            // 5) System-derived from Orders COGS (Confirmed/Delivered/Shipped)
            var ordersWithCogs = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .Where(o => (companyId == null || o.CompanyId == companyId)
                    && (o.OrderStatus == "Confirmed" || o.OrderStatus == "Delivered" || o.OrderStatus == "Shipped"))
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var cogsDerived = ordersWithCogs
                .Where(o => o.OrderItems.Any(oi => oi.Product != null && oi.Product.CostPrice > 0))
                .Select(o =>
                {
                    var totalCost = o.OrderItems.Where(oi => oi.Product != null).Sum(oi => oi.Product!.CostPrice * oi.Quantity);
                    return new
                    {
                        EntryId = -(300000 + o.OrderId),
                        EntryNumber = $"SYS-COGS-{o.OrderNumber}",
                        EntryDate = o.ConfirmedAt ?? o.OrderDate,
                        Description = $"COGS for Order {o.OrderNumber} - {(o.Customer != null ? o.Customer.FirstName + " " + o.Customer.LastName : "N/A")}",
                        Reference = o.OrderNumber,
                        Status = "Posted",
                        TotalDebit = totalCost,
                        TotalCredit = totalCost,
                        IsArchived = false,
                        Notes = $"Auto-generated COGS from Order. Items: {o.OrderItems.Count}",
                        PostedAt = (DateTime?)(o.ConfirmedAt ?? o.OrderDate),
                        CreatedAt = o.CreatedAt,
                        Source = "System",
                        SourceType = "COGS",
                        Lines = new[]
                        {
                            new { LineId = 0, AccountId = Acct("5000"), AccountCode = "5000", AccountName = "Cost of Goods Sold", Description = $"COGS Order {o.OrderNumber}", DebitAmount = totalCost, CreditAmount = 0m },
                            new { LineId = 0, AccountId = Acct("1020"), AccountCode = "1020", AccountName = "Inventory", Description = $"Inventory reduction {o.OrderNumber}", DebitAmount = 0m, CreditAmount = totalCost }
                        }.Where(l => l.AccountId > 0).ToList()
                    };
                })
                .Where(e => e.TotalDebit > 0)
                .ToList();

            // 6) System-derived from Tax Calculations
            var taxCalcs = await _context.TaxCalculations
                .Include(t => t.Invoice)
                .Include(t => t.TaxRate)
                .Where(t => (companyId == null || t.CompanyId == companyId) && t.TaxAmount > 0)
                .OrderByDescending(t => t.CalculatedAt)
                .ToListAsync();

            var taxDerived = taxCalcs
                .GroupBy(t => t.InvoiceId ?? 0)
                .Where(g => g.Key > 0)
                .Select(g =>
                {
                    var first = g.First();
                    var totalTax = g.Sum(t => t.TaxAmount);
                    var totalTaxable = g.Sum(t => t.TaxableAmount);
                    var invNum = first.Invoice?.InvoiceNumber ?? $"INV-{first.InvoiceId}";
                    return new
                    {
                        EntryId = -(400000 + (first.InvoiceId ?? first.CalculationId)),
                        EntryNumber = $"SYS-TAX-{invNum}",
                        EntryDate = first.CalculatedAt,
                        Description = $"Tax on {invNum} ({first.TaxRate?.TaxName ?? "Tax"} @ {first.AppliedRate}%)",
                        Reference = invNum,
                        Status = "Posted",
                        TotalDebit = totalTax,
                        TotalCredit = totalTax,
                        IsArchived = false,
                        Notes = $"Auto-generated tax entry. Taxable: ₱{totalTaxable:N2}, Rate: {first.AppliedRate}%",
                        PostedAt = (DateTime?)first.CalculatedAt,
                        CreatedAt = first.CalculatedAt,
                        Source = "System",
                        SourceType = "Tax",
                        Lines = new[]
                        {
                            new { LineId = 0, AccountId = Acct("1010"), AccountCode = "1010", AccountName = "Accounts Receivable", Description = $"Tax collected {invNum}", DebitAmount = totalTax, CreditAmount = 0m },
                            new { LineId = 0, AccountId = Acct("2020"), AccountCode = "2020", AccountName = "Sales Tax Payable", Description = $"Tax payable {invNum}", DebitAmount = 0m, CreditAmount = totalTax }
                        }.Where(l => l.AccountId > 0).ToList()
                    };
                })
                .Where(e => e.TotalDebit > 0)
                .ToList();

            // Merge all entries
            var allEntries = manualEntries.Cast<object>()
                .Concat(invoiceDerived.Cast<object>())
                .Concat(paymentDerived.Cast<object>())
                .Concat(refundDerived.Cast<object>())
                .Concat(cogsDerived.Cast<object>())
                .Concat(taxDerived.Cast<object>())
                .ToList();

            return Ok(new { success = true, data = allEntries });
        }

        [HttpGet]
        [Route("api/journal-entries/{id}")]
        public async Task<IActionResult> GetJournalEntry(int id)
        {
            // System-derived entries have negative IDs - reconstruct them
            if (id < 0)
            {
                var companyId = GetCompanyId();
                var map = await GetAccountCodeMap(companyId);
                int Acct(string code) => map.ContainsKey(code) ? map[code] : 0;

                var absId = -id;

                // Invoice-derived (id = -InvoiceId)
                if (absId < 100000)
                {
                    var inv = await _context.Invoices.Include(i => i.Customer)
                        .FirstOrDefaultAsync(i => i.InvoiceId == absId && (companyId == null || i.CompanyId == companyId));
                    if (inv == null) return NotFound(new { success = false, message = "Entry not found" });

                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            EntryId = id,
                            EntryNumber = $"SYS-INV-{inv.InvoiceNumber}",
                            EntryDate = inv.InvoiceDate,
                            Description = $"Invoice {inv.InvoiceNumber} - {(inv.Customer != null ? inv.Customer.FirstName + " " + inv.Customer.LastName : "N/A")}",
                            Reference = inv.InvoiceNumber,
                            Status = "Posted",
                            TotalDebit = inv.TotalAmount,
                            TotalCredit = inv.TotalAmount,
                            Notes = $"System-generated from Invoice. Subtotal: ₱{inv.Subtotal:N2}, Tax: ₱{inv.TaxAmount:N2}, Discount: ₱{inv.DiscountAmount:N2}",
                            PostedAt = (DateTime?)inv.InvoiceDate,
                            CreatedAt = inv.CreatedAt,
                            Source = "System",
                            SourceType = "Invoice",
                            Lines = new[]
                            {
                                new { LineId = 0, AccountId = Acct("1010"), AccountCode = "1010", AccountName = "Accounts Receivable", Description = $"Invoice {inv.InvoiceNumber}", DebitAmount = inv.TotalAmount, CreditAmount = 0m },
                                new { LineId = 0, AccountId = Acct("4000"), AccountCode = "4000", AccountName = "Sales Revenue", Description = $"Revenue from {inv.InvoiceNumber}", DebitAmount = 0m, CreditAmount = inv.Subtotal - inv.DiscountAmount + inv.ShippingAmount },
                                new { LineId = 0, AccountId = Acct("2020"), AccountCode = "2020", AccountName = "Sales Tax Payable", Description = $"Tax on {inv.InvoiceNumber}", DebitAmount = 0m, CreditAmount = inv.TaxAmount }
                            }.Where(l => l.AccountId > 0 && (l.DebitAmount > 0 || l.CreditAmount > 0)).ToArray()
                        }
                    });
                }

                // Payment-derived (absId = 100000 + PaymentId)
                if (absId >= 100000 && absId < 200000)
                {
                    var payId = absId - 100000;
                    var pay = await _context.Payments.Include(p => p.Customer)
                        .FirstOrDefaultAsync(p => p.PaymentId == payId && (companyId == null || p.CompanyId == companyId));
                    if (pay == null) return NotFound(new { success = false, message = "Entry not found" });

                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            EntryId = id,
                            EntryNumber = $"SYS-PAY-{pay.PaymentNumber}",
                            EntryDate = pay.PaymentDate,
                            Description = $"Payment {pay.PaymentNumber} from {(pay.Customer != null ? pay.Customer.FirstName + " " + pay.Customer.LastName : "N/A")}",
                            Reference = pay.PaymentNumber,
                            Status = "Posted",
                            TotalDebit = pay.Amount,
                            TotalCredit = pay.Amount,
                            Notes = $"System-generated from Payment. Method: {pay.PaymentMethodType}, Ref: {pay.ReferenceNumber}",
                            PostedAt = (DateTime?)pay.PaymentDate,
                            CreatedAt = pay.CreatedAt,
                            Source = "System",
                            SourceType = "Payment",
                            Lines = new[]
                            {
                                new { LineId = 0, AccountId = Acct("1000"), AccountCode = "1000", AccountName = "Cash", Description = $"Payment {pay.PaymentNumber}", DebitAmount = pay.Amount, CreditAmount = 0m },
                                new { LineId = 0, AccountId = Acct("1010"), AccountCode = "1010", AccountName = "Accounts Receivable", Description = $"Payment received", DebitAmount = 0m, CreditAmount = pay.Amount }
                            }.Where(l => l.AccountId > 0).ToArray()
                        }
                    });
                }

                // Refund-derived (absId = 200000 + RefundId)
                if (absId >= 200000 && absId < 300000)
                {
                    var refId = absId - 200000;
                    var ref_ = await _context.Refunds.Include(r => r.Customer)
                        .FirstOrDefaultAsync(r => r.RefundId == refId && (companyId == null || r.CompanyId == companyId));
                    if (ref_ == null) return NotFound(new { success = false, message = "Entry not found" });

                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            EntryId = id,
                            EntryNumber = $"SYS-REF-{ref_.RefundNumber}",
                            EntryDate = ref_.ProcessedAt ?? ref_.RequestedAt,
                            Description = $"Refund {ref_.RefundNumber} to {(ref_.Customer != null ? ref_.Customer.FirstName + " " + ref_.Customer.LastName : "N/A")}",
                            Reference = ref_.RefundNumber,
                            Status = "Posted",
                            TotalDebit = ref_.Amount,
                            TotalCredit = ref_.Amount,
                            Notes = $"System-generated from Refund. Reason: {ref_.Reason}",
                            PostedAt = ref_.ProcessedAt,
                            CreatedAt = ref_.RequestedAt,
                            Source = "System",
                            SourceType = "Refund",
                            Lines = new[]
                            {
                                new { LineId = 0, AccountId = Acct("4000"), AccountCode = "4000", AccountName = "Sales Revenue", Description = $"Refund reversal", DebitAmount = ref_.Amount, CreditAmount = 0m },
                                new { LineId = 0, AccountId = Acct("1000"), AccountCode = "1000", AccountName = "Cash", Description = $"Refund payment", DebitAmount = 0m, CreditAmount = ref_.Amount }
                            }.Where(l => l.AccountId > 0).ToArray()
                        }
                    });
                }

                // COGS-derived (absId = 300000 + OrderId)
                if (absId >= 300000 && absId < 400000)
                {
                    var ordId = absId - 300000;
                    var ord = await _context.Orders
                        .Include(o => o.Customer).Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                        .FirstOrDefaultAsync(o => o.OrderId == ordId && (companyId == null || o.CompanyId == companyId));
                    if (ord == null) return NotFound(new { success = false, message = "Entry not found" });

                    var totalCost = ord.OrderItems.Where(oi => oi.Product != null).Sum(oi => oi.Product!.CostPrice * oi.Quantity);
                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            EntryId = id,
                            EntryNumber = $"SYS-COGS-{ord.OrderNumber}",
                            EntryDate = ord.ConfirmedAt ?? ord.OrderDate,
                            Description = $"COGS for Order {ord.OrderNumber}",
                            Reference = ord.OrderNumber,
                            Status = "Posted",
                            TotalDebit = totalCost,
                            TotalCredit = totalCost,
                            Notes = $"System-generated COGS. Items: {ord.OrderItems.Count}, Total cost: ₱{totalCost:N2}",
                            PostedAt = (DateTime?)(ord.ConfirmedAt ?? ord.OrderDate),
                            CreatedAt = ord.CreatedAt,
                            Source = "System",
                            SourceType = "COGS",
                            Lines = new[]
                            {
                                new { LineId = 0, AccountId = Acct("5000"), AccountCode = "5000", AccountName = "Cost of Goods Sold", Description = $"COGS Order {ord.OrderNumber}", DebitAmount = totalCost, CreditAmount = 0m },
                                new { LineId = 0, AccountId = Acct("1020"), AccountCode = "1020", AccountName = "Inventory", Description = $"Inventory reduction", DebitAmount = 0m, CreditAmount = totalCost }
                            }.Where(l => l.AccountId > 0).ToArray()
                        }
                    });
                }

                return NotFound(new { success = false, message = "Entry not found" });
            }

            // Standard manual entry lookup
            {
                var companyId = GetCompanyId();
                var entry = await _context.JournalEntries
                    .Include(j => j.Lines).ThenInclude(l => l.Account)
                    .Where(j => j.EntryId == id && (companyId == null || j.CompanyId == companyId))
                    .Select(j => new
                    {
                        j.EntryId,
                        j.EntryNumber,
                        j.EntryDate,
                        j.Description,
                        j.Reference,
                        j.Status,
                        j.TotalDebit,
                        j.TotalCredit,
                        j.Notes,
                        j.PostedAt,
                        j.CreatedAt,
                        Source = "Manual",
                        SourceType = "Journal Entry",
                        Lines = j.Lines.Select(l => new
                        {
                            l.LineId,
                            l.AccountId,
                            AccountCode = l.Account.AccountCode,
                            AccountName = l.Account.AccountName,
                            l.Description,
                            l.DebitAmount,
                            l.CreditAmount
                        })
                    })
                    .FirstOrDefaultAsync();

                if (entry == null) return NotFound(new { success = false, message = "Journal entry not found" });

                return Ok(new { success = true, data = entry });
            }
        }

        [HttpPost]
        [Route("api/journal-entries")]
        public async Task<IActionResult> CreateJournalEntry([FromBody] JournalEntry entry)
        {
            try
            {
                var companyId = GetCompanyId();
                entry.CompanyId = companyId ?? 1;
                entry.EntryNumber = $"JE-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
                entry.CreatedAt = DateTime.UtcNow;
                entry.UpdatedAt = DateTime.UtcNow;
                entry.CreatedBy = HttpContext.Session.GetInt32("UserId");

                // Calculate totals from lines
                entry.TotalDebit = entry.Lines.Sum(l => l.DebitAmount);
                entry.TotalCredit = entry.Lines.Sum(l => l.CreditAmount);

                _context.JournalEntries.Add(entry);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Journal entry created", data = new { entry.EntryId, entry.EntryNumber } });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut]
        [Route("api/journal-entries/{id}")]
        public async Task<IActionResult> UpdateJournalEntry(int id, [FromBody] JournalEntry entry)
        {
            try
            {
                var companyId = GetCompanyId();
                var existing = await _context.JournalEntries
                    .Include(j => j.Lines)
                    .FirstOrDefaultAsync(j => j.EntryId == id && (companyId == null || j.CompanyId == companyId));

                if (existing == null) return NotFound(new { success = false, message = "Entry not found" });
                if (existing.Status == "Posted") return BadRequest(new { success = false, message = "Cannot edit a posted entry" });

                existing.EntryDate = entry.EntryDate;
                existing.Description = entry.Description;
                existing.Reference = entry.Reference;
                existing.Notes = entry.Notes;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedBy = HttpContext.Session.GetInt32("UserId");

                // Replace lines
                _context.JournalEntryLines.RemoveRange(existing.Lines);
                foreach (var line in entry.Lines)
                {
                    line.EntryId = id;
                    _context.JournalEntryLines.Add(line);
                }

                existing.TotalDebit = entry.Lines.Sum(l => l.DebitAmount);
                existing.TotalCredit = entry.Lines.Sum(l => l.CreditAmount);

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Journal entry updated" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut]
        [Route("api/journal-entries/{id}/post")]
        public async Task<IActionResult> PostJournalEntry(int id)
        {
            try
            {
                var companyId = GetCompanyId();
                var entry = await _context.JournalEntries
                    .Include(j => j.Lines)
                    .FirstOrDefaultAsync(j => j.EntryId == id && (companyId == null || j.CompanyId == companyId));

                if (entry == null) return NotFound(new { success = false, message = "Entry not found" });
                if (entry.Status == "Posted") return BadRequest(new { success = false, message = "Already posted" });
                if (entry.TotalDebit != entry.TotalCredit) return BadRequest(new { success = false, message = "Debits must equal credits" });
                if (!entry.Lines.Any()) return BadRequest(new { success = false, message = "Entry must have at least one line" });

                entry.Status = "Posted";
                entry.PostedAt = DateTime.UtcNow;
                entry.PostedBy = HttpContext.Session.GetInt32("UserId");
                entry.UpdatedAt = DateTime.UtcNow;

                // Post to General Ledger
                foreach (var line in entry.Lines)
                {
                    _context.GeneralLedger.Add(new GeneralLedgerEntry
                    {
                        CompanyId = entry.CompanyId,
                        AccountId = line.AccountId,
                        EntryId = entry.EntryId,
                        TransactionDate = entry.EntryDate,
                        Description = line.Description ?? entry.Description,
                        DebitAmount = line.DebitAmount,
                        CreditAmount = line.CreditAmount,
                        Reference = entry.Reference,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Journal entry posted to General Ledger" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut]
        [Route("api/journal-entries/{id}/void")]
        public async Task<IActionResult> VoidJournalEntry(int id)
        {
            var companyId = GetCompanyId();
            var entry = await _context.JournalEntries
                .FirstOrDefaultAsync(j => j.EntryId == id && (companyId == null || j.CompanyId == companyId));

            if (entry == null) return NotFound(new { success = false, message = "Entry not found" });

            entry.Status = "Void";
            entry.UpdatedAt = DateTime.UtcNow;

            // Remove from general ledger
            var ledgerEntries = await _context.GeneralLedger.Where(g => g.EntryId == id).ToListAsync();
            _context.GeneralLedger.RemoveRange(ledgerEntries);

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Journal entry voided" });
        }

        [HttpPut]
        [Route("api/journal-entries/{id}/archive")]
        public async Task<IActionResult> ArchiveJournalEntry(int id)
        {
            var companyId = GetCompanyId();
            var entry = await _context.JournalEntries
                .FirstOrDefaultAsync(j => j.EntryId == id && (companyId == null || j.CompanyId == companyId));

            if (entry == null) return NotFound(new { success = false, message = "Entry not found" });

            entry.IsArchived = !entry.IsArchived;
            entry.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = entry.IsArchived ? "Entry archived" : "Entry restored" });
        }

        #endregion

        #region ===== GENERAL LEDGER =====

        [HttpGet]
        [Route("api/general-ledger")]
        public async Task<IActionResult> GetGeneralLedger(int? accountId = null, string? dateFrom = null, string? dateTo = null, string? source = null)
        {
            var companyId = GetCompanyId();

            DateTime? parsedFrom = null, parsedTo = null;
            if (!string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out var dfrom)) parsedFrom = dfrom;
            if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out var dto)) parsedTo = dto.AddDays(1);

            // 1) Manual GL entries (from posted journal entries)
            var glQuery = _context.GeneralLedger
                .Include(g => g.Account)
                .Include(g => g.JournalEntry)
                .Where(g => !g.IsArchived && (companyId == null || g.CompanyId == companyId));

            if (accountId.HasValue) glQuery = glQuery.Where(g => g.AccountId == accountId.Value);
            if (parsedFrom.HasValue) glQuery = glQuery.Where(g => g.TransactionDate >= parsedFrom.Value);
            if (parsedTo.HasValue) glQuery = glQuery.Where(g => g.TransactionDate <= parsedTo.Value);

            var manualEntries = await glQuery
                .OrderByDescending(g => g.TransactionDate)
                .ThenBy(g => g.LedgerId)
                .Select(g => new
                {
                    g.LedgerId,
                    g.AccountId,
                    AccountCode = g.Account.AccountCode,
                    AccountName = g.Account.AccountName,
                    AccountType = g.Account.AccountType,
                    g.EntryId,
                    EntryNumber = g.JournalEntry != null ? g.JournalEntry.EntryNumber : null,
                    g.TransactionDate,
                    g.Description,
                    g.DebitAmount,
                    g.CreditAmount,
                    g.RunningBalance,
                    g.Reference,
                    g.IsArchived,
                    g.CreatedAt,
                    Source = "Manual",
                    SourceType = "Journal Entry"
                })
                .ToListAsync();

            // ---- System-derived ledger transactions ----
            var map = await GetAccountCodeMap(companyId);
            int Acct(string code) => map.ContainsKey(code) ? map[code] : 0;

            // Helper for account filter
            bool MatchesAccountFilter(int acctId) => !accountId.HasValue || acctId == accountId.Value;
            bool MatchesDateFilter(DateTime date) =>
                (!parsedFrom.HasValue || date >= parsedFrom.Value) &&
                (!parsedTo.HasValue || date <= parsedTo.Value);

            var systemEntries = new List<object>();

            // 2) Invoice transactions → DR Accounts Receivable, CR Sales Revenue, CR Tax Payable
            var invoices = await _context.Invoices
                .Include(i => i.Customer)
                .Where(i => (companyId == null || i.CompanyId == companyId)
                    && i.Status != "Cancelled" && i.Status != "Void" && i.Status != "Draft")
                .ToListAsync();

            foreach (var inv in invoices)
            {
                if (!MatchesDateFilter(inv.InvoiceDate)) continue;
                var custName = inv.Customer != null ? inv.Customer.FirstName + " " + inv.Customer.LastName : "N/A";

                var arId = Acct("1010"); var revId = Acct("4000"); var taxId = Acct("2020"); var othId = Acct("4020");

                if (arId > 0 && MatchesAccountFilter(arId))
                    systemEntries.Add(new { LedgerId = -(inv.InvoiceId * 10 + 1), AccountId = arId, AccountCode = "1010", AccountName = "Accounts Receivable", AccountType = "Asset", EntryId = (int?)null, EntryNumber = $"SYS-INV-{inv.InvoiceNumber}", TransactionDate = inv.InvoiceDate, Description = $"Invoice {inv.InvoiceNumber} - {custName}", DebitAmount = inv.TotalAmount, CreditAmount = 0m, RunningBalance = 0m, Reference = inv.InvoiceNumber, IsArchived = false, CreatedAt = inv.CreatedAt, Source = "System", SourceType = "Invoice" });

                var netRevenue = inv.Subtotal - inv.DiscountAmount + inv.ShippingAmount;
                if (revId > 0 && netRevenue > 0 && MatchesAccountFilter(revId))
                    systemEntries.Add(new { LedgerId = -(inv.InvoiceId * 10 + 2), AccountId = revId, AccountCode = "4000", AccountName = "Sales Revenue", AccountType = "Revenue", EntryId = (int?)null, EntryNumber = $"SYS-INV-{inv.InvoiceNumber}", TransactionDate = inv.InvoiceDate, Description = $"Revenue from {inv.InvoiceNumber} - {custName}", DebitAmount = 0m, CreditAmount = netRevenue, RunningBalance = 0m, Reference = inv.InvoiceNumber, IsArchived = false, CreatedAt = inv.CreatedAt, Source = "System", SourceType = "Sales Revenue" });

                if (taxId > 0 && inv.TaxAmount > 0 && MatchesAccountFilter(taxId))
                    systemEntries.Add(new { LedgerId = -(inv.InvoiceId * 10 + 3), AccountId = taxId, AccountCode = "2020", AccountName = "Sales Tax Payable", AccountType = "Liability", EntryId = (int?)null, EntryNumber = $"SYS-INV-{inv.InvoiceNumber}", TransactionDate = inv.InvoiceDate, Description = $"Tax on {inv.InvoiceNumber}", DebitAmount = 0m, CreditAmount = inv.TaxAmount, RunningBalance = 0m, Reference = inv.InvoiceNumber, IsArchived = false, CreatedAt = inv.CreatedAt, Source = "System", SourceType = "Tax" });
            }

            // 3) Payments → DR Cash, CR Accounts Receivable
            var payments = await _context.Payments
                .Include(p => p.Customer)
                .Where(p => (companyId == null || p.CompanyId == companyId) && p.Status == "Completed")
                .ToListAsync();

            foreach (var pay in payments)
            {
                if (!MatchesDateFilter(pay.PaymentDate)) continue;
                var custName = pay.Customer != null ? pay.Customer.FirstName + " " + pay.Customer.LastName : "N/A";
                var cashId = Acct("1000"); var arId = Acct("1010");

                if (cashId > 0 && MatchesAccountFilter(cashId))
                    systemEntries.Add(new { LedgerId = -(500000 + pay.PaymentId * 10 + 1), AccountId = cashId, AccountCode = "1000", AccountName = "Cash", AccountType = "Asset", EntryId = (int?)null, EntryNumber = $"SYS-PAY-{pay.PaymentNumber}", TransactionDate = pay.PaymentDate, Description = $"Payment {pay.PaymentNumber} from {custName} ({pay.PaymentMethodType})", DebitAmount = pay.Amount, CreditAmount = 0m, RunningBalance = 0m, Reference = pay.PaymentNumber, IsArchived = false, CreatedAt = pay.CreatedAt, Source = "System", SourceType = "Payment" });

                if (arId > 0 && MatchesAccountFilter(arId))
                    systemEntries.Add(new { LedgerId = -(500000 + pay.PaymentId * 10 + 2), AccountId = arId, AccountCode = "1010", AccountName = "Accounts Receivable", AccountType = "Asset", EntryId = (int?)null, EntryNumber = $"SYS-PAY-{pay.PaymentNumber}", TransactionDate = pay.PaymentDate, Description = $"Received from {custName}", DebitAmount = 0m, CreditAmount = pay.Amount, RunningBalance = 0m, Reference = pay.PaymentNumber, IsArchived = false, CreatedAt = pay.CreatedAt, Source = "System", SourceType = "Payment" });
            }

            // 4) Refunds → DR Sales Revenue, CR Cash
            var refunds = await _context.Refunds
                .Include(r => r.Customer)
                .Where(r => (companyId == null || r.CompanyId == companyId) && r.Status == "Processed")
                .ToListAsync();

            foreach (var ref_ in refunds)
            {
                var refDate = ref_.ProcessedAt ?? ref_.RequestedAt;
                if (!MatchesDateFilter(refDate)) continue;
                var custName = ref_.Customer != null ? ref_.Customer.FirstName + " " + ref_.Customer.LastName : "N/A";
                var revId = Acct("4000"); var cashId = Acct("1000");

                if (revId > 0 && MatchesAccountFilter(revId))
                    systemEntries.Add(new { LedgerId = -(600000 + ref_.RefundId * 10 + 1), AccountId = revId, AccountCode = "4000", AccountName = "Sales Revenue", AccountType = "Revenue", EntryId = (int?)null, EntryNumber = $"SYS-REF-{ref_.RefundNumber}", TransactionDate = refDate, Description = $"Refund {ref_.RefundNumber} to {custName}", DebitAmount = ref_.Amount, CreditAmount = 0m, RunningBalance = 0m, Reference = ref_.RefundNumber, IsArchived = false, CreatedAt = ref_.RequestedAt, Source = "System", SourceType = "Refund" });

                if (cashId > 0 && MatchesAccountFilter(cashId))
                    systemEntries.Add(new { LedgerId = -(600000 + ref_.RefundId * 10 + 2), AccountId = cashId, AccountCode = "1000", AccountName = "Cash", AccountType = "Asset", EntryId = (int?)null, EntryNumber = $"SYS-REF-{ref_.RefundNumber}", TransactionDate = refDate, Description = $"Refund payment {ref_.RefundNumber}", DebitAmount = 0m, CreditAmount = ref_.Amount, RunningBalance = 0m, Reference = ref_.RefundNumber, IsArchived = false, CreatedAt = ref_.RequestedAt, Source = "System", SourceType = "Refund" });
            }

            // 5) COGS from Orders → DR COGS, CR Inventory
            var orders = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .Where(o => (companyId == null || o.CompanyId == companyId)
                    && (o.OrderStatus == "Confirmed" || o.OrderStatus == "Delivered" || o.OrderStatus == "Shipped"))
                .ToListAsync();

            foreach (var ord in orders)
            {
                var ordDate = ord.ConfirmedAt ?? ord.OrderDate;
                if (!MatchesDateFilter(ordDate)) continue;
                var totalCost = ord.OrderItems.Where(oi => oi.Product != null).Sum(oi => oi.Product!.CostPrice * oi.Quantity);
                if (totalCost <= 0) continue;

                var cogsId = Acct("5000"); var invId = Acct("1020");
                var custName = ord.Customer != null ? ord.Customer.FirstName + " " + ord.Customer.LastName : "N/A";

                if (cogsId > 0 && MatchesAccountFilter(cogsId))
                    systemEntries.Add(new { LedgerId = -(700000 + ord.OrderId * 10 + 1), AccountId = cogsId, AccountCode = "5000", AccountName = "Cost of Goods Sold", AccountType = "Expense", EntryId = (int?)null, EntryNumber = $"SYS-COGS-{ord.OrderNumber}", TransactionDate = ordDate, Description = $"COGS for Order {ord.OrderNumber} - {custName}", DebitAmount = totalCost, CreditAmount = 0m, RunningBalance = 0m, Reference = ord.OrderNumber, IsArchived = false, CreatedAt = ord.CreatedAt, Source = "System", SourceType = "COGS" });

                if (invId > 0 && MatchesAccountFilter(invId))
                    systemEntries.Add(new { LedgerId = -(700000 + ord.OrderId * 10 + 2), AccountId = invId, AccountCode = "1020", AccountName = "Inventory", AccountType = "Asset", EntryId = (int?)null, EntryNumber = $"SYS-COGS-{ord.OrderNumber}", TransactionDate = ordDate, Description = $"Inventory reduction {ord.OrderNumber}", DebitAmount = 0m, CreditAmount = totalCost, RunningBalance = 0m, Reference = ord.OrderNumber, IsArchived = false, CreatedAt = ord.CreatedAt, Source = "System", SourceType = "COGS" });
            }

            // Filter by source if requested
            var allEntries = manualEntries.Cast<object>().Concat(systemEntries).ToList();
            // source filter is applied client-side via JS, but if passed we filter here too
            if (!string.IsNullOrEmpty(source) && source != "All")
            {
                // We cannot easily filter anonymous objects by property, so we keep all and let JS do it
            }

            // Calculate account summaries from BOTH manual + system
            var summaryDict = new Dictionary<int, (string code, string name, string type, decimal dr, decimal cr)>();

            void AddToSummary(int acctId, string code, string name, string type, decimal dr, decimal cr)
            {
                if (summaryDict.ContainsKey(acctId))
                {
                    var existing = summaryDict[acctId];
                    summaryDict[acctId] = (existing.code, existing.name, existing.type, existing.dr + dr, existing.cr + cr);
                }
                else
                {
                    summaryDict[acctId] = (code, name, type, dr, cr);
                }
            }

            // Summary from manual GL
            var manualSummary = await _context.GeneralLedger
                .Include(g => g.Account)
                .Where(g => !g.IsArchived && (companyId == null || g.CompanyId == companyId))
                .GroupBy(g => new { g.AccountId, g.Account.AccountCode, g.Account.AccountName, g.Account.AccountType })
                .Select(g => new { g.Key.AccountId, g.Key.AccountCode, g.Key.AccountName, g.Key.AccountType, Debit = g.Sum(x => x.DebitAmount), Credit = g.Sum(x => x.CreditAmount) })
                .ToListAsync();

            foreach (var s in manualSummary) AddToSummary(s.AccountId, s.AccountCode, s.AccountName, s.AccountType, s.Debit, s.Credit);

            // Summary from system-derived: reconstruct from invoices, payments, etc.
            var balances = await ComputeAccountBalances(companyId);
            var accountLookup = await _context.ChartOfAccounts
                .Where(a => !a.IsArchived && (companyId == null || a.CompanyId == companyId))
                .ToDictionaryAsync(a => a.AccountId, a => new { a.AccountCode, a.AccountName, a.AccountType });

            foreach (var kvp in balances)
            {
                if (!accountLookup.ContainsKey(kvp.Key)) continue;
                var acct = accountLookup[kvp.Key];
                // Don't double-count manual GL (already in manualSummary). System balances include manual GL, so just use system totals
                if (summaryDict.ContainsKey(kvp.Key))
                {
                    // Replace with full system balance (which includes manual GL + system transactions)
                    summaryDict[kvp.Key] = (acct.AccountCode, acct.AccountName, acct.AccountType, kvp.Value.debit, kvp.Value.credit);
                }
                else
                {
                    AddToSummary(kvp.Key, acct.AccountCode, acct.AccountName, acct.AccountType, kvp.Value.debit, kvp.Value.credit);
                }
            }

            var accountSummary = summaryDict
                .OrderBy(s => s.Value.code)
                .Select(s => new
                {
                    AccountId = s.Key,
                    AccountCode = s.Value.code,
                    AccountName = s.Value.name,
                    AccountType = s.Value.type,
                    TotalDebit = s.Value.dr,
                    TotalCredit = s.Value.cr,
                    Balance = s.Value.dr - s.Value.cr
                })
                .ToList();

            return Ok(new { success = true, data = allEntries, summary = accountSummary });
        }

        [HttpPut]
        [Route("api/general-ledger/{id}/archive")]
        public async Task<IActionResult> ArchiveGeneralLedgerEntry(int id)
        {
            if (id < 0) return BadRequest(new { success = false, message = "System-generated entries cannot be archived" });

            var companyId = GetCompanyId();
            var entry = await _context.GeneralLedger
                .FirstOrDefaultAsync(g => g.LedgerId == id && (companyId == null || g.CompanyId == companyId));

            if (entry == null) return NotFound(new { success = false, message = "Entry not found" });

            entry.IsArchived = !entry.IsArchived;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = entry.IsArchived ? "Entry archived" : "Entry restored" });
        }

        #endregion
    }
}
