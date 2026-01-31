using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CompuGear.Data;
using CompuGear.Models;

namespace CompuGear.Controllers
{
    /// <summary>
    /// Authentication Controller for Customer Login/Register
    /// </summary>
    public class AuthController : Controller
    {
        private readonly CompuGearDbContext _context;

        public AuthController(CompuGearDbContext context)
        {
            _context = context;
        }

        // Customer Login Page
        public IActionResult Login()
        {
            // If already logged in, redirect to customer portal
            if (HttpContext.Session.GetInt32("CustomerId") != null)
            {
                return RedirectToAction("Index", "CustomerPortal");
            }
            return View();
        }

        // Customer Register Page
        public IActionResult Register()
        {
            // If already logged in, redirect to customer portal
            if (HttpContext.Session.GetInt32("CustomerId") != null)
            {
                return RedirectToAction("Index", "CustomerPortal");
            }
            return View();
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

                // Find customer by email
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Email == request.Email);

                if (customer == null)
                {
                    return Json(new { success = false, message = "Invalid email or password" });
                }

                // Check if customer has linked user account for password verification
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (user != null)
                {
                    // Verify password
                    var hashedPassword = Convert.ToBase64String(
                        System.Security.Cryptography.SHA256.HashData(
                            System.Text.Encoding.UTF8.GetBytes(request.Password + user.Salt)));

                    if (user.PasswordHash != hashedPassword)
                    {
                        return Json(new { success = false, message = "Invalid email or password" });
                    }
                }

                // Set session
                HttpContext.Session.SetInt32("CustomerId", customer.CustomerId);
                HttpContext.Session.SetString("CustomerName", customer.FullName);
                HttpContext.Session.SetString("CustomerEmail", customer.Email);

                return Json(new { success = true, message = "Login successful", redirectUrl = "/CustomerPortal" });
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

                // Check if email already exists
                if (await _context.Customers.AnyAsync(c => c.Email == request.Email))
                {
                    return Json(new { success = false, message = "An account with this email already exists" });
                }

                // Create customer
                var customer = new Customer
                {
                    CustomerCode = $"CUST-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}",
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    Phone = request.Phone,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                // Create user account for password-based login
                var salt = Guid.NewGuid().ToString("N").Substring(0, 16);
                var user = new User
                {
                    Username = request.Email.Split('@')[0] + customer.CustomerId,
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
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);

                // Link user to customer
                customer.UserId = user.UserId;
                await _context.SaveChangesAsync();

                // Set session
                HttpContext.Session.SetInt32("CustomerId", customer.CustomerId);
                HttpContext.Session.SetString("CustomerName", customer.FullName);
                HttpContext.Session.SetString("CustomerEmail", customer.Email);

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
