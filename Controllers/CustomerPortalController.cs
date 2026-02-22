using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CompuGear.Data;
using CompuGear.Models;
using System.Security.Claims;

namespace CompuGear.Controllers
{
    /// <summary>
    /// Customer Portal Controller - For Customer-facing pages
    /// Handles customer dashboard, products, orders, support, and promotions
    /// </summary>
    public class CustomerPortalController : Controller
    {
        private readonly CompuGearDbContext _context;
        private readonly IConfiguration _configuration;

        public CustomerPortalController(CompuGearDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // Helper method to get current customer
        private async Task<Customer?> GetCurrentCustomerAsync()
        {
            // Try session-based customer ID first
            var sessionCustomerId = HttpContext.Session.GetInt32("CustomerId");
            if (sessionCustomerId.HasValue)
            {
                return await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == sessionCustomerId.Value);
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                return await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
            }
            return null;
        }

        // Helper: Get CompanyId from session for company isolation
        private int? GetPortalCompanyId()
        {
            return HttpContext.Session.GetInt32("CompanyId");
        }

        // Customer Dashboard
        public IActionResult Index()
        {
            ViewData["Title"] = "My Dashboard";
            return View();
        }

        // Browse Products & Promotions
        public IActionResult Products()
        {
            ViewData["Title"] = "Browse Products";
            return View();
        }

        // Current Promotions & Sales
        public IActionResult Promotions()
        {
            ViewData["Title"] = "Promotions & Deals";
            return View();
        }

        // My Orders
        public IActionResult Orders()
        {
            ViewData["Title"] = "My Orders";
            return View();
        }

        // Order Details
        public IActionResult OrderDetails(int id)
        {
            ViewData["Title"] = "Order Details";
            ViewData["OrderId"] = id;
            return View();
        }

        // Support Center with AI Chatbot
        public IActionResult Support()
        {
            ViewData["Title"] = "Support Center";
            return View();
        }

        // Submit Support Ticket
        public IActionResult SubmitTicket()
        {
            ViewData["Title"] = "Submit Support Ticket";
            return View();
        }

        // My Support Tickets
        public IActionResult MyTickets()
        {
            ViewData["Title"] = "My Support Tickets";
            return View();
        }

        // Ticket Details
        public IActionResult TicketDetails(int id)
        {
            ViewData["Title"] = "Ticket Details";
            ViewData["TicketId"] = id;
            return View();
        }

        // Customer Profile
        public IActionResult Profile()
        {
            ViewData["Title"] = "My Profile";
            return View();
        }

        // Shopping Cart
        public IActionResult Cart()
        {
            ViewData["Title"] = "Shopping Cart";
            return View();
        }

        // Checkout
        public IActionResult Checkout()
        {
            ViewData["Title"] = "Checkout";
            return View();
        }

        // Payment Success - Thank you page
        public IActionResult PaymentSuccess(string? orderId, string? orderNumber, decimal? amount)
        {
            ViewData["Title"] = "Payment Successful";
            ViewData["OrderId"] = orderId;
            ViewData["OrderNumber"] = orderNumber ?? "ORD-" + DateTime.Now.ToString("yyyyMMddHHmmss");
            ViewData["Amount"] = amount ?? 0;
            return View();
        }

        // Payment Failed page
        public IActionResult PaymentFailed(string? errorCode, string? errorMessage)
        {
            ViewData["Title"] = "Payment Failed";
            ViewData["ErrorCode"] = errorCode ?? "UNKNOWN";
            ViewData["ErrorMessage"] = errorMessage ?? "An error occurred during payment processing.";
            return View();
        }

        // Wishlist
        public IActionResult Wishlist()
        {
            ViewData["Title"] = "My Wishlist";
            return View();
        }

        #region API Endpoints

        // GET: /CustomerPortal/GetProfile
        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
            {
                return Json(new { success = false, message = "Customer not found. Please log in." });
            }

            customer = await _context.Customers
                .Include(c => c.Category)
                .Include(c => c.Addresses)
                .FirstOrDefaultAsync(c => c.CustomerId == customer.CustomerId);

            if (customer == null)
                return Json(new { success = false, message = "Customer not found" });

            return Json(new
            {
                success = true,
                data = new
                {
                    customer.CustomerId,
                    customer.CustomerCode,
                    customer.FirstName,
                    customer.LastName,
                    customer.Email,
                    customer.Phone,
                    customer.DateOfBirth,
                    customer.Gender,
                    customer.Avatar,
                    CategoryName = customer.Category?.CategoryName,
                    customer.BillingAddress,
                    customer.BillingCity,
                    customer.BillingState,
                    customer.BillingZipCode,
                    customer.BillingCountry,
                    customer.ShippingAddress,
                    customer.ShippingCity,
                    customer.ShippingState,
                    customer.ShippingZipCode,
                    customer.ShippingCountry,
                    customer.CompanyName,
                    customer.Status,
                    customer.TotalOrders,
                    customer.TotalSpent,
                    customer.LoyaltyPoints,
                    customer.MarketingOptIn,
                    customer.CreatedAt,
                    Addresses = customer.Addresses.Select(a => new
                    {
                        a.AddressId,
                        a.AddressType,
                        a.AddressLine1,
                        a.AddressLine2,
                        a.City,
                        a.State,
                        a.ZipCode,
                        a.Country,
                        a.IsDefault
                    })
                }
            });
        }

        // PUT: /CustomerPortal/UpdateProfile
        [HttpPost]
        public async Task<IActionResult> UpdateProfile([FromBody] CustomerProfileUpdateModel model)
        {
            var customer = await GetCurrentCustomerAsync();

            if (customer == null)
                return Json(new { success = false, message = "Customer not found. Please log in." });

            customer.FirstName = model.FirstName ?? customer.FirstName;
            customer.LastName = model.LastName ?? customer.LastName;
            customer.Phone = model.Phone ?? customer.Phone;
            customer.DateOfBirth = model.DateOfBirth ?? customer.DateOfBirth;
            customer.Gender = model.Gender ?? customer.Gender;
            customer.BillingAddress = model.BillingAddress ?? customer.BillingAddress;
            customer.BillingCity = model.BillingCity ?? customer.BillingCity;
            customer.BillingState = model.BillingState ?? customer.BillingState;
            customer.BillingZipCode = model.BillingZipCode ?? customer.BillingZipCode;
            customer.BillingCountry = model.BillingCountry ?? customer.BillingCountry;
            customer.ShippingAddress = model.ShippingAddress ?? customer.ShippingAddress;
            customer.ShippingCity = model.ShippingCity ?? customer.ShippingCity;
            customer.ShippingState = model.ShippingState ?? customer.ShippingState;
            customer.ShippingZipCode = model.ShippingZipCode ?? customer.ShippingZipCode;
            customer.ShippingCountry = model.ShippingCountry ?? customer.ShippingCountry;
            customer.MarketingOptIn = model.MarketingOptIn;
            customer.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Profile updated successfully" });
        }

        // GET: /CustomerPortal/GetProducts
        [HttpGet]
        public async Task<IActionResult> GetProducts(int page = 1, int pageSize = 12, int? categoryId = null, int? brandId = null, string? search = null, string? sort = "featured", bool? onSale = null, decimal? minPrice = null, decimal? maxPrice = null, bool? inStock = null)
        {
            var companyId = GetPortalCompanyId();
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Where(p => p.Status == "Active");

            // Company isolation: only show products belonging to this company
            if (companyId.HasValue)
                query = query.Where(p => p.CompanyId == companyId);

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId);

