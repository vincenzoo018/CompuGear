using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CompuGear.Data;
using CompuGear.Models;

namespace CompuGear.Controllers
{
    /// <summary>
    /// Authentication Controller for Customer and User Login/Register
    /// </summary>
    public class AuthController : Controller
    {
        private readonly CompuGearDbContext _context;

        public AuthController(CompuGearDbContext context)
        {
            _context = context;
        }

        // Debug: Check current session (useful for troubleshooting)
        [HttpGet]
        public IActionResult CheckSession()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");
            var userName = HttpContext.Session.GetString("UserName");
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var roleName = HttpContext.Session.GetString("RoleName");

            return Json(new
            {
                isLoggedIn = userId.HasValue,
                userId,
                roleId,
                userName,
                userEmail,
                roleName,
                expectedDashboard = roleId switch
                {
                    1 => "/SuperAdmin (Super Admin Dashboard)",
                    2 => "/Home (Company Admin Dashboard)",
                    3 => "/SalesStaff (Sales Dashboard)",
                    4 => "/SupportStaff (Support Dashboard)",
                    5 => "/MarketingStaff (Marketing Dashboard)",
                    6 => "/BillingStaff (Billing Dashboard)",
                    7 => "/InventoryStaff (Inventory Dashboard)",
                    8 => "/CustomerPortal (Customer Dashboard)",
                    _ => "Not logged in"
                }
            });
        }

        // Debug: Update user role (for testing purposes)
        [HttpGet]
        public async Task<IActionResult> SetUserRole(string email, int roleId)
        {
            if (string.IsNullOrEmpty(email) || roleId < 1 || roleId > 8)
            {
                return Json(new { success = false, message = "Invalid email or roleId (1-8)" });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                return Json(new { success = false, message = $"User not found: {email}" });
            }

            user.RoleId = roleId;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Json(new { 
                success = true, 
                message = $"User {email} role updated to {roleId}",
                redirectUrl = roleId switch
                {
                    1 => "/SuperAdmin/Index",
                    2 => "/Home/Index",
                    3 => "/SalesStaff/Index",
                    4 => "/SupportStaff/Index",
                    5 => "/MarketingStaff/Index",
                    6 => "/BillingStaff/Index",
                    7 => "/InventoryStaff/Index",
                    8 => "/CustomerPortal/Index",
                    _ => "/Home/Index"
                }
            });
        }

