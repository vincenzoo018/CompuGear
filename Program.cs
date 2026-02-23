using Microsoft.EntityFrameworkCore;
using CompuGear.Models;
using CompuGear.Data;
using CompuGear.Services;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        // Use camelCase for JSON property names (JavaScript convention)
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        // Suppress automatic 400 response for invalid model states
        // This allows us to handle validation errors in the controllers
        options.SuppressModelStateInvalidFilter = true;
    });

// Add Entity Framework DbContext with retry policy and performance optimizations
builder.Services.AddDbContext<CompuGearDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null);
        sqlOptions.CommandTimeout(30);
    })
    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

// Add HttpContextAccessor for services that need HTTP context
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();

// Register Audit Service
builder.Services.AddScoped<IAuditService, AuditService>();

// Register PayMongo Payment Service
builder.Services.AddHttpClient<IPayMongoService, PayMongoService>();

// Add session support (for user sessions)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Seed test users and sample data on startup in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<CompuGearDbContext>();
    await SeedTestUsersAsync(context);
    await SeedSampleDataAsync(context);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthorization();
app.UseMiddleware<ActivityAuditMiddleware>();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();

// Seed test users for development
async Task SeedTestUsersAsync(CompuGearDbContext context)
{
    try
    {
        var salt = "CompuGearSalt2024";
        var passwordHash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes("password123" + salt)));

        var usersToSeed = new List<(string username, string email, string firstName, string lastName, int roleId)>
        {
            ("admin", "admin@compugear.com", "System", "Administrator", 1),
            ("company.admin", "companyadmin@compugear.com", "Company", "Admin", 2),
            ("sarah.johnson", "sales@compugear.com", "Sarah", "Johnson", 3),
            ("mike.chen", "support@compugear.com", "Mike", "Chen", 4),
            ("emily.brown", "marketing@compugear.com", "Emily", "Brown", 5),
            ("john.doe", "billing@compugear.com", "John", "Doe", 6),
            ("james.wilson", "inventory@compugear.com", "James", "Wilson", 8)  // RoleId 8 = Inventory Staff
        };

        foreach (var (username, email, firstName, lastName, roleId) in usersToSeed)
        {
            // Check by email first
            var existingUser = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
            
            if (existingUser != null)
            {
                // Update existing user with correct hash and role
                existingUser.PasswordHash = passwordHash;
                existingUser.Salt = salt;
                existingUser.RoleId = roleId;
                existingUser.IsActive = true;
                existingUser.IsEmailVerified = true;
                existingUser.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Check if username already exists
                var existingByUsername = await context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (existingByUsername != null)
                {
                    // Update existing user with correct email and hash
                    existingByUsername.Email = email;
                    existingByUsername.PasswordHash = passwordHash;
                    existingByUsername.Salt = salt;
                    existingByUsername.RoleId = roleId;
                    existingByUsername.IsActive = true;
                    existingByUsername.IsEmailVerified = true;
                    existingByUsername.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    // Create new user
                    context.Users.Add(new User
                    {
                        Username = username,
                        Email = email,
                        PasswordHash = passwordHash,
                        Salt = salt,
                        FirstName = firstName,
                        LastName = lastName,
                        RoleId = roleId,
                        CompanyId = 1,
                        IsActive = true,
                        IsEmailVerified = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }
        }

        await context.SaveChangesAsync();
        Console.WriteLine("✓ Test users seeded successfully!");
        Console.WriteLine("  Login credentials: [email]@compugear.com / password123");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠ User seeding skipped: {ex.Message}");
        // Don't throw - just log and continue
    }
}

// Seed comprehensive sample data for development/demo
async Task SeedSampleDataAsync(CompuGearDbContext context)
{
    try
    {
        // Skip if orders already exist (data already seeded)
        var hasOrders = await context.Orders.AnyAsync();
        if (hasOrders)
        {
            Console.WriteLine("✓ Sample data already exists - skipping seed.");
            return;
        }

        Console.WriteLine("Seeding sample data...");

        // 1. Seed Product Categories
        var categories = new List<ProductCategory>();
        var categoryNames = new[] { "Processors", "Graphics Cards", "Motherboards", "Memory", "Storage", "Peripherals", "Monitors", "Cases & PSU" };
        foreach (var name in categoryNames)
        {
            var existing = await context.ProductCategories.FirstOrDefaultAsync(c => c.CategoryName == name);
            if (existing == null)
            {
                existing = new ProductCategory { CategoryName = name, Description = $"{name} category", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
                context.ProductCategories.Add(existing);
            }
            categories.Add(existing);
        }
        await context.SaveChangesAsync();
        // Re-fetch to get IDs
        categories = await context.ProductCategories.ToListAsync();

        // 2. Seed Brands
        var brandNames = new[] { "Intel", "AMD", "NVIDIA", "ASUS", "Corsair", "Samsung", "Logitech", "Razer" };
        foreach (var name in brandNames)
        {
            var existing = await context.Brands.FirstOrDefaultAsync(b => b.BrandName == name);
            if (existing == null)
            {
                context.Brands.Add(new Brand { BrandName = name, IsActive = true, CreatedAt = DateTime.UtcNow });
            }
        }
        await context.SaveChangesAsync();
        var brands = await context.Brands.ToListAsync();

        // 3. Seed Products
        var productData = new[]
        {
            new { Code = "PRD-001", Name = "Intel Core i7-13700K", Desc = "13th Gen Intel processor, 16 cores, 5.4GHz boost", Cat = "Processors", Brand = "Intel", Cost = 18000m, Price = 22500m, Stock = 45 },
            new { Code = "PRD-002", Name = "AMD Ryzen 9 7950X", Desc = "16-core, 32-thread processor, 5.7GHz boost", Cat = "Processors", Brand = "AMD", Cost = 25000m, Price = 32000m, Stock = 30 },
            new { Code = "PRD-003", Name = "NVIDIA GeForce RTX 4090", Desc = "Top-tier gaming GPU, 24GB GDDR6X", Cat = "Graphics Cards", Brand = "NVIDIA", Cost = 95000m, Price = 120000m, Stock = 12 },
            new { Code = "PRD-004", Name = "ASUS ROG Maximus Z790", Desc = "Premium DDR5 motherboard for Intel", Cat = "Motherboards", Brand = "ASUS", Cost = 25000m, Price = 32000m, Stock = 20 },
            new { Code = "PRD-005", Name = "Corsair Vengeance 32GB DDR5", Desc = "DDR5-5600 dual-channel memory kit", Cat = "Memory", Brand = "Corsair", Cost = 8500m, Price = 11500m, Stock = 60 },
            new { Code = "PRD-006", Name = "Samsung 990 Pro 1TB NVMe", Desc = "PCIe 4.0 NVMe M.2 SSD, 7450MB/s read", Cat = "Storage", Brand = "Samsung", Cost = 5500m, Price = 7500m, Stock = 80 },
            new { Code = "PRD-007", Name = "Logitech G502 X Plus", Desc = "Wireless gaming mouse, HERO 25K sensor", Cat = "Peripherals", Brand = "Logitech", Cost = 4500m, Price = 6500m, Stock = 100 },
            new { Code = "PRD-008", Name = "Razer BlackWidow V4 Pro", Desc = "Mechanical gaming keyboard, RGB", Cat = "Peripherals", Brand = "Razer", Cost = 8000m, Price = 11000m, Stock = 55 },
            new { Code = "PRD-009", Name = "ASUS ROG Swift PG27AQN", Desc = "27-inch 1440p 360Hz gaming monitor", Cat = "Monitors", Brand = "ASUS", Cost = 45000m, Price = 58000m, Stock = 15 },
            new { Code = "PRD-010", Name = "Corsair 5000D Airflow", Desc = "Mid-tower ATX case, excellent airflow", Cat = "Cases & PSU", Brand = "Corsair", Cost = 6500m, Price = 8500m, Stock = 40 }
        };

        foreach (var p in productData)
        {
            var existing = await context.Products.FirstOrDefaultAsync(x => x.ProductCode == p.Code);
            if (existing == null)
            {
                var cat = categories.FirstOrDefault(c => c.CategoryName == p.Cat);
                var brand = brands.FirstOrDefault(b => b.BrandName == p.Brand);
                context.Products.Add(new Product
                {
                    CompanyId = 1,
                    ProductCode = p.Code,
                    SKU = p.Code.Replace("PRD", "SKU"),
                    ProductName = p.Name,
                    ShortDescription = p.Desc,
                    CategoryId = cat?.CategoryId,
                    BrandId = brand?.BrandId,
                    CostPrice = p.Cost,
                    SellingPrice = p.Price,
                    StockQuantity = p.Stock,
                    ReorderLevel = 10,
                    Status = "Active",
                    IsFeatured = p.Price > 30000m,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }
        await context.SaveChangesAsync();
        var products = await context.Products.ToListAsync();

        // 4. Seed Customer Categories
        var custCatNames = new[] { "Regular", "VIP", "Corporate", "Wholesale", "Student" };
        foreach (var name in custCatNames)
        {
            var existing = await context.CustomerCategories.FirstOrDefaultAsync(c => c.CategoryName == name);
            if (existing == null)
            {
                context.CustomerCategories.Add(new CustomerCategory
                {
                    CategoryName = name,
                    Description = $"{name} customer tier",
                    DiscountPercent = name == "VIP" ? 10 : name == "Corporate" ? 15 : name == "Wholesale" ? 20 : name == "Student" ? 5 : 0,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        await context.SaveChangesAsync();
        var custCategories = await context.CustomerCategories.ToListAsync();

        // 5. Seed Customers
        var customerData = new[]
        {
            new { Code = "CUST-001", First = "Maria", Last = "Santos", Email = "maria.santos@email.com", Phone = "+63-917-111-0001", City = "Manila", Cat = "VIP" },
            new { Code = "CUST-002", First = "Juan", Last = "Dela Cruz", Email = "juan.delacruz@email.com", Phone = "+63-918-222-0002", City = "Quezon City", Cat = "Regular" },
            new { Code = "CUST-003", First = "Ana", Last = "Reyes", Email = "ana.reyes@email.com", Phone = "+63-919-333-0003", City = "Makati", Cat = "Corporate" },
            new { Code = "CUST-004", First = "Carlos", Last = "Garcia", Email = "carlos.garcia@techcorp.ph", Phone = "+63-920-444-0004", City = "Taguig", Cat = "Corporate" },
            new { Code = "CUST-005", First = "Pedro", Last = "Lim", Email = "pedro.lim@email.com", Phone = "+63-921-555-0005", City = "Cebu", Cat = "Regular" }
        };

        foreach (var c in customerData)
        {
            var existing = await context.Customers.FirstOrDefaultAsync(x => x.Email == c.Email);
            if (existing == null)
            {
                var cat = custCategories.FirstOrDefault(cc => cc.CategoryName == c.Cat);
                context.Customers.Add(new Customer
                {
                    CompanyId = 1,
                    CustomerCode = c.Code,
                    FirstName = c.First,
                    LastName = c.Last,
                    Email = c.Email,
                    Phone = c.Phone,
                    BillingCity = c.City,
                    BillingCountry = "Philippines",
                    ShippingCity = c.City,
                    ShippingCountry = "Philippines",
                    CategoryId = cat?.CategoryId,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }
        await context.SaveChangesAsync();
        var customers = await context.Customers.ToListAsync();

        // 6. Seed Orders with Order Items
        var p001 = products.FirstOrDefault(p => p.ProductCode == "PRD-001");
        var p003 = products.FirstOrDefault(p => p.ProductCode == "PRD-003");
        var p004 = products.FirstOrDefault(p => p.ProductCode == "PRD-004");
        var p005 = products.FirstOrDefault(p => p.ProductCode == "PRD-005");
        var p006 = products.FirstOrDefault(p => p.ProductCode == "PRD-006");
        var p007 = products.FirstOrDefault(p => p.ProductCode == "PRD-007");
        var p008 = products.FirstOrDefault(p => p.ProductCode == "PRD-008");
        var p009 = products.FirstOrDefault(p => p.ProductCode == "PRD-009");
        var p010 = products.FirstOrDefault(p => p.ProductCode == "PRD-010");

        var cust1 = customers.FirstOrDefault(c => c.CustomerCode == "CUST-001");
        var cust2 = customers.FirstOrDefault(c => c.CustomerCode == "CUST-002");
        var cust3 = customers.FirstOrDefault(c => c.CustomerCode == "CUST-003");
        var cust4 = customers.FirstOrDefault(c => c.CustomerCode == "CUST-004");
        var cust5 = customers.FirstOrDefault(c => c.CustomerCode == "CUST-005");

        var now = DateTime.UtcNow;

        var ordersToSeed = new[]
        {
            new {
                Number = "ORD-2026-0001", Cust = cust1, Date = now.AddDays(-45),
                Status = "Delivered", PayStatus = "Paid", Method = "Credit Card",
                Items = new[] { (p001, 1, 22500m), (p005, 1, 11500m) }
            },
            new {
                Number = "ORD-2026-0002", Cust = cust2, Date = now.AddDays(-30),
                Status = "Completed", PayStatus = "Paid", Method = "Bank Transfer",
                Items = new[] { (p003, 1, 120000m), (p004, 1, 32000m), (p005, 1, 11500m) }
            },
            new {
                Number = "ORD-2026-0003", Cust = cust3, Date = now.AddDays(-20),
                Status = "Confirmed", PayStatus = "Pending", Method = "GCash",
                Items = new[] { (p006, 2, 7500m) }
            },
            new {
                Number = "ORD-2026-0004", Cust = cust4, Date = now.AddDays(-15),
                Status = "Processing", PayStatus = "Paid", Method = "Bank Transfer",
                Items = new[] { (p001, 5, 22500m), (p007, 5, 6500m) }
            },
            new {
                Number = "ORD-2026-0005", Cust = cust5, Date = now.AddDays(-10),
                Status = "Pending", PayStatus = "Pending", Method = "COD",
                Items = new[] { (p007, 1, 6500m), (p008, 1, 11000m) }
            },
            new {
                Number = "ORD-2026-0006", Cust = cust1, Date = now.AddDays(-7),
                Status = "Confirmed", PayStatus = "Paid", Method = "Credit Card",
                Items = new[] { (p009, 1, 58000m), (p010, 1, 8500m) }
            },
            new {
                Number = "ORD-2026-0007", Cust = cust3, Date = now.AddDays(-3),
                Status = "Pending", PayStatus = "Pending", Method = "GCash",
                Items = new[] { (p008, 2, 11000m), (p006, 1, 7500m) }
            },
            new {
                Number = "ORD-2026-0008", Cust = cust2, Date = now.AddDays(-1),
                Status = "Pending", PayStatus = "Pending", Method = "Bank Transfer",
                Items = new[] { (p003, 1, 120000m) }
            }
        };

        foreach (var od in ordersToSeed)
        {
            if (od.Cust == null) continue;

            var subtotal = od.Items.Sum(i => i.Item3 * i.Item2);
            var tax = Math.Round(subtotal * 0.12m, 2);
            var total = subtotal + tax;
            var paid = od.PayStatus == "Paid" ? total : 0m;

            var order = new Order
            {
                CompanyId = 1,
                OrderNumber = od.Number,
                CustomerId = od.Cust.CustomerId,
                OrderDate = od.Date,
                OrderStatus = od.Status,
                PaymentStatus = od.PayStatus,
                Subtotal = subtotal,
                TaxAmount = tax,
                TotalAmount = total,
                PaidAmount = paid,
                PaymentMethod = od.Method,
                ShippingAddress = od.Cust.BillingAddress ?? "123 Main St",
                ShippingCity = od.Cust.ShippingCity ?? "Manila",
                ShippingCountry = "Philippines",
                ConfirmedAt = od.Status != "Pending" ? od.Date.AddHours(2) : null,
                ShippedAt = od.Status == "Shipped" || od.Status == "Delivered" ? od.Date.AddDays(2) : null,
                DeliveredAt = od.Status == "Delivered" ? od.Date.AddDays(5) : null,
                CreatedAt = od.Date,
                UpdatedAt = now
            };

            context.Orders.Add(order);
            await context.SaveChangesAsync(); // Save to get OrderId

            foreach (var (prod, qty, price) in od.Items)
            {
                if (prod == null) continue;
                context.OrderItems.Add(new OrderItem
                {
                    OrderId = order.OrderId,
                    ProductId = prod.ProductId,
                    ProductName = prod.ProductName,
                    ProductCode = prod.ProductCode,
                    Quantity = qty,
                    UnitPrice = price,
                    TotalPrice = price * qty
                });
            }
            await context.SaveChangesAsync();
        }

        // 7. Seed a few Leads
        var hasLeads = await context.Leads.AnyAsync();
        if (!hasLeads)
        {
            var leadData = new[]
            {
                new Lead { CompanyId = 1, LeadCode = "LEAD-001", FirstName = "Roberto", LastName = "Villanueva", Email = "roberto.v@enterprise.ph", Phone = "+63-917-111-2233", CompanyName = "Enterprise Solutions PH", Source = "Website", Status = "Qualified", Priority = "High", EstimatedValue = 150000m, CreatedAt = now.AddDays(-20) },
                new Lead { CompanyId = 1, LeadCode = "LEAD-002", FirstName = "Carmela", LastName = "Diaz", Email = "carmela.d@startupco.com", Phone = "+63-918-222-3344", CompanyName = "StartupCo", Source = "Referral", Status = "Proposal", Priority = "Medium", EstimatedValue = 85000m, CreatedAt = now.AddDays(-15) },
                new Lead { CompanyId = 1, LeadCode = "LEAD-003", FirstName = "Dennis", LastName = "Lim", Email = "dennis.l@schoolsph.edu", Phone = "+63-919-333-4455", CompanyName = "Schools PH Academy", Source = "Phone", Status = "Contacted", Priority = "High", EstimatedValue = 500000m, CreatedAt = now.AddDays(-10) },
                new Lead { CompanyId = 1, LeadCode = "LEAD-004", FirstName = "Patricia", LastName = "Torres", Email = "patricia.t@designhub.com", Phone = "+63-920-444-5566", CompanyName = "Design Hub Creative", Source = "Social Media", Status = "New", Priority = "Low", EstimatedValue = 45000m, CreatedAt = now.AddDays(-5) },
                new Lead { CompanyId = 1, LeadCode = "LEAD-005", FirstName = "Michael", LastName = "Gonzales", Email = "michael.g@bpoworld.com", Phone = "+63-921-555-6677", CompanyName = "BPO World Corp", Source = "Email", Status = "Negotiation", Priority = "Critical", EstimatedValue = 1200000m, CreatedAt = now.AddDays(-2) }
            };
            context.Leads.AddRange(leadData);
            await context.SaveChangesAsync();
        }

        // 8. Seed Campaigns
        var hasCampaigns = await context.Campaigns.AnyAsync();
        if (!hasCampaigns)
        {
            context.Campaigns.AddRange(
                new Campaign { CompanyId = 1, CampaignCode = "CMP-2026-0001", CampaignName = "Summer Tech Sale 2026", Description = "Big summer discounts on all products", Type = "Sale", Status = "Active", StartDate = now.AddDays(-30), EndDate = now.AddDays(30), Budget = 50000m, ActualSpend = 25000m, TotalReach = 15000, Impressions = 45000, Clicks = 3200, Conversions = 180, Revenue = 450000m, CreatedAt = now.AddDays(-30) },
                new Campaign { CompanyId = 1, CampaignCode = "CMP-2026-0002", CampaignName = "Back to School Promo", Description = "Student discounts on laptops and peripherals", Type = "Promotion", Status = "Active", StartDate = now.AddDays(-15), EndDate = now.AddDays(45), Budget = 30000m, ActualSpend = 12000m, TotalReach = 8000, Impressions = 22000, Clicks = 1800, Conversions = 95, Revenue = 180000m, CreatedAt = now.AddDays(-15) },
                new Campaign { CompanyId = 1, CampaignCode = "CMP-2026-0003", CampaignName = "Gaming Month Bundle", Description = "Special gaming PC bundles at discounted prices", Type = "Bundle", Status = "Completed", StartDate = now.AddDays(-60), EndDate = now.AddDays(-30), Budget = 40000m, ActualSpend = 38000m, TotalReach = 25000, Impressions = 80000, Clicks = 5500, Conversions = 320, Revenue = 890000m, CreatedAt = now.AddDays(-60) }
            );
            await context.SaveChangesAsync();
        }

        // 9. Seed Promotions
        var hasPromos = await context.Promotions.AnyAsync();
        if (!hasPromos)
        {
            context.Promotions.AddRange(
                new Promotion { CompanyId = 1, PromotionCode = "SUMMER2026", PromotionName = "Summer Sale 20% Off", Description = "Get 20% off on all products this summer", DiscountType = "Percentage", DiscountValue = 20, MinOrderAmount = 5000, MaxDiscountAmount = 10000, StartDate = now.AddDays(-15), EndDate = now.AddDays(45), UsageLimit = 500, TimesUsed = 85, IsActive = true, CreatedAt = now.AddDays(-15) },
                new Promotion { CompanyId = 1, PromotionCode = "NEWCUST500", PromotionName = "New Customer ₱500 Off", Description = "₱500 discount for first-time buyers", DiscountType = "Fixed", DiscountValue = 500, MinOrderAmount = 2000, StartDate = now.AddDays(-30), EndDate = now.AddDays(60), UsageLimit = 1000, TimesUsed = 230, IsActive = true, CreatedAt = now.AddDays(-30) },
                new Promotion { CompanyId = 1, PromotionCode = "BUNDLE15", PromotionName = "Bundle Discount 15%", Description = "15% off when buying 3 or more items", DiscountType = "Percentage", DiscountValue = 15, MinOrderAmount = 15000, MaxDiscountAmount = 20000, StartDate = now.AddDays(-10), EndDate = now.AddDays(20), UsageLimit = 200, TimesUsed = 42, IsActive = true, CreatedAt = now.AddDays(-10) }
            );
            await context.SaveChangesAsync();
        }

        Console.WriteLine("✓ Sample data seeded successfully!");
        Console.WriteLine("  - 10 Products, 5 Customers, 8 Orders, 5 Leads, 3 Campaigns, 3 Promotions");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠ Sample data seeding skipped: {ex.Message}");
        // Don't throw - just log and continue
    }
}