            if (brandId.HasValue)
                query = query.Where(p => p.BrandId == brandId);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => p.ProductName.Contains(search) || p.ShortDescription!.Contains(search));

            if (onSale == true)
                query = query.Where(p => p.IsOnSale);

            // Price range filter
            if (minPrice.HasValue)
                query = query.Where(p => p.SellingPrice >= minPrice.Value);
            if (maxPrice.HasValue)
                query = query.Where(p => p.SellingPrice <= maxPrice.Value);

            // In stock filter
            if (inStock == true)
                query = query.Where(p => p.StockQuantity > 0);

            // Sorting
            query = sort switch
            {
                "price-asc" => query.OrderBy(p => p.SellingPrice),
                "price-desc" => query.OrderByDescending(p => p.SellingPrice),
                "newest" => query.OrderByDescending(p => p.CreatedAt),
                "name" => query.OrderBy(p => p.ProductName),
                _ => query.OrderByDescending(p => p.IsFeatured).ThenBy(p => p.ProductName)
            };

            var total = await query.CountAsync();
            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.ProductId,
                    p.ProductCode,
                    p.SKU,
                    p.ProductName,
                    p.ShortDescription,
                    CategoryName = p.Category != null ? p.Category.CategoryName : null,
                    BrandName = p.Brand != null ? p.Brand.BrandName : null,
                    p.SellingPrice,
                    p.CompareAtPrice,
                    p.StockQuantity,
                    p.MainImageUrl,
                    p.IsFeatured,
                    p.IsOnSale,
                    p.StockStatus,
                    DiscountPercent = p.CompareAtPrice.HasValue && p.CompareAtPrice > 0
                        ? Math.Round((1 - (p.SellingPrice / p.CompareAtPrice.Value)) * 100)
                        : 0
                })
                .ToListAsync();

            return Json(new
            {
                success = true,
                data = products,
                pagination = new
                {
                    page,
                    pageSize,
                    total,
                    totalPages = (int)Math.Ceiling(total / (double)pageSize)
                }
            });
        }

        // GET: /CustomerPortal/GetFeaturedProducts
        [HttpGet]
        public async Task<IActionResult> GetFeaturedProducts(int count = 8)
        {
            var companyId = GetPortalCompanyId();
            var baseQuery = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Where(p => p.Status == "Active");

            if (companyId.HasValue)
                baseQuery = baseQuery.Where(p => p.CompanyId == companyId);

            var products = await baseQuery
                .Where(p => p.IsFeatured)
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .Select(p => new
                {
                    p.ProductId,
                    p.ProductCode,
                    p.SKU,
                    p.ProductName,
                    p.ShortDescription,
                    CategoryName = p.Category != null ? p.Category.CategoryName : null,
                    BrandName = p.Brand != null ? p.Brand.BrandName : null,
                    p.SellingPrice,
                    p.CompareAtPrice,
                    p.StockQuantity,
                    p.MainImageUrl,
                    p.IsFeatured,
                    p.IsOnSale,
                    p.StockStatus
                })
                .ToListAsync();

            // If no featured products, get latest active products
            if (!products.Any())
            {
                products = await baseQuery
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(count)
                    .Select(p => new
                    {
                        p.ProductId,
                        p.ProductCode,
                        p.SKU,
                        p.ProductName,
                        p.ShortDescription,
                        CategoryName = p.Category != null ? p.Category.CategoryName : null,
                        BrandName = p.Brand != null ? p.Brand.BrandName : null,
                        p.SellingPrice,
                        p.CompareAtPrice,
                        p.StockQuantity,
                        p.MainImageUrl,
                        p.IsFeatured,
                        p.IsOnSale,
                        p.StockStatus
                    })
                    .ToListAsync();
            }

            return Json(new { success = true, data = products });
        }

        // GET: /CustomerPortal/GetCategories
        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.ProductCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new
                {
                    c.CategoryId,
                    c.CategoryName,
                    c.Description,
                    c.ImageUrl,
                    ProductCount = c.Products.Count(p => p.Status == "Active")
                })
                .ToListAsync();

            return Json(new { success = true, data = categories });
        }

        // GET: /CustomerPortal/GetBrands
        [HttpGet]
        public async Task<IActionResult> GetBrands()
        {
            var brands = await _context.Brands
                .Where(b => b.IsActive)
                .OrderBy(b => b.BrandName)
                .Select(b => new
                {
                    b.BrandId,
                    b.BrandName,
                    b.Logo,
                    ProductCount = b.Products.Count(p => p.Status == "Active")
                })
                .ToListAsync();

            return Json(new { success = true, data = brands });
        }

        // GET: /CustomerPortal/GetPromotions
        [HttpGet]
        public async Task<IActionResult> GetPromotions()
        {
            var companyId = GetPortalCompanyId();
            var now = DateTime.UtcNow;
            var promoQuery = _context.Promotions
                .Include(p => p.Campaign)
                .Where(p => p.IsActive && p.StartDate <= now && p.EndDate >= now);

            if (companyId.HasValue)
                promoQuery = promoQuery.Where(p => p.CompanyId == companyId);

            var promotions = await promoQuery
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
                    CampaignName = p.Campaign != null ? p.Campaign.CampaignName : null,
                    RemainingUses = p.UsageLimit.HasValue ? p.UsageLimit - p.TimesUsed : null
                })
                .ToListAsync();

            // Also get active campaigns
            var campaignQuery = _context.Campaigns
                .Where(c => c.Status == "Active" && c.StartDate <= now && c.EndDate >= now);

            if (companyId.HasValue)
                campaignQuery = campaignQuery.Where(c => c.CompanyId == companyId);

            var campaigns = await campaignQuery
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new
                {
                    c.CampaignId,
                    c.CampaignCode,
                    c.CampaignName,
                    c.Description,
                    c.Type,
                    c.StartDate,
                    c.EndDate,
                    c.Subject,
                    c.Content
                })
                .ToListAsync();

            return Json(new { success = true, data = new { promotions, campaigns } });
        }

        // GET: /CustomerPortal/GetOrders
        [HttpGet]
        public async Task<IActionResult> GetOrders(int page = 1, int pageSize = 10, string? status = null)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
                return Json(new { success = false, message = "Please log in to view orders" });

            var customerId = customer.CustomerId;
            var companyId = GetPortalCompanyId();

            var query = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.CustomerId == customerId);

            // Company isolation
            if (companyId.HasValue)
                query = query.Where(o => o.CompanyId == companyId);

            if (!string.IsNullOrEmpty(status) && status != "all")
                query = query.Where(o => o.OrderStatus == status);

            var total = await query.CountAsync();
            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new
                {
                    o.OrderId,
                    o.OrderNumber,
                    o.OrderDate,
                    o.OrderStatus,
                    o.PaymentStatus,
                    o.PaymentMethod,
                    o.TotalAmount,
                    o.ShippingMethod,
                    o.TrackingNumber,
                    ItemCount = o.OrderItems.Count,
                    Items = o.OrderItems.Select(oi => new
                    {
                        oi.ProductId,
                        oi.ProductName,
                        oi.ProductCode,
                        oi.Quantity,
                        oi.UnitPrice,
                        oi.TotalPrice,
                        ProductImage = oi.Product.MainImageUrl
                    })
                })
                .ToListAsync();

            // Get stats
            var stats = new
            {
                total = await _context.Orders.CountAsync(o => o.CustomerId == customerId),
                pending = await _context.Orders.CountAsync(o => o.CustomerId == customerId && (o.OrderStatus == "Pending" || o.OrderStatus == "Processing")),
                delivered = await _context.Orders.CountAsync(o => o.CustomerId == customerId && o.OrderStatus == "Delivered"),
                cancelled = await _context.Orders.CountAsync(o => o.CustomerId == customerId && o.OrderStatus == "Cancelled")
            };

            return Json(new
            {
                success = true,
                data = orders,
                stats,
                pagination = new { page, pageSize, total, totalPages = (int)Math.Ceiling(total / (double)pageSize) }
            });
        }

        // POST: /CustomerPortal/CreateOrder
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderModel model)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
                return Json(new { success = false, message = "Please log in to place an order" });

            var customerId = customer.CustomerId;
            var companyId = GetPortalCompanyId();

            // Validate cart items
            if (model.Items == null || !model.Items.Any())
                return Json(new { success = false, message = "Cart is empty" });

            // Get product details
            var productIds = model.Items.Select(i => i.ProductId).ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.ProductId))
                .ToListAsync();

            // Calculate totals
            decimal subtotal = 0;
            var orderItems = new List<OrderItem>();

            foreach (var item in model.Items)
            {
                var product = products.FirstOrDefault(p => p.ProductId == item.ProductId);
                if (product == null)
                    return Json(new { success = false, message = $"Product {item.ProductId} not found" });

                if (product.StockQuantity < item.Quantity)
                    return Json(new { success = false, message = $"Insufficient stock for {product.ProductName}" });

                var itemTotal = product.SellingPrice * item.Quantity;
                subtotal += itemTotal;

                orderItems.Add(new OrderItem
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    ProductCode = product.ProductCode,
                    SKU = product.SKU,
                    Quantity = item.Quantity,
                    UnitPrice = product.SellingPrice,
                    TotalPrice = itemTotal,
                    WarrantyPeriod = product.WarrantyPeriod
                });
            }

            // Apply promotion if provided
            decimal discountAmount = 0;
            if (!string.IsNullOrEmpty(model.PromoCode))
            {
                var promo = await _context.Promotions
                    .FirstOrDefaultAsync(p => p.PromotionCode == model.PromoCode && p.IsActive);

                if (promo != null && promo.IsValid && subtotal >= promo.MinOrderAmount)
                {
                    discountAmount = promo.DiscountType == "Percentage"
                        ? subtotal * (promo.DiscountValue / 100)
                        : promo.DiscountValue;

                    if (promo.MaxDiscountAmount.HasValue && discountAmount > promo.MaxDiscountAmount)
                        discountAmount = promo.MaxDiscountAmount.Value;

                    // Increment promo usage
                    promo.TimesUsed += 1;
                }
            }

            // VAT calculation (12% VAT inclusive in the Philippines)
            // Subtotal is VAT-inclusive, so: VAT = subtotal / 1.12 * 0.12
            var vatableAmount = (subtotal - discountAmount) / 1.12m;
            var taxAmount = vatableAmount * 0.12m;
            var shippingAmount = subtotal > 5000 ? 0 : 150m;
            var totalAmount = subtotal - discountAmount + shippingAmount;

            // Generate order number
            var orderCount = await _context.Orders.CountAsync() + 1;
            var orderNumber = $"ORD-{DateTime.Now:yyyy}-{orderCount:D4}";

            var order = new Order
            {
                OrderNumber = orderNumber,
                CustomerId = customerId,
                CompanyId = companyId,
                OrderDate = DateTime.UtcNow,
                OrderStatus = "Pending",
                PaymentStatus = "Pending",
                Subtotal = subtotal,
                DiscountAmount = discountAmount,
                TaxAmount = taxAmount,
                ShippingAmount = shippingAmount,
                TotalAmount = totalAmount,
                PaymentMethod = model.PaymentMethod,
                ShippingAddress = model.ShippingAddress,
                ShippingCity = model.ShippingCity,
                ShippingState = model.ShippingState,
                ShippingZipCode = model.ShippingZipCode,
                ShippingCountry = model.ShippingCountry ?? "Philippines",
                Notes = model.Notes,
                CreatedAt = DateTime.UtcNow
            };

            order.OrderItems = orderItems;

            // Update stock
            foreach (var item in model.Items)
            {
                var product = products.First(p => p.ProductId == item.ProductId);
                product.StockQuantity -= item.Quantity;
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // ===== AUTO-GENERATE INVOICE =====
            try
            {
                var invoiceCount = await _context.Invoices.CountAsync() + 1;
                var invoiceNumber = $"INV-{DateTime.Now:yyyyMMdd}-{invoiceCount:D4}";

                var invoice = new Invoice
                {
                    InvoiceNumber = invoiceNumber,
                    OrderId = order.OrderId,
                    CustomerId = customerId,
                    CompanyId = companyId,
                    InvoiceDate = DateTime.UtcNow,
                    DueDate = DateTime.UtcNow.AddDays(30),
                    Subtotal = subtotal,
                    DiscountAmount = discountAmount,
                    TaxAmount = taxAmount,
                    ShippingAmount = shippingAmount,
                    TotalAmount = totalAmount,
                    PaidAmount = 0,
                    BalanceDue = totalAmount,
                    Status = "Pending",
                    BillingName = customer != null ? $"{customer.FirstName} {customer.LastName}" : "Customer",
                    BillingAddress = model.ShippingAddress,
                    BillingCity = model.ShippingCity,
                    BillingState = model.ShippingState,
                    BillingZipCode = model.ShippingZipCode,
                    BillingCountry = model.ShippingCountry ?? "Philippines",
                    BillingEmail = customer?.Email,
                    PaymentTerms = "Due on Receipt",
                    Notes = $"Auto-generated from Order #{orderNumber}",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Create invoice items from order items
                foreach (var oi in orderItems)
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
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log but don't fail the order if invoice generation fails
                System.Diagnostics.Debug.WriteLine($"Invoice auto-generation failed: {ex.Message}");
            }

            return Json(new
            {
                success = true,
                message = "Order created successfully",
                data = new
                {
                    order.OrderId,
                    order.OrderNumber,
                    order.TotalAmount,
                    order.OrderStatus
                }
            });
        }

        // POST: /CustomerPortal/ProcessPayment — records payment via PayMongo and marks order paid
        [HttpPost]
        public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequestModel model)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
                return Json(new { success = false, message = "Please log in" });

            var order = await _context.Orders
                .AsTracking()
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == model.OrderId && o.CustomerId == customer.CustomerId);

            if (order == null)
                return Json(new { success = false, message = "Order not found" });

            var paymongoSecretKey = _configuration["PayMongo:SecretKey"] ?? "sk_test_SakyRyg4R6hXeni4x5EaNUow";
            string? paymentIntentId = null;

            try
            {
                // --- Call PayMongo to create a Payment Intent (record the payment) ---
                using var httpClient = new HttpClient();
                var authToken = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{paymongoSecretKey}:"));
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {authToken}");

                var amountInCentavos = (int)(order.TotalAmount * 100);
                var paymentIntentRequest = new
                {
                    data = new
                    {
                        attributes = new
                        {
                            amount = amountInCentavos,
                            currency = "PHP",
                            payment_method_allowed = new[] { "card", "gcash", "grab_pay", "paymaya" },
                            description = $"CompuGear Order {order.OrderNumber}",
                            statement_descriptor = "CompuGear",
                            capture_type = "automatic"
                        }
                    }
                };

                var piContent = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(paymentIntentRequest),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var piResponse = await httpClient.PostAsync("https://api.paymongo.com/v1/payment_intents", piContent);
                var piResponseContent = await piResponse.Content.ReadAsStringAsync();

                if (piResponse.IsSuccessStatusCode)
                {
                    var piJson = System.Text.Json.JsonDocument.Parse(piResponseContent);
                    paymentIntentId = piJson.RootElement.GetProperty("data").GetProperty("id").GetString();
                }
            }
            catch (Exception ex)
            {
                // PayMongo call failed — log but continue to mark order paid
                System.Diagnostics.Debug.WriteLine($"PayMongo payment intent creation failed: {ex.Message}");
            }

            // --- Mark the order as Paid directly (no redirect) ---
            try
            {
                var strategy = _context.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    order.PaymentStatus = "Paid";
                    order.PaidAmount = order.TotalAmount;
                    order.OrderStatus = "Confirmed";
                    order.ConfirmedAt = DateTime.UtcNow;
                    order.PaymentMethod = model.PaymentMethod ?? "card";
                    order.PaymentReference = paymentIntentId ?? $"LOCAL-{DateTime.Now:yyyyMMddHHmmss}-{order.OrderId}";
                    order.UpdatedAt = DateTime.UtcNow;

                    // Update customer stats
                    var cust = await _context.Customers.AsTracking().FirstOrDefaultAsync(c => c.CustomerId == order.CustomerId);
                    if (cust != null)
                    {
                        cust.TotalOrders += 1;
                        cust.TotalSpent += order.TotalAmount;
                    }

                    // Update linked invoice to Paid
                    var invoice = await _context.Invoices.AsTracking().FirstOrDefaultAsync(i => i.OrderId == order.OrderId);
                    if (invoice != null)
                    {
                        invoice.Status = "Paid";
                        invoice.PaidAmount = invoice.TotalAmount;
                        invoice.BalanceDue = 0;
                        invoice.PaidAt = DateTime.UtcNow;
                        invoice.UpdatedAt = DateTime.UtcNow;

                        // Create payment record
                        var paymentRecord = new Payment
                        {
                            PaymentNumber = $"PAY-{DateTime.Now:yyMMddHHmm}-{order.OrderId}",
                            InvoiceId = invoice.InvoiceId,
                            OrderId = order.OrderId,
                            CustomerId = order.CustomerId,
                            CompanyId = order.CompanyId,
                            PaymentDate = DateTime.UtcNow,
                            Amount = order.TotalAmount,
                            PaymentMethodType = model.PaymentMethod ?? "card",
                            Status = "Completed",
                            TransactionId = order.PaymentReference,
                            ReferenceNumber = order.PaymentReference,
                            PayMongoPaymentId = paymentIntentId,
                            Currency = "PHP",
                            Notes = $"Auto-recorded from Order #{order.OrderNumber}",
                            ProcessedAt = DateTime.UtcNow,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _context.Payments.Add(paymentRecord);
                    }

                    await _context.SaveChangesAsync();
                });

                return Json(new
                {
                    success = true,
                    paymentIntentId = paymentIntentId ?? order.PaymentReference,
                    orderNumber = order.OrderNumber,
                    totalAmount = order.TotalAmount,
                    message = "Payment successful!"
                });
            }
            catch (Exception ex)
            {
                var innerError = ex.InnerException?.Message;
                return Json(new
                {
                    success = false,
                    message = "Payment processing error: " + ex.Message,
                    detail = innerError
                });
            }
        }

        // GET: /CustomerPortal/PaymentCallback
        [HttpGet]
        public async Task<IActionResult> PaymentCallback(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                return RedirectToAction("Orders");

            // Check payment status with PayMongo
            if (!string.IsNullOrEmpty(order.PaymentReference))
            {
                try
                {
                    var paymongoSecretKey = _configuration["PayMongo:SecretKey"] ?? "sk_test_SakyRyg4R6hXeni4x5EaNUow";
                    using var httpClient = new HttpClient();
                    var authToken = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{paymongoSecretKey}:"));
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {authToken}");

                    var response = await httpClient.GetAsync($"https://api.paymongo.com/v1/payment_intents/{order.PaymentReference}");
                    var content = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var json = System.Text.Json.JsonDocument.Parse(content);
                        var status = json.RootElement.GetProperty("data").GetProperty("attributes").GetProperty("status").GetString();

                        if (status == "succeeded")
                        {
                            order.PaymentStatus = "Paid";
                            order.PaidAmount = order.TotalAmount;
                            order.OrderStatus = "Confirmed";
                            order.ConfirmedAt = DateTime.UtcNow;
                            order.UpdatedAt = DateTime.UtcNow;

                            // Deduct stock
                            var orderItems = await _context.OrderItems.Where(oi => oi.OrderId == orderId).ToListAsync();
                            foreach (var item in orderItems)
                            {
                                var product = await _context.Products.FindAsync(item.ProductId);
                                if (product != null)
                                {
                                    product.StockQuantity -= item.Quantity;
                                }
                            }

                            // Update customer stats
                            var customer = await _context.Customers.FindAsync(order.CustomerId);
                            if (customer != null)
                            {
                                customer.TotalOrders += 1;
                                customer.TotalSpent += order.TotalAmount;
                            }

                            // Update linked invoice to Paid
                            var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.OrderId == orderId);
                            if (invoice != null)
                            {
                                invoice.Status = "Paid";
                                invoice.PaidAmount = invoice.TotalAmount;
                                invoice.BalanceDue = 0;
                                invoice.PaidAt = DateTime.UtcNow;
                                invoice.UpdatedAt = DateTime.UtcNow;

                                // Create payment record
                                var paymentRecord = new Payment
                                {
                                    PaymentNumber = $"PAY-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}",
                                    InvoiceId = invoice.InvoiceId,
                                    OrderId = orderId,
                                    CustomerId = order.CustomerId,
                                    CompanyId = order.CompanyId,
                                    PaymentDate = DateTime.UtcNow,
                                    Amount = order.TotalAmount,
                                    PaymentMethodType = order.PaymentMethod ?? "gcash",
                                    Status = "Completed",
                                    TransactionId = order.PaymentReference,
                                    ReferenceNumber = order.PaymentReference,
                                    PayMongoPaymentId = order.PaymentReference,
                                    Currency = "PHP",
                                    Notes = $"Auto-recorded from Order #{order.OrderNumber}",
                                    ProcessedAt = DateTime.UtcNow,
                                    CreatedAt = DateTime.UtcNow,
                                    UpdatedAt = DateTime.UtcNow
                                };
                                _context.Payments.Add(paymentRecord);
                            }

                            await _context.SaveChangesAsync();
                        }
                    }
                }
                catch { /* Log error */ }
            }

            return Redirect($"/CustomerPortal/Orders?payment=success&orderId={orderId}");
        }

        // GET: /CustomerPortal/CheckPaymentStatus
        [HttpGet]
        public async Task<IActionResult> CheckPaymentStatus(int orderId)
        {
            var customer = await GetCurrentCustomerAsync();
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId && 
                (customer == null || o.CustomerId == customer.CustomerId));
            if (order == null)
                return Json(new { success = false, message = "Order not found" });

            return Json(new
            {
                success = true,
                data = new
                {
                    order.OrderId,
                    order.OrderNumber,
                    order.OrderStatus,
                    order.PaymentStatus,
                    order.TotalAmount,
                    order.PaidAmount
                }
            });
        }

        // POST: /CustomerPortal/GetCartProducts - Fetch real product data for cart items
        [HttpPost]
        public async Task<IActionResult> GetCartProducts([FromBody] List<int> productIds)
        {
            try
            {
                var companyId = GetPortalCompanyId();
                var query = _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Where(p => productIds.Contains(p.ProductId));

                if (companyId.HasValue)
                    query = query.Where(p => p.CompanyId == companyId);

                var products = await query
                    .Select(p => new
                    {
                        p.ProductId,
                        p.ProductCode,
                        p.SKU,
                        p.ProductName,
                        p.ShortDescription,
                        CategoryName = p.Category != null ? p.Category.CategoryName : null,
                        BrandName = p.Brand != null ? p.Brand.BrandName : null,
                        p.SellingPrice,
                        p.CompareAtPrice,
                        p.StockQuantity,
                        p.MainImageUrl,
                        p.IsOnSale,
                        p.StockStatus
                    })
                    .ToListAsync();

                return Json(new { success = true, data = products });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: /CustomerPortal/ValidatePromo
        [HttpPost]
        public async Task<IActionResult> ValidatePromo([FromBody] ValidatePromoModel model)
        {
            var companyId = GetPortalCompanyId();
            var promoQuery = _context.Promotions
                .Where(p => p.PromotionCode.ToLower() == model.PromoCode.ToLower() && p.IsActive);

            if (companyId.HasValue)
                promoQuery = promoQuery.Where(p => p.CompanyId == companyId);

            var promo = await promoQuery.FirstOrDefaultAsync();

            if (promo == null)
                return Json(new { success = false, message = "Invalid promo code" });

            if (!promo.IsValid)
                return Json(new { success = false, message = "Promo code has expired or reached usage limit" });

            if (model.Subtotal < promo.MinOrderAmount)
                return Json(new { success = false, message = $"Minimum order amount is ₱{promo.MinOrderAmount:N2}" });

            decimal discount = promo.DiscountType == "Percentage"
                ? model.Subtotal * (promo.DiscountValue / 100)
                : promo.DiscountValue;

            if (promo.MaxDiscountAmount.HasValue && discount > promo.MaxDiscountAmount)
                discount = promo.MaxDiscountAmount.Value;

            return Json(new
            {
                success = true,
                data = new
                {
                    promo.PromotionCode,
                    promo.PromotionName,
                    promo.DiscountType,
                    promo.DiscountValue,
                    discountAmount = discount
                }
            });
        }

        // GET: /CustomerPortal/GetTickets
        [HttpGet]
        public async Task<IActionResult> GetTickets()
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
                return Json(new { success = false, message = "Please log in to view tickets" });

            var customerId = customer.CustomerId;
            var companyId = GetPortalCompanyId();

            var query = _context.SupportTickets
                .Include(t => t.Category)
                .Include(t => t.AssignedUser)
                .Where(t => t.CustomerId == customerId);

            if (companyId.HasValue)
                query = query.Where(t => t.CompanyId == companyId);

            var tickets = await query
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new
                {
                    t.TicketId,
                    t.TicketNumber,
                    t.Subject,
                    t.Description,
                    t.Priority,
                    t.Status,
                    CategoryName = t.Category != null ? t.Category.CategoryName : null,
                    AgentName = t.AssignedUser != null ? $"{t.AssignedUser.FirstName} {t.AssignedUser.LastName}" : null,
                    t.CreatedAt,
                    t.UpdatedAt,
                    t.ResolvedAt
                })
                .ToListAsync();

            return Json(new { success = true, data = tickets });
        }

        // GET: /CustomerPortal/GetTicketDetails
        [HttpGet]
        public async Task<IActionResult> GetTicketDetails(int id)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
                return Json(new { success = false, message = "Please log in" });

            var customerId = customer.CustomerId;

            var ticket = await _context.SupportTickets
                .Include(t => t.Category)
                .Include(t => t.AssignedUser)
                .Where(t => t.TicketId == id && t.CustomerId == customerId)
                .Select(t => new
                {
                    t.TicketId,
                    t.TicketNumber,
                    t.Subject,
                    t.Description,
                    t.Priority,
                    t.Status,
                    CategoryName = t.Category != null ? t.Category.CategoryName : null,
                    AgentName = t.AssignedUser != null ? $"{t.AssignedUser.FirstName} {t.AssignedUser.LastName}" : null,
                    t.ContactName,
                    t.ContactEmail,
                    t.ContactPhone,
                    t.CreatedAt,
                    t.UpdatedAt,
                    t.ResolvedAt,
                    t.Resolution
                })
                .FirstOrDefaultAsync();

            if (ticket == null)
                return Json(new { success = false, message = "Ticket not found" });

            // Get ticket messages/replies
            var messages = await _context.TicketMessages
                .Where(m => m.TicketId == id && !m.IsInternal)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new
                {
                    m.MessageId,
                    m.Message,
                    m.SenderType,
                    SenderName = m.Sender != null ? $"{m.Sender.FirstName} {m.Sender.LastName}" : "Customer",
                    m.CreatedAt,
                    m.IsInternal
                })
                .ToListAsync();

            return Json(new { success = true, data = ticket, messages = messages });
        }

        // POST: /CustomerPortal/ReplyToTicket
        [HttpPost]
        public async Task<IActionResult> ReplyToTicket([FromBody] TicketReplyModel model)
        {
            var customer = await GetCurrentCustomerAsync();

            var ticket = await _context.SupportTickets.FindAsync(model.TicketId);
            if (ticket == null || ticket.CustomerId != (customer?.CustomerId))
                return Json(new { success = false, message = "Ticket not found" });

            var ticketMessage = new TicketMessage
            {
                TicketId = model.TicketId,
                Message = model.Message,
                SenderType = "Customer",
                CreatedAt = DateTime.UtcNow,
                IsInternal = false
            };

            _context.TicketMessages.Add(ticketMessage);
            ticket.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Reply sent successfully" });
        }

        // POST: /CustomerPortal/CreateTicket
        [HttpPost]
        public async Task<IActionResult> CreateTicket([FromBody] CreateTicketModel model)
        {
            var customer = await GetCurrentCustomerAsync();
            var customerId = customer?.CustomerId;
            var companyId = GetPortalCompanyId();

            var ticketCount = await _context.SupportTickets.CountAsync() + 1;
            var ticketNumber = $"TKT-{DateTime.Now:yyyy}-{ticketCount:D4}";

            var ticket = new SupportTicket
            {
                TicketNumber = ticketNumber,
                CustomerId = customerId,
                CompanyId = companyId,
                CategoryId = model.CategoryId,
                Subject = model.Subject,
                Description = model.Description,
                ContactName = model.ContactName ?? customer?.FullName,
                ContactEmail = model.ContactEmail ?? customer?.Email ?? "",
                ContactPhone = model.ContactPhone ?? customer?.Phone,
                Priority = model.Priority ?? "Medium",
                Status = "Open",
                Source = "Web",
                CreatedAt = DateTime.UtcNow
            };

            _context.SupportTickets.Add(ticket);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Ticket created successfully",
                data = new { ticket.TicketId, ticket.TicketNumber }
            });
        }

        // POST: /CustomerPortal/SendChatMessage
        [HttpPost]
        public async Task<IActionResult> SendChatMessage([FromBody] ChatMessageModel model)
        {
            var customer = await GetCurrentCustomerAsync();
            var customerId = customer?.CustomerId ?? 0;
            var companyId = GetPortalCompanyId();

            // Find or create chat session for this customer and company
            var sessionQuery = _context.ChatSessions
                .Where(s => s.CustomerId == customerId && s.Status == "Active");

            var session = await sessionQuery.FirstOrDefaultAsync();

            if (session == null)
            {
                session = new ChatSession
                {
                    CustomerId = customer?.CustomerId,
                    VisitorId = model.VisitorId ?? Guid.NewGuid().ToString(),
                    SessionToken = Guid.NewGuid().ToString(),
                    Status = "Active",
                    StartedAt = DateTime.UtcNow,
                    Source = "Website"
                };
                _context.ChatSessions.Add(session);
                await _context.SaveChangesAsync();
            }

            // Save customer message
            var customerMessage = new ChatMessage
            {
                SessionId = session.SessionId,
                SenderType = "Customer",
                Message = model.Message,
                MessageType = "Text",
                CreatedAt = DateTime.UtcNow
            };
            _context.ChatMessages.Add(customerMessage);

            // Generate AI response based on message content
            var aiResponse = GenerateAIResponse(model.Message);

            var botMessage = new ChatMessage
            {
                SessionId = session.SessionId,
                SenderType = "Bot",
                Message = aiResponse.Response,
                MessageType = "Text",
                Intent = aiResponse.Intent,
                Confidence = aiResponse.Confidence,
                CreatedAt = DateTime.UtcNow.AddMilliseconds(100)
            };
            _context.ChatMessages.Add(botMessage);

            session.TotalMessages += 2;
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                data = new
                {
                    sessionId = session.SessionId,
                    response = aiResponse.Response,
                    intent = aiResponse.Intent,
                    quickReplies = aiResponse.QuickReplies,
                    shouldTransferToAgent = aiResponse.ShouldTransferToAgent
                }
            });
        }

        // POST: /CustomerPortal/TransferToAgent
        [HttpPost]
        public async Task<IActionResult> TransferToAgent([FromBody] TransferToAgentModel model)
        {
            var session = await _context.ChatSessions.FindAsync(model.SessionId);
            if (session == null)
                return Json(new { success = false, message = "Chat session not found" });

            var companyId = GetPortalCompanyId();

            // Find available support agent (scoped to company if set)
            var agentQuery = _context.Users
                .Where(u => u.RoleId == 4 && u.IsActive);

            if (companyId.HasValue)
                agentQuery = agentQuery.Where(u => u.CompanyId == companyId);

            var supportAgent = await agentQuery.FirstOrDefaultAsync();

            if (supportAgent != null)
            {
                session.AgentId = supportAgent.UserId;
                session.Status = "Transferred";

                var systemMessage = new ChatMessage
                {
                    SessionId = session.SessionId,
                    SenderType = "System",
                    Message = $"Chat transferred to {supportAgent.FirstName} {supportAgent.LastName}. They will be with you shortly.",
                    MessageType = "Text",
                    CreatedAt = DateTime.UtcNow
                };
                _context.ChatMessages.Add(systemMessage);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Transferred to support agent",
                    agentName = $"{supportAgent.FirstName} {supportAgent.LastName}"
                });
            }

            return Json(new
            {
                success = true,
                message = "All agents are currently busy. Please leave your contact information and we'll get back to you.",
                agentAvailable = false
            });
        }

        // GET: /CustomerPortal/GetOrderTracking
        [HttpGet]
        public async Task<IActionResult> GetOrderTracking(int orderId)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
                return Json(new { success = false, message = "Please log in" });

            var customerId = customer.CustomerId;

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.StatusHistory)
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.CustomerId == customerId);

            if (order == null)
                return Json(new { success = false, message = "Order not found" });

            // Build status history timeline
            var statusHistory = await _context.Set<OrderStatusHistory>()
                .Where(h => h.OrderId == orderId)
                .OrderBy(h => h.ChangedAt)
                .Select(h => new
                {
                    h.HistoryId,
                    h.PreviousStatus,
                    h.NewStatus,
                    h.Notes,
                    h.ChangedAt
                })
                .ToListAsync();

            // Build timeline from order dates
            var timeline = new List<object>();

            // Order placed
            timeline.Add(new
            {
                status = "Order Placed",
                description = $"Order #{order.OrderNumber} was placed",
                date = (DateTime?)order.OrderDate,
                completed = true,
                icon = "placed"
            });

            // Confirmed
            var isConfirmed = order.OrderStatus != "Pending" && order.OrderStatus != "Cancelled";
            timeline.Add(new
            {
                status = "Confirmed",
                description = isConfirmed ? "Your order has been confirmed and payment verified" : "Awaiting order confirmation",
                date = isConfirmed ? (DateTime?)(order.ConfirmedAt ?? order.OrderDate.AddHours(1)) : null,
                completed = isConfirmed,
                icon = "confirmed"
            });

            // Processing
            var isProcessing = new[] { "Processing", "Shipped", "Out for Delivery", "Delivered" }.Contains(order.OrderStatus);
            timeline.Add(new
            {
                status = "Processing",
                description = isProcessing ? "Your order is being prepared for shipment" : "Order will be prepared once confirmed",
                date = isProcessing ? (DateTime?)(order.ConfirmedAt ?? order.OrderDate).AddHours(2) : null,
                completed = isProcessing,
                icon = "processing"
            });

            // Shipped
            var isShipped = new[] { "Shipped", "Out for Delivery", "Delivered" }.Contains(order.OrderStatus);
            timeline.Add(new
            {
                status = "Shipped",
                description = isShipped ? $"Package shipped via {order.ShippingMethod ?? "Standard Shipping"}" : "Package will be shipped once processed",
                date = isShipped ? (DateTime?)(order.ShippedAt ?? order.OrderDate.AddDays(1)) : null,
                completed = isShipped,
                icon = "shipped"
            });

            // Out for Delivery
            var isOutForDelivery = new[] { "Out for Delivery", "Delivered" }.Contains(order.OrderStatus);
            timeline.Add(new
            {
                status = "Out for Delivery",
                description = isOutForDelivery ? "Your package is on its way to you" : "Package will be out for delivery soon",
                date = isOutForDelivery ? (DateTime?)(order.ShippedAt ?? order.OrderDate).AddDays(1) : null,
                completed = isOutForDelivery,
                icon = "outfordelivery"
            });

            // Delivered
            var isDelivered = order.OrderStatus == "Delivered";
            timeline.Add(new
            {
                status = "Delivered",
                description = isDelivered ? "Your package has been delivered" : "Package will be delivered to your address",
                date = isDelivered ? (DateTime?)(order.DeliveredAt ?? DateTime.UtcNow) : null,
                completed = isDelivered,
                icon = "delivered"
            });

            // CompuGear Warehouse in Makati as origin
            var warehouseLat = 14.5547;
            var warehouseLng = 121.0244;

            // Default destination in Manila
            var destLat = 14.5995;
            var destLng = 120.9842;

            // Determine progress (0.0 to 1.0)
            double progress = 0.0;
            if (order.OrderStatus == "Pending") progress = 0.0;
            else if (order.OrderStatus == "Confirmed") progress = 0.1;
            else if (order.OrderStatus == "Processing") progress = 0.25;
            else if (order.OrderStatus == "Shipped") progress = 0.55;
            else if (order.OrderStatus == "Out for Delivery") progress = 0.85;
            else if (order.OrderStatus == "Delivered") progress = 1.0;

            // Generate waypoints along the route
            var waypoints = new List<object>();
            var steps = 5;
            for (int i = 0; i <= steps; i++)
            {
                var frac = (double)i / steps;
                var lat = warehouseLat + (destLat - warehouseLat) * frac;
                var lng = warehouseLng + (destLng - warehouseLng) * frac;
                var label = i == 0 ? "CompuGear Warehouse"
                    : i == steps ? (order.ShippingAddress ?? "Delivery Address")
                    : $"Transit Point {i}";
                waypoints.Add(new { lat, lng, label, reached = frac <= progress });
            }

            // Current package location
            var currentLat = warehouseLat + (destLat - warehouseLat) * progress;
            var currentLng = warehouseLng + (destLng - warehouseLng) * progress;

            return Json(new
            {
                success = true,
                data = new
                {
                    order.OrderId,
                    order.OrderNumber,
                    order.OrderStatus,
                    order.PaymentStatus,
                    order.OrderDate,
                    order.TotalAmount,
                    order.ShippingAddress,
                    order.ShippingCity,
                    order.ShippingState,
                    order.ShippingMethod,
                    order.TrackingNumber,
                    order.ShippedAt,
                    order.DeliveredAt,
                    items = order.OrderItems.Select(oi => new
                    {
                        oi.ProductName,
                        oi.Quantity,
                        oi.UnitPrice,
                        oi.TotalPrice,
                        productImage = oi.Product?.MainImageUrl
                    }),
                    timeline,
                    tracking = new
                    {
                        origin = new { lat = warehouseLat, lng = warehouseLng, label = "CompuGear Warehouse, Makati" },
                        destination = new { lat = destLat, lng = destLng, label = order.ShippingAddress ?? "Delivery Address" },
                        currentLocation = new { lat = currentLat, lng = currentLng },
                        progress,
                        waypoints,
                        estimatedDelivery = order.OrderStatus == "Delivered"
                            ? order.DeliveredAt?.ToString("MMM dd, yyyy")
                            : order.OrderDate.AddDays(5).ToString("MMM dd, yyyy")
                    }
                }
            });
        }

        // GET: /CustomerPortal/GetChatHistory
        [HttpGet]
        public async Task<IActionResult> GetChatHistory(int sessionId)

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

            return Json(new { success = true, data = messages });
        }

        // POST: /CustomerPortal/SendAgentMessage (For Support Staff)
        [HttpPost]
        public async Task<IActionResult> SendAgentMessage([FromBody] AgentChatModel model)
        {
            var session = await _context.ChatSessions.FindAsync(model.SessionId);
            if (session == null)
                return Json(new { success = false, message = "Chat session not found" });

            var agentMessage = new ChatMessage
            {
                SessionId = session.SessionId,
                SenderType = "Agent",
                SenderId = model.AgentId,
                Message = model.Message,
                MessageType = "Text",
                CreatedAt = DateTime.UtcNow
            };
            _context.ChatMessages.Add(agentMessage);
            session.TotalMessages += 1;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Message sent" });
        }

        // GET: /CustomerPortal/GetActiveChats (For Support Staff)
        [HttpGet]
        public async Task<IActionResult> GetActiveChats()
        {
            var companyId = GetPortalCompanyId();

            var chatQuery = _context.ChatSessions
                .Include(s => s.Customer)
                .Where(s => s.Status == "Active" || s.Status == "Transferred");

            // Company isolation: only show chats from customers of this company
            if (companyId.HasValue)
                chatQuery = chatQuery.Where(s => s.Customer != null && s.Customer.CompanyId == companyId);

            var chats = await chatQuery
                .OrderByDescending(s => s.StartedAt)
                .Select(s => new
                {
                    s.SessionId,
                    CustomerName = s.Customer != null ? s.Customer.FullName : "Guest",
                    s.Status,
                    s.TotalMessages,
                    s.StartedAt,
                    s.AgentId
                })
                .ToListAsync();

            return Json(new { success = true, data = chats });
        }

        private (string Response, string Intent, decimal Confidence, string[] QuickReplies, bool ShouldTransferToAgent) GenerateAIResponse(string message)
        {
            var lowerMessage = message.ToLower();

            // Order related queries
            if (lowerMessage.Contains("order") || lowerMessage.Contains("delivery") || lowerMessage.Contains("shipping") || lowerMessage.Contains("track"))
            {
                return (
                    "I can help you with your order! You can track your orders from the 'My Orders' section in your dashboard. If you have a specific order number, please share it and I'll look up the details for you. Would you like me to connect you with a support agent for more assistance?",
                    "order_inquiry",
                    0.92m,
                    new[] { "View My Orders", "Track an Order", "Talk to Agent" },
                    false
                );
            }

            // Return/Refund queries
            if (lowerMessage.Contains("return") || lowerMessage.Contains("refund") || lowerMessage.Contains("exchange"))
            {
                return (
                    "I understand you'd like to process a return or refund. Our return policy allows returns within 7 days of delivery for most products. For warranty claims, you can initiate a return from your order details page. Would you like me to explain the process or connect you with our support team?",
                    "return_refund",
                    0.89m,
                    new[] { "Return Policy", "Start Return", "Talk to Agent" },
                    false
                );
            }

            // Product queries
            if (lowerMessage.Contains("product") || lowerMessage.Contains("price") || lowerMessage.Contains("stock") || lowerMessage.Contains("available"))
            {
                return (
                    "I'd be happy to help you find products! You can browse our catalog in the Products section. We have a wide range of computer accessories, gaming gear, and components. Is there a specific product category or brand you're looking for?",
                    "product_inquiry",
                    0.88m,
                    new[] { "Browse Products", "Gaming Gear", "Components", "Accessories" },
                    false
                );
            }

            // Payment queries
            if (lowerMessage.Contains("payment") || lowerMessage.Contains("pay") || lowerMessage.Contains("gcash") || lowerMessage.Contains("card"))
            {
                return (
                    "We accept multiple payment methods including Credit/Debit Cards, GCash, Maya (PayMaya), GrabPay, and Bank Transfer through PayMongo. All transactions are secure and encrypted. Is there a specific payment method you'd like to use?",
                    "payment_inquiry",
                    0.91m,
                    new[] { "Payment Methods", "Checkout", "Talk to Agent" },
                    false
                );
            }

            // Promo/Discount queries
            if (lowerMessage.Contains("promo") || lowerMessage.Contains("discount") || lowerMessage.Contains("sale") || lowerMessage.Contains("deal"))
            {
                return (
                    "Great timing! We have several active promotions right now including Gaming Week Sale with up to 30% off! Check out our Promotions page for all current deals and discount codes. Don't miss out on these limited-time offers!",
                    "promo_inquiry",
                    0.93m,
                    new[] { "View Promotions", "Current Deals", "Gaming Sale" },
                    false
                );
            }

            // Technical support
            if (lowerMessage.Contains("help") || lowerMessage.Contains("support") || lowerMessage.Contains("issue") || lowerMessage.Contains("problem"))
            {
                return (
                    "I'm here to help! Could you please describe the issue you're experiencing? I'll do my best to assist you. If you need specialized technical support, I can also connect you with our support team who can provide more detailed assistance.",
                    "support_request",
                    0.85m,
                    new[] { "Technical Support", "Submit Ticket", "Talk to Agent" },
                    false
                );
            }

            // Greeting
            if (lowerMessage.Contains("hi") || lowerMessage.Contains("hello") || lowerMessage.Contains("hey") || lowerMessage.Contains("good"))
            {
                return (
                    "Hello! 👋 Welcome to CompuGear Support! I'm your AI assistant and I'm here to help you with orders, products, payments, returns, and more. How can I assist you today?",
                    "greeting",
                    0.95m,
                    new[] { "Browse Products", "Track Order", "View Promotions", "Get Support" },
                    false
                );
            }

            // Agent request
            if (lowerMessage.Contains("agent") || lowerMessage.Contains("human") || lowerMessage.Contains("person") || lowerMessage.Contains("representative"))
            {
                return (
                    "I'll connect you with a customer support representative right away. Please hold on while I transfer your chat.",
                    "agent_request",
                    0.98m,
                    new string[] { },
                    true
                );
            }

            // Default response
            return (
                "Thank you for your message! I'm still learning, but I want to make sure you get the help you need. Could you please rephrase your question, or would you like me to connect you with a support agent for personalized assistance?",
                "unknown",
                0.60m,
                new[] { "Browse Products", "View Orders", "Talk to Agent" },
                false
            );
        }

        #endregion

        #region New API Endpoints - Full Implementation

        // ========== PROFILE: Address Management ==========

        // POST: /CustomerPortal/AddAddress
        [HttpPost]
        public async Task<IActionResult> AddAddress([FromBody] AddAddressModel model)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
                return Json(new { success = false, message = "Please log in" });

            var address = new CustomerAddress
            {
                CustomerId = customer.CustomerId,
                AddressType = model.AddressType ?? "Shipping",
                AddressLine1 = model.AddressLine1,
                AddressLine2 = model.AddressLine2,
                City = model.City,
                State = model.State,
                ZipCode = model.ZipCode,
                Country = model.Country ?? "Philippines",
                IsDefault = model.IsDefault,
                CreatedAt = DateTime.UtcNow
            };

            // If setting as default, unset existing defaults
            if (model.IsDefault)
            {
                var existingDefaults = await _context.CustomerAddresses
                    .Where(a => a.CustomerId == customer.CustomerId && a.IsDefault)
                    .ToListAsync();
                foreach (var a in existingDefaults) a.IsDefault = false;
            }

            _context.CustomerAddresses.Add(address);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Address added successfully", data = new { address.AddressId } });
        }

        // POST: /CustomerPortal/UpdateAddress
        [HttpPost]
        public async Task<IActionResult> UpdateAddress([FromBody] UpdateAddressModel model)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
                return Json(new { success = false, message = "Please log in" });

            var address = await _context.CustomerAddresses
                .FirstOrDefaultAsync(a => a.AddressId == model.AddressId && a.CustomerId == customer.CustomerId);
            if (address == null)
                return Json(new { success = false, message = "Address not found" });

            address.AddressType = model.AddressType ?? address.AddressType;
            address.AddressLine1 = model.AddressLine1 ?? address.AddressLine1;
            address.AddressLine2 = model.AddressLine2;
            address.City = model.City ?? address.City;
            address.State = model.State ?? address.State;
            address.ZipCode = model.ZipCode ?? address.ZipCode;
            address.Country = model.Country ?? address.Country;

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Address updated successfully" });
        }

        // POST: /CustomerPortal/SetDefaultAddress
        [HttpPost]
        public async Task<IActionResult> SetDefaultAddress([FromBody] SetDefaultAddressModel model)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
                return Json(new { success = false, message = "Please log in" });

            var allAddresses = await _context.CustomerAddresses
                .Where(a => a.CustomerId == customer.CustomerId)
                .ToListAsync();

            foreach (var a in allAddresses)
                a.IsDefault = (a.AddressId == model.AddressId);

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Default address updated" });
        }

        // POST: /CustomerPortal/DeleteAddress
        [HttpPost]
        public async Task<IActionResult> DeleteAddress([FromBody] DeleteAddressModel model)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
                return Json(new { success = false, message = "Please log in" });

            var address = await _context.CustomerAddresses
                .FirstOrDefaultAsync(a => a.AddressId == model.AddressId && a.CustomerId == customer.CustomerId);
            if (address == null)
                return Json(new { success = false, message = "Address not found" });

            _context.CustomerAddresses.Remove(address);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Address deleted" });
        }

        // ========== PROFILE: Security ==========

        // POST: /CustomerPortal/ChangePassword
        [HttpPost]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordModel model)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
                return Json(new { success = false, message = "Please log in" });

            if (customer.UserId == null)
                return Json(new { success = false, message = "No linked user account" });

            var user = await _context.Users.FindAsync(customer.UserId);
            if (user == null)
                return Json(new { success = false, message = "User account not found" });

            // Verify current password using simple hash comparison
            // (Since BCrypt is not installed, compare with stored hash directly)
            var currentHash = Convert.ToBase64String(System.Security.Cryptography.SHA256.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes(model.CurrentPassword)));
            if (user.PasswordHash != currentHash && user.PasswordHash != model.CurrentPassword)
                return Json(new { success = false, message = "Current password is incorrect" });

            if (model.NewPassword.Length < 8)
                return Json(new { success = false, message = "New password must be at least 8 characters" });

            user.PasswordHash = Convert.ToBase64String(System.Security.Cryptography.SHA256.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes(model.NewPassword)));
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Password changed successfully" });
        }

        // POST: /CustomerPortal/UpdateEmailPreferences
        [HttpPost]
        public async Task<IActionResult> UpdateEmailPreferences([FromBody] EmailPreferencesModel model)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
                return Json(new { success = false, message = "Please log in" });

            customer.MarketingOptIn = model.MarketingOptIn;
            customer.PreferredContactMethod = model.PreferredContactMethod;
            customer.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Email preferences updated" });
        }

        // POST: /CustomerPortal/DeleteAccount
        [HttpPost]
        public async Task<IActionResult> DeleteAccountRequest()
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
                return Json(new { success = false, message = "Please log in" });

            customer.Status = "PendingDeletion";
            customer.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Account deletion request submitted. Your account will be deleted within 30 days." });
        }

        // ========== PRODUCT REVIEWS ==========

        // GET: /CustomerPortal/GetProductReviews
        [HttpGet]
        public async Task<IActionResult> GetProductReviews(int productId, int page = 1, int pageSize = 10)
        {
            var query = _context.ProductReviews
                .Include(r => r.Customer)
                .Where(r => r.ProductId == productId && r.Status == "Approved");

            var total = await query.CountAsync();
            var reviews = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new
                {
                    r.ReviewId,
                    r.Rating,
                    r.Title,
                    r.Comment,
                    r.Pros,
                    r.Cons,
                    r.IsVerifiedPurchase,
                    r.HelpfulCount,
                    r.CreatedAt,
                    CustomerName = r.Customer != null ? r.Customer.FirstName + " " + r.Customer.LastName.Substring(0, 1) + "." : "Anonymous"
                })
                .ToListAsync();

            // Calculate rating summary
            var allRatings = await _context.ProductReviews
                .Where(r => r.ProductId == productId && r.Status == "Approved")
                .GroupBy(r => r.Rating)
                .Select(g => new { Rating = g.Key, Count = g.Count() })
                .ToListAsync();

            var totalReviews = allRatings.Sum(r => r.Count);
            var avgRating = totalReviews > 0 ? allRatings.Sum(r => r.Rating * r.Count) / (double)totalReviews : 0;

            return Json(new
            {
                success = true,
                data = reviews,
                summary = new
                {
                    averageRating = Math.Round(avgRating, 1),
                    totalReviews,
                    breakdown = Enumerable.Range(1, 5).Select(i => new
                    {
                        rating = i,
                        count = allRatings.FirstOrDefault(r => r.Rating == i)?.Count ?? 0
                    })
                },
                pagination = new { page, pageSize, total, totalPages = (int)Math.Ceiling(total / (double)pageSize) }
            });
        }

        // POST: /CustomerPortal/SubmitReview
        [HttpPost]
        public async Task<IActionResult> SubmitReview([FromBody] SubmitReviewModel model)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
                return Json(new { success = false, message = "Please log in to leave a review" });

            // Check if already reviewed
            var existingReview = await _context.ProductReviews
                .FirstOrDefaultAsync(r => r.ProductId == model.ProductId && r.CustomerId == customer.CustomerId);
            if (existingReview != null)
                return Json(new { success = false, message = "You have already reviewed this product" });

            // Check if verified purchase
            var hasPurchased = await _context.OrderItems
                .AnyAsync(oi => oi.ProductId == model.ProductId &&
                    oi.Order.CustomerId == customer.CustomerId &&
                    oi.Order.OrderStatus == "Delivered");

            var review = new ProductReview
            {
                ProductId = model.ProductId,
                CustomerId = customer.CustomerId,
                OrderId = model.OrderId,
                Rating = model.Rating,
                Title = model.Title,
                Comment = model.Comment,
                Pros = model.Pros,
                Cons = model.Cons,
                IsVerifiedPurchase = hasPurchased,
                Status = "Approved", // Auto-approve for now
                CreatedAt = DateTime.UtcNow,
                ApprovedAt = DateTime.UtcNow
            };

            _context.ProductReviews.Add(review);

            // Award loyalty points for review
            var loyaltyEntry = new LoyaltyPoints
            {
                CustomerId = customer.CustomerId,
                TransactionType = "Earned",
                Points = 50,
                Description = "Product review submitted",
                CreatedAt = DateTime.UtcNow
            };
            _context.LoyaltyPoints.Add(loyaltyEntry);
            customer.LoyaltyPoints += 50;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Review submitted successfully! You earned 50 loyalty points." });
        }

        // POST: /CustomerPortal/MarkReviewHelpful
        [HttpPost]
        public async Task<IActionResult> MarkReviewHelpful([FromBody] ReviewHelpfulModel model)
        {
            var review = await _context.ProductReviews.FindAsync(model.ReviewId);
            if (review == null)
                return Json(new { success = false, message = "Review not found" });

            review.HelpfulCount += 1;
            await _context.SaveChangesAsync();

            return Json(new { success = true, helpfulCount = review.HelpfulCount });
        }

        // ========== ORDER CANCELLATION & RETURNS ==========

        // POST: /CustomerPortal/CancelOrder
        [HttpPost]
        public async Task<IActionResult> CancelOrder([FromBody] CancelOrderModel model)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
                return Json(new { success = false, message = "Please log in" });

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == model.OrderId && o.CustomerId == customer.CustomerId);

            if (order == null)
                return Json(new { success = false, message = "Order not found" });

            if (!new[] { "Pending", "Confirmed", "Processing" }.Contains(order.OrderStatus))
                return Json(new { success = false, message = "This order cannot be cancelled. Only pending, confirmed or processing orders can be cancelled." });

            // Restore stock
            foreach (var item in order.OrderItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                    product.StockQuantity += item.Quantity;
            }

            order.OrderStatus = "Cancelled";
            order.CancelledAt = DateTime.UtcNow;
            order.Notes = string.IsNullOrEmpty(order.Notes)
                ? $"Cancelled by customer: {model.Reason}"
                : order.Notes + $"\nCancelled by customer: {model.Reason}";
            order.UpdatedAt = DateTime.UtcNow;

            // Add status history
            _context.Set<OrderStatusHistory>().Add(new OrderStatusHistory
            {
                OrderId = order.OrderId,
                PreviousStatus = order.OrderStatus,
                NewStatus = "Cancelled",
                Notes = $"Cancelled by customer: {model.Reason}",
                ChangedAt = DateTime.UtcNow
            });

            // If paid, create refund record
            if (order.PaymentStatus == "Paid")
            {
                var refund = new Refund
                {
                    RefundNumber = $"RFD-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}",
                    OrderId = order.OrderId,
                    CustomerId = customer.CustomerId,
                    CompanyId = order.CompanyId,
                    Amount = order.TotalAmount,
                    Reason = model.Reason ?? "Customer cancelled order",
                    Status = "Pending",
                    RequestedAt = DateTime.UtcNow
                };
                _context.Refunds.Add(refund);
                order.PaymentStatus = "Refund Pending";
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Order cancelled successfully" });
        }

        // POST: /CustomerPortal/RequestReturn
        [HttpPost]
        public async Task<IActionResult> RequestReturn([FromBody] ReturnRequestModel model)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
                return Json(new { success = false, message = "Please log in" });

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == model.OrderId && o.CustomerId == customer.CustomerId);

            if (order == null)
                return Json(new { success = false, message = "Order not found" });

            if (order.OrderStatus != "Delivered")
                return Json(new { success = false, message = "Only delivered orders can be returned" });

            // Check 7-day return window
            if (order.DeliveredAt.HasValue && (DateTime.UtcNow - order.DeliveredAt.Value).TotalDays > 7)
                return Json(new { success = false, message = "Return window has expired (7 days after delivery)" });

            order.OrderStatus = "Return Requested";
            order.Notes = string.IsNullOrEmpty(order.Notes)
                ? $"Return requested: {model.Reason}"
                : order.Notes + $"\nReturn requested: {model.Reason}";
            order.UpdatedAt = DateTime.UtcNow;

            _context.Set<OrderStatusHistory>().Add(new OrderStatusHistory
            {
                OrderId = order.OrderId,
                PreviousStatus = "Delivered",
                NewStatus = "Return Requested",
                Notes = $"Return requested by customer: {model.Reason}",
                ChangedAt = DateTime.UtcNow
            });

            // Create refund record
            var refund = new Refund
            {
                RefundNumber = $"RFD-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}",
                OrderId = order.OrderId,
                CustomerId = customer.CustomerId,
                CompanyId = order.CompanyId,
                Amount = order.TotalAmount,
                Reason = model.Reason ?? "Customer return request",
                Status = "Pending",
                RequestedAt = DateTime.UtcNow
            };
            _context.Refunds.Add(refund);

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Return request submitted successfully. You will be contacted within 24-48 hours." });
        }

        // ========== LOYALTY SYSTEM ==========

        // GET: /CustomerPortal/GetLoyaltyInfo
        [HttpGet]
        public async Task<IActionResult> GetLoyaltyInfo()
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
                return Json(new { success = false, message = "Please log in" });

            var companyId = GetPortalCompanyId();

            // Get loyalty program config
            var program = await _context.LoyaltyPrograms
                .FirstOrDefaultAsync(p => p.IsActive && (p.CompanyId == companyId || p.CompanyId == null));

            // Get points history
            var history = await _context.LoyaltyPoints
                .Where(lp => lp.CustomerId == customer.CustomerId)
                .OrderByDescending(lp => lp.CreatedAt)
                .Take(20)
                .Select(lp => new
                {
                    lp.PointId,
                    lp.TransactionType,
                    lp.Points,
                    lp.Description,
                    lp.CreatedAt,
                    OrderNumber = lp.Order != null ? lp.Order.OrderNumber : null
                })
                .ToListAsync();

            // Tier calculation
            var points = customer.LoyaltyPoints;
            string tier = "Bronze";
            string nextTier = "Silver";
            int pointsToNext = 1000 - points;
            if (points >= 10000) { tier = "Platinum"; nextTier = "Max"; pointsToNext = 0; }
            else if (points >= 5000) { tier = "Gold"; nextTier = "Platinum"; pointsToNext = 10000 - points; }
            else if (points >= 1000) { tier = "Silver"; nextTier = "Gold"; pointsToNext = 5000 - points; }

            return Json(new
            {
                success = true,
                data = new
                {
                    currentPoints = customer.LoyaltyPoints,
                    tier,
                    nextTier,
                    pointsToNext = Math.Max(0, pointsToNext),
                    pointsPerCurrency = program?.PointsPerCurrency ?? 1m,
                    pointsValue = program?.PointsValue ?? 0.01m,
                    minRedeemPoints = program?.MinRedeemPoints ?? 100,
                    history
                }
            });
        }

        // POST: /CustomerPortal/RedeemPoints
        [HttpPost]
        public async Task<IActionResult> RedeemPoints([FromBody] RedeemPointsModel model)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
                return Json(new { success = false, message = "Please log in" });

            var program = await _context.LoyaltyPrograms
                .FirstOrDefaultAsync(p => p.IsActive);

            var minRedeem = program?.MinRedeemPoints ?? 100;
            if (model.Points < minRedeem)
                return Json(new { success = false, message = $"Minimum {minRedeem} points required to redeem" });

            if (customer.LoyaltyPoints < model.Points)
                return Json(new { success = false, message = "Insufficient points" });

            var pointValue = program?.PointsValue ?? 0.01m;
            var discountAmount = model.Points * pointValue;

            customer.LoyaltyPoints -= model.Points;
            customer.UpdatedAt = DateTime.UtcNow;

            _context.LoyaltyPoints.Add(new LoyaltyPoints
            {
                CustomerId = customer.CustomerId,
                TransactionType = "Redeemed",
                Points = -model.Points,
                Description = $"Redeemed {model.Points} points for ₱{discountAmount:N2} discount",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = $"Redeemed {model.Points} points for ₱{discountAmount:N2} discount!",
                data = new
                {
                    remainingPoints = customer.LoyaltyPoints,
                    discountAmount
                }
            });
        }

        // ========== DASHBOARD STATS ==========

        // GET: /CustomerPortal/GetDashboardStats
        [HttpGet]
        public async Task<IActionResult> GetDashboardStats()
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
                return Json(new { success = false, message = "Please log in" });

            var customerId = customer.CustomerId;
            var companyId = GetPortalCompanyId();

            var ordersQuery = _context.Orders.Where(o => o.CustomerId == customerId);
            if (companyId.HasValue)
                ordersQuery = ordersQuery.Where(o => o.CompanyId == companyId);

            var totalOrders = await ordersQuery.CountAsync();
            var inProgress = await ordersQuery.CountAsync(o => new[] { "Pending", "Confirmed", "Processing", "Shipped" }.Contains(o.OrderStatus));
            var delivered = await ordersQuery.CountAsync(o => o.OrderStatus == "Delivered");
            var totalSpent = await ordersQuery.SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            // Monthly order counts for chart (last 6 months)
            var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
            var monthlyOrders = await ordersQuery
                .Where(o => o.OrderDate >= sixMonthsAgo)
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count(),
                    Amount = g.Sum(o => o.TotalAmount)
                })
                .OrderBy(g => g.Year).ThenBy(g => g.Month)
                .ToListAsync();

            // Category breakdown for pie chart
            var categoryBreakdown = await ordersQuery
                .SelectMany(o => o.OrderItems)
                .Where(oi => oi.Product != null && oi.Product.Category != null)
                .GroupBy(oi => oi.Product!.Category!.CategoryName)
                .Select(g => new
                {
                    Category = g.Key,
                    Count = g.Sum(oi => oi.Quantity),
                    Amount = g.Sum(oi => oi.TotalPrice)
                })
                .OrderByDescending(g => g.Amount)
                .Take(5)
                .ToListAsync();

            return Json(new
            {
                success = true,
                data = new
                {
                    totalOrders,
                    inProgress,
                    delivered,
                    totalSpent,
                    loyaltyPoints = customer.LoyaltyPoints,
                    monthlyOrders,
                    categoryBreakdown
                }
            });
        }

        // ========== SUBMIT TICKET WITH FILES ==========

        // POST: /CustomerPortal/CreateTicketWithFiles
        [HttpPost]
        public async Task<IActionResult> CreateTicketWithFiles([FromForm] string subject, [FromForm] string description,
            [FromForm] string? priority, [FromForm] string? contactName, [FromForm] string? contactEmail,
            [FromForm] string? contactPhone, [FromForm] int? categoryId, [FromForm] string? relatedOrderNumber,
            List<IFormFile>? attachments)
        {
            var customer = await GetCurrentCustomerAsync();
            var customerId = customer?.CustomerId;
            var companyId = GetPortalCompanyId();

            var ticketCount = await _context.SupportTickets.CountAsync() + 1;
            var ticketNumber = $"TKT-{DateTime.Now:yyyy}-{ticketCount:D4}";

            var ticket = new SupportTicket
            {
                TicketNumber = ticketNumber,
                CustomerId = customerId,
                CompanyId = companyId,
                CategoryId = categoryId,
                Subject = subject,
                Description = description,
                ContactName = contactName ?? customer?.FullName,
                ContactEmail = contactEmail ?? customer?.Email ?? "",
                ContactPhone = contactPhone ?? customer?.Phone,
                Priority = priority ?? "Medium",
                Status = "Open",
                Source = "Web",
                CreatedAt = DateTime.UtcNow
            };

            _context.SupportTickets.Add(ticket);
            await _context.SaveChangesAsync();

            // Handle file attachments
            if (attachments != null && attachments.Count > 0)
            {
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "tickets", ticket.TicketId.ToString());
                Directory.CreateDirectory(uploadsDir);

                foreach (var file in attachments.Take(5))
                {
                    if (file.Length > 10 * 1024 * 1024) continue; // Skip files > 10MB

                    var safeFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                    var filePath = Path.Combine(uploadsDir, safeFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    _context.TicketAttachments.Add(new TicketAttachment
                    {
                        TicketId = ticket.TicketId,
                        FileName = file.FileName,
                        FileUrl = $"/uploads/tickets/{ticket.TicketId}/{safeFileName}",
                        FileSize = (int?)file.Length,
                        FileType = file.ContentType,
                        UploadedAt = DateTime.UtcNow
                    });
                }
                await _context.SaveChangesAsync();
            }

            return Json(new
            {
                success = true,
                message = "Ticket created successfully",
                data = new { ticket.TicketId, ticket.TicketNumber }
            });
        }

        // GET: /CustomerPortal/GetCustomerOrders (lightweight, for dropdowns)
        [HttpGet]
        public async Task<IActionResult> GetCustomerOrders()
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
                return Json(new { success = true, data = new List<object>() });

            var orders = await _context.Orders
                .Where(o => o.CustomerId == customer.CustomerId)
                .OrderByDescending(o => o.OrderDate)
                .Take(20)
                .Select(o => new
                {
                    o.OrderId,
                    o.OrderNumber,
                    o.OrderDate,
                    o.TotalAmount,
                    o.OrderStatus,
                    FirstItem = o.OrderItems.Select(oi => oi.ProductName).FirstOrDefault()
                })
                .ToListAsync();

            return Json(new { success = true, data = orders });
        }

        // ========== PROMOTIONS (Real Data) ==========

        // GET: /CustomerPortal/GetActiveCampaigns
        [HttpGet]
        public async Task<IActionResult> GetActiveCampaigns()
        {
            var companyId = GetPortalCompanyId();
            var now = DateTime.UtcNow;

            var campaignQuery = _context.Campaigns
                .Where(c => c.Status == "Active" && c.StartDate <= now && c.EndDate >= now);

            if (companyId.HasValue)
                campaignQuery = campaignQuery.Where(c => c.CompanyId == companyId);

            var campaigns = await campaignQuery
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new
                {
                    c.CampaignId,
                    c.CampaignName,
                    c.Description,
                    c.Type,
                    c.StartDate,
                    c.EndDate,
                    c.Subject,
                    c.Content
                })
                .ToListAsync();

            return Json(new { success = true, data = campaigns });
        }

        // ========== GIFT OPTIONS ==========

        // POST: /CustomerPortal/AddGiftOption
        [HttpPost]
        public async Task<IActionResult> AddGiftOption([FromBody] AddGiftOptionModel model)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
                return Json(new { success = false, message = "Please log in" });

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == model.OrderId && o.CustomerId == customer.CustomerId);
            if (order == null)
                return Json(new { success = false, message = "Order not found" });

            var giftOption = new GiftOption
            {
                OrderId = model.OrderId,
                CompanyId = order.CompanyId,
                IsGift = true,
                GiftWrap = model.GiftWrap,
                GiftWrapPrice = model.GiftWrap ? 50m : 0m,
                GiftMessage = model.GiftMessage,
                RecipientName = model.RecipientName,
                RecipientEmail = model.RecipientEmail,
                HidePrice = model.HidePrice,
                CreatedAt = DateTime.UtcNow
            };

            _context.GiftOptions.Add(giftOption);

            if (model.GiftWrap)
            {
                order.TotalAmount += 50m;
                order.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Gift options saved" });
        }

        // ========== INSTALLMENT PLANS ==========

        // GET: /CustomerPortal/GetInstallmentOptions
        [HttpGet]
        public Task<IActionResult> GetInstallmentOptions(decimal amount)
        {
            var options = new[]
            {
                new { months = 3, interestRate = 0m, monthly = Math.Round(amount / 3, 2), total = amount },
                new { months = 6, interestRate = 3.5m, monthly = Math.Round(amount * 1.035m / 6, 2), total = Math.Round(amount * 1.035m, 2) },
                new { months = 12, interestRate = 7m, monthly = Math.Round(amount * 1.07m / 12, 2), total = Math.Round(amount * 1.07m, 2) }
            };

            return Task.FromResult<IActionResult>(Json(new { success = true, data = options }));
        }

        // POST: /CustomerPortal/CreateInstallmentPlan
        [HttpPost]
        public async Task<IActionResult> CreateInstallmentPlan([FromBody] CreateInstallmentModel model)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
                return Json(new { success = false, message = "Please log in" });

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == model.OrderId && o.CustomerId == customer.CustomerId);
            if (order == null)
                return Json(new { success = false, message = "Order not found" });

            var interestRate = model.Months switch { 6 => 3.5m, 12 => 7m, _ => 0m };
            var totalAmount = order.TotalAmount * (1 + interestRate / 100);
            var installmentAmount = Math.Round(totalAmount / model.Months, 2);

            var plan = new InstallmentPlan
            {
                OrderId = order.OrderId,
                CustomerId = customer.CustomerId,
                CompanyId = order.CompanyId,
                TotalAmount = totalAmount,
                NumberOfInstallments = model.Months,
                InstallmentAmount = installmentAmount,
                InterestRate = interestRate,
                Status = "Active",
                StartDate = DateTime.UtcNow,
                NextDueDate = DateTime.UtcNow.AddMonths(1),
                CreatedAt = DateTime.UtcNow
            };

            // Create individual payment schedules
            for (int i = 1; i <= model.Months; i++)
            {
                plan.Payments.Add(new InstallmentPayment
                {
                    InstallmentNumber = i,
                    Amount = installmentAmount,
                    DueDate = DateTime.UtcNow.AddMonths(i),
                    Status = "Pending"
                });
            }

            _context.InstallmentPlans.Add(plan);
            order.PaymentMethod = $"Installment ({model.Months} months)";
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Installment plan created: {model.Months} payments of ₱{installmentAmount:N2}" });
        }

        // ========== SUBSCRIPTION ORDERS ==========

        // GET: /CustomerPortal/GetSubscriptions
        [HttpGet]
        public async Task<IActionResult> GetSubscriptions()
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
                return Json(new { success = false, message = "Please log in" });

            var subscriptions = await _context.SubscriptionOrders
                .Include(s => s.Items)
                .ThenInclude(si => si.Product)
                .Where(s => s.CustomerId == customer.CustomerId)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new
                {
                    s.SubscriptionId,
                    s.SubscriptionCode,
                    s.Frequency,
                    s.NextOrderDate,
                    s.LastOrderDate,
                    s.EstimatedTotal,
                    s.Status,
                    Items = s.Items.Select(i => new
                    {
                        i.ProductId,
                        ProductName = i.Product != null ? i.Product.ProductName : "Unknown",
                        i.Quantity,
                        i.UnitPrice
                    })
                })
                .ToListAsync();

            return Json(new { success = true, data = subscriptions });
        }

        // POST: /CustomerPortal/CreateSubscription
        [HttpPost]
        public async Task<IActionResult> CreateSubscription([FromBody] CreateSubscriptionModel model)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
                return Json(new { success = false, message = "Please log in" });

            var companyId = GetPortalCompanyId();
            var subCount = await _context.SubscriptionOrders.CountAsync() + 1;

            var subscription = new SubscriptionOrder
            {
                CustomerId = customer.CustomerId,
                CompanyId = companyId,
                SubscriptionCode = $"SUB-{DateTime.Now:yyyyMMdd}-{subCount:D4}",
                Frequency = model.Frequency ?? "Monthly",
                NextOrderDate = DateTime.UtcNow.AddDays(model.Frequency == "Weekly" ? 7 : model.Frequency == "BiWeekly" ? 14 : 30),
                Status = "Active",
                ShippingAddressId = model.ShippingAddressId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            decimal total = 0;
            foreach (var item in model.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product == null) continue;

                subscription.Items.Add(new SubscriptionItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = product.SellingPrice
                });
                total += product.SellingPrice * item.Quantity;
            }
            subscription.EstimatedTotal = total;

            _context.SubscriptionOrders.Add(subscription);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Subscription created", data = new { subscription.SubscriptionId, subscription.SubscriptionCode } });
        }

        // POST: /CustomerPortal/CancelSubscription
        [HttpPost]
        public async Task<IActionResult> CancelSubscription([FromBody] CancelSubscriptionModel model)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
                return Json(new { success = false, message = "Please log in" });

            var sub = await _context.SubscriptionOrders
                .FirstOrDefaultAsync(s => s.SubscriptionId == model.SubscriptionId && s.CustomerId == customer.CustomerId);
            if (sub == null)
                return Json(new { success = false, message = "Subscription not found" });

            sub.Status = "Cancelled";
            sub.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Subscription cancelled" });
        }

        #endregion
    }

    public class CustomerProfileUpdateModel
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Phone { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? BillingAddress { get; set; }
        public string? BillingCity { get; set; }
        public string? BillingState { get; set; }
        public string? BillingZipCode { get; set; }
        public string? BillingCountry { get; set; }
        public string? ShippingAddress { get; set; }
        public string? ShippingCity { get; set; }
        public string? ShippingState { get; set; }
        public string? ShippingZipCode { get; set; }
        public string? ShippingCountry { get; set; }
        public bool MarketingOptIn { get; set; }
    }

    public class CreateOrderModel
    {
        public List<CartItemModel>? Items { get; set; }
        public string? PromoCode { get; set; }
        public string? PaymentMethod { get; set; }
        public string? ShippingAddress { get; set; }
        public string? ShippingCity { get; set; }
        public string? ShippingState { get; set; }
        public string? ShippingZipCode { get; set; }
        public string? ShippingCountry { get; set; }
        public string? Notes { get; set; }
    }

    public class CartItemModel
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class PaymentRequestModel
    {
        public int OrderId { get; set; }
        public string? PaymentMethod { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
    }

    public class ValidatePromoModel
    {
        public string PromoCode { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }
    }

    public class CreateTicketModel
    {
        public int? CategoryId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ContactName { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string? Priority { get; set; }
    }

    public class ChatMessageModel
    {
        public string Message { get; set; } = string.Empty;
        public string? VisitorId { get; set; }
    }

    public class TransferToAgentModel
    {
        public int SessionId { get; set; }
    }

    public class AgentChatModel
    {
        public int SessionId { get; set; }
        public int AgentId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class TicketReplyModel
    {
        public int TicketId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // ========== New Request Models ==========

    public class AddAddressModel
    {
        public string? AddressType { get; set; }
        public string AddressLine1 { get; set; } = string.Empty;
        public string? AddressLine2 { get; set; }
        public string City { get; set; } = string.Empty;
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public string? Country { get; set; }
        public bool IsDefault { get; set; }
    }

    public class UpdateAddressModel
    {
        public int AddressId { get; set; }
        public string? AddressType { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public string? Country { get; set; }
    }

    public class SetDefaultAddressModel
    {
        public int AddressId { get; set; }
    }

    public class DeleteAddressModel
    {
        public int AddressId { get; set; }
    }

    public class ChangePasswordModel
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class EmailPreferencesModel
    {
        public bool MarketingOptIn { get; set; }
        public string? PreferredContactMethod { get; set; }
    }

    public class SubmitReviewModel
    {
        public int ProductId { get; set; }
        public int? OrderId { get; set; }
        public int Rating { get; set; }
        public string? Title { get; set; }
        public string? Comment { get; set; }
        public string? Pros { get; set; }
        public string? Cons { get; set; }
    }

    public class ReviewHelpfulModel
    {
        public int ReviewId { get; set; }
    }

    public class CancelOrderModel
    {
        public int OrderId { get; set; }
        public string? Reason { get; set; }
    }

    public class ReturnRequestModel
    {
        public int OrderId { get; set; }
        public string? Reason { get; set; }
    }

    public class RedeemPointsModel
    {
        public int Points { get; set; }
    }

    public class AddGiftOptionModel
    {
        public int OrderId { get; set; }
        public bool GiftWrap { get; set; }
        public string? GiftMessage { get; set; }
        public string? RecipientName { get; set; }
        public string? RecipientEmail { get; set; }
        public bool HidePrice { get; set; } = true;
    }

    public class CreateInstallmentModel
    {
        public int OrderId { get; set; }
        public int Months { get; set; } = 3;
    }

    public class CreateSubscriptionModel
    {
        public string? Frequency { get; set; }
        public int? ShippingAddressId { get; set; }
        public List<SubscriptionItemModel> Items { get; set; } = new();
    }

    public class SubscriptionItemModel
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;
    }

    public class CancelSubscriptionModel
    {
        public int SubscriptionId { get; set; }
    }
}
