using System.Diagnostics;
using CompuGear.Data;
using CompuGear.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace CompuGear.Controllers
{
    /// <summary>
    /// Home Controller - Dashboard for Admin users (Super Admin & Company Admin only)
    /// RoleId: 1 - Super Admin, 2 - Company Admin
    /// </summary>
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly CompuGearDbContext _context;

        public HomeController(ILogger<HomeController> logger, CompuGearDbContext context)
        {
            _logger = logger;
            _context = context;
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
            
            // API endpoints: allow all authenticated staff + admin roles
            if (isApiRequest) return;
            
            // View endpoints: admin only
            if (roleId != 1 && roleId != 2)
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

        #region View Actions

        public IActionResult Index()
        {
            return View("~/Views/Admin/Index.cshtml");
        }

        public IActionResult Approvals()
        {
            return View("~/Views/Admin/Approvals.cshtml");
        }

        public IActionResult Reports()
        {
            return View("~/Views/Admin/Reports/Index.cshtml");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        #endregion

        #region Dashboard API

        [HttpGet]
        [Route("api/dashboard/stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var today = DateTime.Today;
            var thisMonth = new DateTime(today.Year, today.Month, 1);

            var companyId = GetCompanyId();

            var stats = new
            {
                TotalCustomers = await _context.Customers.CountAsync(c => companyId == null || c.CompanyId == companyId),
                TotalOrders = await _context.Orders.CountAsync(o => companyId == null || o.CompanyId == companyId),
                TotalRevenue = await _context.Orders.Where(o => o.PaymentStatus == "Paid" && (companyId == null || o.CompanyId == companyId)).SumAsync(o => o.TotalAmount),
                PendingOrders = await _context.Orders.CountAsync(o => o.OrderStatus == "Pending" && (companyId == null || o.CompanyId == companyId)),
                OpenTickets = await _context.SupportTickets.CountAsync(t => (t.Status == "Open" || t.Status == "In Progress") && (companyId == null || t.CompanyId == companyId)),
                LowStockProducts = await _context.Products.CountAsync(p => p.StockQuantity <= p.ReorderLevel && (companyId == null || p.CompanyId == companyId)),
                ActiveCampaigns = await _context.Campaigns.CountAsync(c => c.Status == "Active" && (companyId == null || c.CompanyId == companyId)),
                MonthlyRevenue = await _context.Orders
                    .Where(o => o.OrderDate >= thisMonth && o.PaymentStatus == "Paid" && (companyId == null || o.CompanyId == companyId))
                    .SumAsync(o => o.TotalAmount)
            };

            return Ok(stats);
        }

        [HttpGet]
        [Route("api/analytics/dashboard")]
        public async Task<IActionResult> GetDashboardAnalytics()
        {
            try
            {
                var companyId = GetCompanyId();
                var now = DateTime.UtcNow;
                var thisMonth = new DateTime(now.Year, now.Month, 1);
                var lastMonth = thisMonth.AddMonths(-1);
                var thisYear = new DateTime(now.Year, 1, 1);

                // Revenue metrics
                var totalRevenue = await _context.Orders
                    .Where(o => o.PaymentStatus == "Paid" && (companyId == null || o.CompanyId == companyId))
                    .SumAsync(o => o.TotalAmount);

                var monthlyRevenue = await _context.Orders
                    .Where(o => o.OrderDate >= thisMonth && o.PaymentStatus == "Paid" && (companyId == null || o.CompanyId == companyId))
                    .SumAsync(o => o.TotalAmount);

                var lastMonthRevenue = await _context.Orders
                    .Where(o => o.OrderDate >= lastMonth && o.OrderDate < thisMonth && o.PaymentStatus == "Paid" && (companyId == null || o.CompanyId == companyId))
                    .SumAsync(o => o.TotalAmount);

                // Order metrics
                var totalOrders = await _context.Orders.Where(o => companyId == null || o.CompanyId == companyId).CountAsync();
                var monthlyOrders = await _context.Orders.Where(o => o.OrderDate >= thisMonth && (companyId == null || o.CompanyId == companyId)).CountAsync();
                var pendingOrders = await _context.Orders.Where(o => o.OrderStatus == "Pending" && (companyId == null || o.CompanyId == companyId)).CountAsync();

                // Customer metrics
                var totalCustomers = await _context.Customers.Where(c => companyId == null || c.CompanyId == companyId).CountAsync();
                var newCustomersThisMonth = await _context.Customers.Where(c => c.CreatedAt >= thisMonth && (companyId == null || c.CompanyId == companyId)).CountAsync();

                // Product metrics
                var totalProducts = await _context.Products.Where(p => companyId == null || p.CompanyId == companyId).CountAsync();
                var lowStockProducts = await _context.Products.Where(p => p.StockQuantity <= p.ReorderLevel && (companyId == null || p.CompanyId == companyId)).CountAsync();
                var outOfStockProducts = await _context.Products.Where(p => p.StockQuantity == 0 && (companyId == null || p.CompanyId == companyId)).CountAsync();

                // Support metrics  
                var openTickets = await _context.SupportTickets.Where(t => t.Status != "Closed" && t.Status != "Resolved" && (companyId == null || t.CompanyId == companyId)).CountAsync();

                // Monthly sales data for chart
                var monthlySales = await _context.Orders
                    .Where(o => o.OrderDate >= thisYear && o.PaymentStatus == "Paid" && (companyId == null || o.CompanyId == companyId))
                    .GroupBy(o => o.OrderDate.Month)
                    .Select(g => new { Month = g.Key, Revenue = g.Sum(o => o.TotalAmount), Orders = g.Count() })
                    .ToListAsync();

                // Top products
                var topProducts = await _context.OrderItems
                    .Include(oi => oi.Order)
                    .Where(oi => oi.Order.OrderDate >= thisMonth && (companyId == null || oi.Order.CompanyId == companyId))
                    .GroupBy(oi => new { oi.ProductId, oi.ProductName })
                    .Select(g => new { g.Key.ProductName, Quantity = g.Sum(x => x.Quantity), Revenue = g.Sum(x => x.TotalPrice) })
                    .OrderByDescending(x => x.Revenue)
                    .Take(5)
                    .ToListAsync();

                // Recent orders
                var recentOrders = await _context.Orders
                    .Where(o => companyId == null || o.CompanyId == companyId)
                    .Include(o => o.Customer)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .Select(o => new { o.OrderNumber, CustomerName = o.Customer != null ? o.Customer.FirstName + " " + o.Customer.LastName : "Guest", o.TotalAmount, o.OrderStatus, o.OrderDate })
                    .ToListAsync();

                var revenueGrowth = lastMonthRevenue > 0 ? ((monthlyRevenue - lastMonthRevenue) / lastMonthRevenue) * 100 : 0;

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        revenue = new { total = totalRevenue, monthly = monthlyRevenue, lastMonth = lastMonthRevenue, growth = revenueGrowth },
                        orders = new { total = totalOrders, monthly = monthlyOrders, pending = pendingOrders },
                        customers = new { total = totalCustomers, newThisMonth = newCustomersThisMonth },
                        products = new { total = totalProducts, lowStock = lowStockProducts, outOfStock = outOfStockProducts },
                        support = new { openTickets },
                        charts = new { monthlySales, topProducts },
                        recentOrders
                    }
                });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        #endregion
    }

}
