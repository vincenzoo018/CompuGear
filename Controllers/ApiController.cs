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

        // ===================================================================
        // CROSS-CUTTING ENDPOINTS ONLY
        // Domain-specific endpoints have been migrated to:
        //   - MarketingStaffController (campaigns, promotions)
        //   - SalesStaffController (orders, leads)
        //   - InventoryStaffController (products, purchase orders, stock)
        //   - BillingStaffController (invoices, payments, refunds)
        //   - SupportStaffController (tickets, live chat, knowledge base)
        //   - CustomerPortalController (customer-facing APIs)
        //   - SuperAdminController (users, roles, RBAC)
        // ===================================================================

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
