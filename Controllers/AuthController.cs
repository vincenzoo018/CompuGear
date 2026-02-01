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
                1 or 2 => RedirectToAction("Index", "Home"), // Super Admin, Company Admin
                3 => RedirectToAction("Orders", "Sales"), // Sales Staff
                4 => RedirectToAction("Tickets", "Support"), // Customer Support
                5 => RedirectToAction("Campaigns", "Marketing"), // Marketing Staff
                6 => RedirectToAction("Summary", "Billing"), // Accounting & Billing
                7 => RedirectToAction("Index", "CustomerPortal"), // Customer
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
                    // Verify password
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
                            return Json(new { success = true, message = "Login successful", redirectUrl = "/CustomerPortal" });
                        }

                        // Redirect based on role
                        var redirectUrl = user.RoleId switch
                        {
                            1 or 2 => "/Home", // Super Admin, Company Admin
                            3 => "/Sales/Orders", // Sales Staff
                            4 => "/Support/Tickets", // Customer Support
                            5 => "/Marketing/Campaigns", // Marketing Staff
                            6 => "/Billing/Summary", // Accounting & Billing
                            _ => "/Home"
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
                            HttpContext.Session.SetInt32("RoleId", 7);

                            return Json(new { success = true, message = "Login successful", redirectUrl = "/CustomerPortal" });
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
