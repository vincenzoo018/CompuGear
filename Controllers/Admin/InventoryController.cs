using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using CompuGear.Data;
using CompuGear.Models;
using CompuGear.Services;

namespace CompuGear.Controllers
{
    /// <summary>
    /// Inventory Controller for Admin - Uses Views/Admin/Inventory folder
    /// RoleId: 1 - Super Admin, 2 - Company Admin
    /// </summary>
    public class InventoryController : Controller
    {
        private readonly CompuGearDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IAuditService _auditService;

        public InventoryController(CompuGearDbContext context, IConfiguration configuration, IAuditService auditService)
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

        #region View Actions

        public IActionResult Products()
        {
            return View("~/Views/Admin/Inventory/Products.cshtml");
        }

        public IActionResult ProductsArchive()
        {
            return View("~/Views/Admin/Inventory/ProductsArchive.cshtml");
        }

        public IActionResult Categories()
        {
            return View("~/Views/Admin/Inventory/Categories.cshtml");
        }

        public IActionResult CategoriesArchive()
        {
            return View("~/Views/Admin/Inventory/CategoriesArchive.cshtml");
        }

        public IActionResult Stock()
        {
            return View("~/Views/Admin/Inventory/Stock.cshtml");
        }

        public IActionResult Alerts()
        {
            return View("~/Views/Admin/Inventory/Alerts.cshtml");
        }

        public IActionResult Reports()
        {
            return View("~/Views/Admin/Inventory/Reports.cshtml");
        }

        public IActionResult Suppliers()
        {
            return View("~/Views/Admin/Inventory/Suppliers.cshtml");
        }

        public IActionResult SuppliersArchive()
        {
            return View("~/Views/Admin/Inventory/SuppliersArchive.cshtml");
        }

        public IActionResult PurchaseOrders()
        {
            return View("~/Views/Admin/Inventory/PurchaseOrders.cshtml");
        }

        public IActionResult StockAdjustment()
        {
            return View("~/Views/Admin/Inventory/StockAdjustment.cshtml");
        }

        #endregion

        #region Products / Inventory API

        [HttpGet]
        [Route("api/products")]
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

        [HttpGet]
        [Route("api/products/{id}")]
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

        [HttpPost]
        [Route("api/products")]
        public async Task<IActionResult> CreateProduct([FromBody] Product product)
        {
            try
            {
                var companyId = GetCompanyId();
                product.CompanyId = companyId;
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

        [HttpPut]
        [Route("api/products/{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] Product product)
        {
            try
            {
                var companyId = GetCompanyId();
                var existing = await _context.Products.FindAsync(id);
                if (existing == null) return NotFound();
                
                // Company isolation check: Company Admin can only edit their own products OR products with null CompanyId
                if (companyId != null && existing.CompanyId != null && existing.CompanyId != companyId)
                    return NotFound();

                // If product has no CompanyId assigned, assign it to current company (migration for legacy data)
                if (existing.CompanyId == null && companyId != null)
                    existing.CompanyId = companyId;

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

        [HttpPut]
        [Route("api/products/{id}/status")]
        public async Task<IActionResult> UpdateProductStatus(int id, [FromBody] StatusUpdateRequest request)
        {
            try
            {
                var companyId = GetCompanyId();
                var product = await _context.Products.FindAsync(id);
                if (product == null) 
                    return NotFound(new { success = false, message = "Product not found" });
                if (companyId != null && product.CompanyId != null && product.CompanyId != companyId)
                    return NotFound(new { success = false, message = "Product not found" });

                // Assign CompanyId if not set (legacy data migration)
                if (product.CompanyId == null && companyId != null)
                    product.CompanyId = companyId;

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

        [HttpPut]
        [Route("api/products/{id}/stock")]
        public async Task<IActionResult> UpdateStock(int id, [FromBody] StockUpdateRequest request)
        {
            try
            {
                var companyId = GetCompanyId();
                var product = await _context.Products
                    .AsTracking()
                    .FirstOrDefaultAsync(p => p.ProductId == id && (companyId == null || p.CompanyId == companyId || p.CompanyId == null));
                if (product == null) return NotFound();

                // Assign CompanyId if not set (legacy data migration)
                if (product.CompanyId == null && companyId != null)
                    product.CompanyId = companyId;

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

        [HttpDelete]
        [Route("api/products/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var companyId = GetCompanyId();
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            if (companyId != null && product.CompanyId != null && product.CompanyId != companyId) return NotFound();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Product deleted successfully" });
        }

        [HttpGet]
        [Route("api/product-categories")]
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

        [HttpGet]
        [Route("api/product-categories/all")]
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

        [HttpGet]
        [Route("api/product-categories/{id}")]
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

        [HttpPost]
        [Route("api/product-categories")]
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

        [HttpPut]
        [Route("api/product-categories/{id}")]
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

        [HttpPut]
        [Route("api/product-categories/{id}/status")]
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

        [HttpGet]
        [Route("api/brands")]
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

        [HttpGet]
        [Route("api/stock-alerts")]
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

        [HttpPost]
        [Route("api/stock-alerts")]
        public async Task<IActionResult> CreateStockAlert([FromBody] StockAlertRequest request)
        {
            try
            {
                var companyId = GetCompanyId();
                // Verify product belongs to company
                var product = await _context.Products.FindAsync(request.ProductId);
                if (product == null)
                    return NotFound(new { success = false, message = "Product not found" });
                if (companyId != null && product.CompanyId != null && product.CompanyId != companyId)
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

        [HttpPut]
        [Route("api/stock-alerts/{id}/resolve")]
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

        #region Purchase Orders API

        [HttpGet]
        [Route("api/purchase-orders")]
        public async Task<IActionResult> GetPurchaseOrders_Api()
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

        [HttpGet]
        [Route("api/purchase-orders/{id}")]
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

        [HttpPost]
        [Route("api/purchase-orders")]
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

        [HttpPut]
        [Route("api/purchase-orders/{id}/approve")]
        public async Task<IActionResult> ApprovePurchaseOrder(int id)
        {
            try
            {
                var companyId = GetCompanyId();
                var order = await _context.PurchaseOrders.FindAsync(id);
                if (order == null)
                    return NotFound(new { success = false, message = "Purchase order not found" });
                if (companyId != null && order.CompanyId != null && order.CompanyId != companyId)
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

        [HttpPut]
        [Route("api/purchase-orders/{id}/ship")]
        public async Task<IActionResult> ShipPurchaseOrder(int id)
        {
            try
            {
                var companyId = GetCompanyId();
                var order = await _context.PurchaseOrders.FindAsync(id);
                if (order == null)
                    return NotFound(new { success = false, message = "Purchase order not found" });
                if (companyId != null && order.CompanyId != null && order.CompanyId != companyId)
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

        [HttpPut]
        [Route("api/purchase-orders/{id}/complete")]
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

        #region Suppliers API

        [HttpGet]
        [Route("api/suppliers")]
        public async Task<IActionResult> GetSuppliers_Api()
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

        [HttpGet]
        [Route("api/suppliers/{id}")]
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

        [HttpPost]
        [Route("api/suppliers")]
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

        [HttpPut]
        [Route("api/suppliers/{id}")]
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

        [HttpPut]
        [Route("api/suppliers/{id}/toggle-status")]
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

        [HttpGet]
        [Route("api/suppliers/{id}/products")]
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

        #region Stock Adjustments API

        [HttpPost]
        [Route("api/stock-adjustments")]
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

        [HttpGet]
        [Route("api/stock-adjustments")]
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

        #region Inventory Transactions API

        [HttpGet]
        [Route("api/inventory-transactions")]
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
    }
}
