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
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                return await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
            }
            return null;
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
                // For demo, get first customer
                customer = await _context.Customers
                    .Include(c => c.Category)
                    .Include(c => c.Addresses)
                    .FirstOrDefaultAsync();
            }
            else
            {
                customer = await _context.Customers
                    .Include(c => c.Category)
                    .Include(c => c.Addresses)
                    .FirstOrDefaultAsync(c => c.CustomerId == customer.CustomerId);
            }

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
            {
                customer = await _context.Customers.FirstOrDefaultAsync();
            }

            if (customer == null)
                return Json(new { success = false, message = "Customer not found" });

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
        public async Task<IActionResult> GetProducts(int page = 1, int pageSize = 12, int? categoryId = null, int? brandId = null, string? search = null, string? sort = "featured", bool? onSale = null)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Where(p => p.Status == "Active");

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId);

            if (brandId.HasValue)
                query = query.Where(p => p.BrandId == brandId);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => p.ProductName.Contains(search) || p.ShortDescription!.Contains(search));

            if (onSale == true)
                query = query.Where(p => p.IsOnSale);

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
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Where(p => p.Status == "Active" && p.IsFeatured)
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
                products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Where(p => p.Status == "Active")
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
            var now = DateTime.UtcNow;
            var promotions = await _context.Promotions
                .Include(p => p.Campaign)
                .Where(p => p.IsActive && p.StartDate <= now && p.EndDate >= now)
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
            var campaigns = await _context.Campaigns
                .Where(c => c.Status == "Active" && c.StartDate <= now && c.EndDate >= now)
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
            var customerId = customer?.CustomerId ?? 1; // Demo fallback

            var query = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.CustomerId == customerId);

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
            var customerId = customer?.CustomerId ?? 1;

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

        // POST: /CustomerPortal/ProcessPayment (PayMongo Payment Intent + GCash)
        [HttpPost]
        public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequestModel model)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == model.OrderId);

            if (order == null)
                return Json(new { success = false, message = "Order not found" });

            var paymongoSecretKey = _configuration["PayMongo:SecretKey"] ?? "sk_test_SakyRyg4R6hXeni4x5EaNUow";

            try
            {
                using var httpClient = new HttpClient();
                var authToken = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{paymongoSecretKey}:"));
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {authToken}");

                // Step 1: Create Payment Intent
                var amountInCentavos = (int)(order.TotalAmount * 100);
                var paymentIntentRequest = new
                {
                    data = new
                    {
                        attributes = new
                        {
                            amount = amountInCentavos,
                            currency = "PHP",
                            payment_method_allowed = new[] { model.PaymentMethod ?? "gcash" },
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

                if (!piResponse.IsSuccessStatusCode)
                {
                    return Json(new { success = false, message = "Failed to create payment intent", details = piResponseContent });
                }

                var piJson = System.Text.Json.JsonDocument.Parse(piResponseContent);
                var paymentIntentId = piJson.RootElement.GetProperty("data").GetProperty("id").GetString();
                var clientKey = piJson.RootElement.GetProperty("data").GetProperty("attributes").GetProperty("client_key").GetString();

                // Step 2: Create Payment Method
                var paymentMethodRequest = new
                {
                    data = new
                    {
                        attributes = new
                        {
                            type = model.PaymentMethod ?? "gcash",
                            billing = new
                            {
                                name = model.CustomerName ?? "Customer",
                                email = model.CustomerEmail ?? "",
                                phone = model.CustomerPhone ?? ""
                            }
                        }
                    }
                };

                var pmContent = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(paymentMethodRequest),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var pmResponse = await httpClient.PostAsync("https://api.paymongo.com/v1/payment_methods", pmContent);
                var pmResponseContent = await pmResponse.Content.ReadAsStringAsync();

                if (!pmResponse.IsSuccessStatusCode)
                {
                    return Json(new { success = false, message = "Failed to create payment method", details = pmResponseContent });
                }

                var pmJson = System.Text.Json.JsonDocument.Parse(pmResponseContent);
                var paymentMethodId = pmJson.RootElement.GetProperty("data").GetProperty("id").GetString();

                // Step 3: Attach Payment Method to Payment Intent
                var attachRequest = new
                {
                    data = new
                    {
                        attributes = new
                        {
                            payment_method = paymentMethodId,
                            client_key = clientKey,
                            return_url = $"{Request.Scheme}://{Request.Host}/CustomerPortal/PaymentCallback?orderId={order.OrderId}"
                        }
                    }
                };

                var attachContent = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(attachRequest),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var attachResponse = await httpClient.PostAsync($"https://api.paymongo.com/v1/payment_intents/{paymentIntentId}/attach", attachContent);
                var attachResponseContent = await attachResponse.Content.ReadAsStringAsync();

                if (!attachResponse.IsSuccessStatusCode)
                {
                    return Json(new { success = false, message = "Failed to attach payment method", details = attachResponseContent });
                }

                var attachJson = System.Text.Json.JsonDocument.Parse(attachResponseContent);
                var status = attachJson.RootElement.GetProperty("data").GetProperty("attributes").GetProperty("status").GetString();

                // Save payment reference
                order.PaymentReference = paymentIntentId;
                order.PaymentMethod = model.PaymentMethod ?? "gcash";
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Check if redirect is needed (for e-wallets like GCash)
                string? checkoutUrl = null;
                if (status == "awaiting_next_action")
                {
                    var nextAction = attachJson.RootElement.GetProperty("data").GetProperty("attributes").GetProperty("next_action");
                    checkoutUrl = nextAction.GetProperty("redirect").GetProperty("url").GetString();
                }

                return Json(new
                {
                    success = true,
                    paymentIntentId,
                    clientKey,
                    status,
                    checkoutUrl,
                    message = status == "succeeded" ? "Payment successful!" : "Redirecting to payment..."
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Payment error: " + ex.Message });
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
            var order = await _context.Orders.FindAsync(orderId);
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
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Where(p => productIds.Contains(p.ProductId))
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
            var promo = await _context.Promotions
                .FirstOrDefaultAsync(p => p.PromotionCode.ToLower() == model.PromoCode.ToLower() && p.IsActive);

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
            var customerId = customer?.CustomerId ?? 1;

            var tickets = await _context.SupportTickets
                .Include(t => t.Category)
                .Include(t => t.AssignedUser)
                .Where(t => t.CustomerId == customerId)
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
            var customerId = customer?.CustomerId ?? 1;

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
            if (ticket == null || ticket.CustomerId != (customer?.CustomerId ?? 1))
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

            var ticketCount = await _context.SupportTickets.CountAsync() + 1;
            var ticketNumber = $"TKT-{DateTime.Now:yyyy}-{ticketCount:D4}";

            var ticket = new SupportTicket
            {
                TicketNumber = ticketNumber,
                CustomerId = customerId,
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

            // Find or create chat session
            var session = await _context.ChatSessions
                .FirstOrDefaultAsync(s => s.CustomerId == customerId && s.Status == "Active");

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

            // Find available support agent
            var supportAgent = await _context.Users
                .Where(u => u.RoleId == 4 && u.IsActive) // Customer Support Staff
                .FirstOrDefaultAsync();

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
            var chats = await _context.ChatSessions
                .Include(s => s.Customer)
                .Where(s => s.Status == "Active" || s.Status == "Transferred")
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
    }

    // Request Models

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
}