        // Login Page
        public IActionResult Login()
        {
            // If already logged in, redirect based on role
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                var roleId = HttpContext.Session.GetInt32("RoleId") ?? 7;
                return RedirectBasedOnRole(roleId);
            }
            if (HttpContext.Session.GetInt32("CustomerId") != null)
            {
                return RedirectToAction("Index", "CustomerPortal");
            }
            return View();
        }

        // Register Page
        public IActionResult Register()
        {
            // If already logged in, redirect based on role
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                var roleId = HttpContext.Session.GetInt32("RoleId") ?? 7;
                return RedirectBasedOnRole(roleId);
            }
            if (HttpContext.Session.GetInt32("CustomerId") != null)
            {
                return RedirectToAction("Index", "CustomerPortal");
            }
            return View();
        }

        private IActionResult RedirectBasedOnRole(int roleId)
        {
            return roleId switch
            {
                1 => RedirectToAction("Index", "SuperAdmin"), // Super Admin -> Super Admin Portal
                2 => RedirectToAction("Index", "Home"), // Company Admin -> Admin Portal
                3 => RedirectToAction("Index", "SalesStaff"), // Sales Staff -> Sales Portal
                4 => RedirectToAction("Index", "SupportStaff"), // Customer Support -> Support Portal
                5 => RedirectToAction("Index", "MarketingStaff"), // Marketing Staff -> Marketing Portal
                6 => RedirectToAction("Index", "BillingStaff"), // Accounting & Billing -> Billing Portal
                7 => RedirectToAction("Index", "CustomerPortal"), // Customer -> Customer Portal
                8 => RedirectToAction("Index", "InventoryStaff"), // Inventory Staff -> Inventory Portal
                _ => RedirectToAction("Index", "Home")
            };
        }

        // Process Login
        [HttpPost]
        public async Task<IActionResult> ProcessLogin([FromBody] LoginRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                {
                    return Json(new { success = false, message = "Email and password are required" });
                }

                // First, try to authenticate as a User (staff member)
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

                if (user != null)
                {
                    // Verify password using the stored salt
                    var hashedPassword = Convert.ToBase64String(
                        System.Security.Cryptography.SHA256.HashData(
                            System.Text.Encoding.UTF8.GetBytes(request.Password + user.Salt)));

                    if (user.PasswordHash == hashedPassword)
                    {
                        // Update last login
                        user.LastLoginAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();

                        // Set session for user
                        HttpContext.Session.SetInt32("UserId", user.UserId);
                        HttpContext.Session.SetString("UserName", user.FullName);
                        HttpContext.Session.SetString("UserEmail", user.Email);
                        HttpContext.Session.SetInt32("RoleId", user.RoleId);
                        HttpContext.Session.SetString("RoleName", user.Role?.RoleName ?? "User");
                        if (user.CompanyId.HasValue)
                        {
                            HttpContext.Session.SetInt32("CompanyId", user.CompanyId.Value);
                        }

                        // If user is a Customer (RoleId = 7), also set customer session
                        if (user.RoleId == 7)
                        {
                            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == user.Email);
                            if (customer != null)
                            {
                                HttpContext.Session.SetInt32("CustomerId", customer.CustomerId);
                                HttpContext.Session.SetString("CustomerName", customer.FullName);
                                HttpContext.Session.SetString("CustomerEmail", customer.Email);
                            }
                            return Json(new { success = true, message = "Login successful", redirectUrl = "/CustomerPortal/Index" });
                        }

                        // Redirect based on role
                        var redirectUrl = user.RoleId switch
                        {
                            1 => "/SuperAdmin/Index", // Super Admin -> Super Admin Portal
                            2 => "/Home/Index", // Company Admin -> Admin Portal
                            3 => "/SalesStaff/Index", // Sales Staff -> Sales Portal
                            4 => "/SupportStaff/Index", // Customer Support -> Support Portal
                            5 => "/MarketingStaff/Index", // Marketing Staff -> Marketing Portal
                            6 => "/BillingStaff/Index", // Accounting & Billing -> Billing Portal
                            7 => "/InventoryStaff/Index", // Inventory Staff -> Inventory Portal
                            8 => "/CustomerPortal/Index", // Customer -> Customer Portal
                            _ => "/Home/Index"
                        };

                        return Json(new { success = true, message = "Login successful", redirectUrl });
                    }
                }

                // If not found as User, try to find as Customer
                var customer2 = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Email == request.Email && c.Status == "Active");

                if (customer2 != null)
                {
                    // Find associated user account for password verification
                    var customerUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email == request.Email);

                    if (customerUser != null)
                    {
                        var hashedPassword = Convert.ToBase64String(
                            System.Security.Cryptography.SHA256.HashData(
                                System.Text.Encoding.UTF8.GetBytes(request.Password + customerUser.Salt)));

                        if (customerUser.PasswordHash == hashedPassword)
                        {
                            // Set session
                            HttpContext.Session.SetInt32("CustomerId", customer2.CustomerId);
                            HttpContext.Session.SetString("CustomerName", customer2.FullName);
                            HttpContext.Session.SetString("CustomerEmail", customer2.Email);
                            HttpContext.Session.SetInt32("UserId", customerUser.UserId);
                            HttpContext.Session.SetInt32("RoleId", 8); // Customer role

                            return Json(new { success = true, message = "Login successful", redirectUrl = "/CustomerPortal/Index" });
                        }
                    }
                }

                return Json(new { success = false, message = "Invalid email or password" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        // Process Registration
        [HttpPost]
        public async Task<IActionResult> ProcessRegister([FromBody] RegisterRequest request)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrEmpty(request.FirstName) || string.IsNullOrEmpty(request.LastName) ||
                    string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                {
                    return Json(new { success = false, message = "All fields are required" });
                }

                // Check if email already exists in Customers
                if (await _context.Customers.AnyAsync(c => c.Email == request.Email))
                {
                    return Json(new { success = false, message = "An account with this email already exists" });
                }

                // Check if email already exists in Users
                if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                {
                    return Json(new { success = false, message = "An account with this email already exists" });
                }

                // Create user account first
                var salt = Guid.NewGuid().ToString("N").Substring(0, 16);
                var user = new User
                {
                    Username = request.Email.Split('@')[0] + DateTime.Now.Ticks.ToString().Substring(10),
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Phone = request.Phone,
                    PasswordHash = Convert.ToBase64String(
                        System.Security.Cryptography.SHA256.HashData(
                            System.Text.Encoding.UTF8.GetBytes(request.Password + salt))),
                    Salt = salt,
                    RoleId = 7, // Customer role
                    IsActive = true,
                    IsEmailVerified = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Create customer record
                var customer = new Customer
                {
                    CustomerCode = $"CUST-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}",
                    UserId = user.UserId,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    Phone = request.Phone,
                    Status = "Active",
                    CategoryId = 1, // Standard customer
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                // Set session
                HttpContext.Session.SetInt32("CustomerId", customer.CustomerId);
                HttpContext.Session.SetString("CustomerName", customer.FullName);
                HttpContext.Session.SetString("CustomerEmail", customer.Email);
                HttpContext.Session.SetInt32("UserId", user.UserId);
                HttpContext.Session.SetInt32("RoleId", 7);

                return Json(new { success = true, message = "Registration successful", redirectUrl = "/CustomerPortal" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        // Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }

    // Request Models
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }

    public class RegisterRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
