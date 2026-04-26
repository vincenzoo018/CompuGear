using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CompuGear.Data;
using CompuGear.Models;
using CompuGear.Services;
using System.Text;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CompuGear.Controllers
{
    /// <summary>
    /// Authentication Controller for Customer and User Login/Register
    /// </summary>
    public class AuthController(CompuGearDbContext context, IAuditService auditService, IConfiguration configuration, IHttpClientFactory httpClientFactory) : Controller
    {
        private readonly CompuGearDbContext _context = context;
        private readonly IAuditService _auditService = auditService;
        private readonly IConfiguration _configuration = configuration;
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private static readonly HashSet<string> AllowedRegistrationAddresses = new(StringComparer.OrdinalIgnoreCase)
        {
            "Quezon City, Metro Manila",
            "Manila, Metro Manila",
            "Makati, Metro Manila",
            "Taguig, Metro Manila",
            "Pasig, Metro Manila",
            "Caloocan, Metro Manila",
            "Cebu City, Cebu",
            "Davao City, Davao del Sur"
        };

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
                    7 => "/CustomerPortal (Customer Dashboard)",
                    8 => "/InventoryStaff (Inventory Dashboard)",
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
                    7 => "/CustomerPortal/Index",
                    8 => "/InventoryStaff/Index",
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
            var recaptchaEnabled = _configuration.GetValue<bool>("ReCaptcha:Enabled");
            var recaptchaSiteKey = _configuration["ReCaptcha:SiteKey"] ?? string.Empty;

            ViewData["ReCaptchaEnabled"] = recaptchaEnabled;
            ViewData["ReCaptchaSiteKey"] = recaptchaSiteKey;

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

        private RedirectToActionResult RedirectBasedOnRole(int roleId)
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
                if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                {
                    return Json(new { success = false, message = "Email/Username and password are required" });
                }

                var recaptchaEnabled = _configuration.GetValue<bool>("ReCaptcha:Enabled");
                if (recaptchaEnabled)
                {
                    var (isValid, message) = await VerifyRecaptchaAsync(request.RecaptchaToken);
                    if (!isValid)
                    {
                        return Json(new { success = false, message });
                    }
                }

                var loginInput = request.Email.Trim();

                // First, try to authenticate as a User (staff member) - match by email or username
                var user = await _context.Users
                    .Include(u => u.Role)
                    .Include(u => u.Company)
                    .FirstOrDefaultAsync(u => (u.Email == loginInput || u.Username == loginInput) && u.IsActive);

                if (user != null)
                {
                    // Verify password using the stored salt
                    var hashedPassword = Convert.ToBase64String(
                        SHA256.HashData(
                            Encoding.UTF8.GetBytes(request.Password + user.Salt)));

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
                            if (user.Company != null)
                            {
                                HttpContext.Session.SetString("CompanyName", user.Company.CompanyName);
                            }
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
                                // Ensure CompanyId is set from Customer record if not already set from User
                                if (!user.CompanyId.HasValue && customer.CompanyId.HasValue)
                                {
                                    HttpContext.Session.SetInt32("CompanyId", customer.CompanyId.Value);
                                }
                            }
                            await _auditService.LogLoginAsync(user.UserId, user.FullName, true);
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
                            7 => "/CustomerPortal/Index", // Customer -> Customer Portal
                            8 => "/InventoryStaff/Index", // Inventory Staff -> Inventory Portal
                            _ => "/Home/Index"
                        };

                        await _auditService.LogLoginAsync(user.UserId, user.FullName, true);
                        return Json(new { success = true, message = "Login successful", redirectUrl });
                    }
                }

                // If not found as User, try to find as Customer (by email or username match)
                var customer2 = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Email == loginInput && c.Status == "Active");

                if (customer2 != null)
                {
                    // Find associated user account for password verification
                    var customerUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email == loginInput || u.Username == loginInput);

                    if (customerUser != null)
                    {
                        var hashedPassword = Convert.ToBase64String(
                            SHA256.HashData(
                                Encoding.UTF8.GetBytes(request.Password + customerUser.Salt)));

                        if (customerUser.PasswordHash == hashedPassword)
                        {
                            // Set session
                            HttpContext.Session.SetInt32("CustomerId", customer2.CustomerId);
                            HttpContext.Session.SetString("CustomerName", customer2.FullName);
                            HttpContext.Session.SetString("CustomerEmail", customer2.Email);
                            HttpContext.Session.SetInt32("UserId", customerUser.UserId);
                            HttpContext.Session.SetInt32("RoleId", 7); // Customer role
                            HttpContext.Session.SetString("RoleName", "Customer");
                            if (customer2.CompanyId.HasValue)
                            {
                                HttpContext.Session.SetInt32("CompanyId", customer2.CompanyId.Value);
                            }
                            else if (customerUser.CompanyId.HasValue)
                            {
                                HttpContext.Session.SetInt32("CompanyId", customerUser.CompanyId.Value);
                            }

                            await _auditService.LogLoginAsync(customerUser.UserId, customer2.FullName, true);
                            return Json(new { success = true, message = "Login successful", redirectUrl = "/CustomerPortal/Index" });
                        }
                    }
                }

                await _auditService.LogLoginAsync(0, request.Email, false, "Invalid credentials");
                return Json(new { success = false, message = "Invalid email/username or password. Please check your credentials and try again." });
            }
            catch (Exception ex)
            {
                var loginIdentifier = request?.Email ?? "unknown";
                await _auditService.LogLoginAsync(0, loginIdentifier, false, ex.Message);
                return Json(new { success = false, message = "An error occurred. Please try again later." });
            }
        }

        private async Task<(bool Success, string Message)> VerifyRecaptchaAsync(string recaptchaToken)
        {
            if (string.IsNullOrWhiteSpace(recaptchaToken))
            {
                return (false, "Please complete the reCAPTCHA challenge.");
            }

            var secretKey = _configuration["ReCaptcha:SecretKey"];
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                return (false, "Login security check is not configured. Please contact support.");
            }

            var verifyUrl = _configuration["ReCaptcha:VerifyUrl"] ?? "https://www.google.com/recaptcha/api/siteverify";
            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

            using var client = _httpClientFactory.CreateClient();
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["secret"] = secretKey,
                ["response"] = recaptchaToken,
                ["remoteip"] = remoteIp
            });

            using var response = await client.PostAsync(verifyUrl, content);
            if (!response.IsSuccessStatusCode)
            {
                return (false, "Unable to verify reCAPTCHA. Please try again.");
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            var verification = JsonSerializer.Deserialize<RecaptchaVerifyResponse>(responseBody);

            if (verification?.Success == true)
            {
                return (true, string.Empty);
            }

            return (false, "reCAPTCHA verification failed. Please try again.");
        }

        // Process Registration
        [HttpPost]
        public async Task<IActionResult> ProcessRegister([FromBody] RegisterRequest request)
        {
            try
            {
                if (request == null)
                {
                    return Json(new { success = false, message = "Invalid registration payload" });
                }

                request.FirstName = request.FirstName?.Trim() ?? string.Empty;
                request.LastName = request.LastName?.Trim() ?? string.Empty;
                request.Email = request.Email?.Trim() ?? string.Empty;
                request.Phone = request.Phone?.Trim() ?? string.Empty;
                request.Address = request.Address?.Trim() ?? string.Empty;

                // Validate required fields
                if (string.IsNullOrEmpty(request.FirstName) || string.IsNullOrEmpty(request.LastName) ||
                    string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                {
                    return Json(new { success = false, message = "All fields are required" });
                }

                if (string.IsNullOrEmpty(request.Address))
                {
                    return Json(new { success = false, message = "Please select an address" });
                }

                if (!AllowedRegistrationAddresses.Contains(request.Address))
                {
                    return Json(new { success = false, message = "Invalid address selection" });
                }

                if (!string.IsNullOrEmpty(request.Phone) && !request.Phone.All(char.IsDigit))
                {
                    return Json(new { success = false, message = "Phone number must contain numbers only" });
                }

                if (!string.IsNullOrEmpty(request.Phone) && request.Phone.Length != 11)
                {
                    return Json(new { success = false, message = "Phone number must be exactly 11 digits" });
                }

                var normalizedEmail = request.Email.ToLowerInvariant();

                // Check if email already exists in Customers
                if (await _context.Customers.AnyAsync(c => c.Email.ToLower() == normalizedEmail))
                {
                    return Json(new { success = false, message = "An account with this email already exists" });
                }
                // Check if email already exists in Users
                if (await _context.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail))
                {
                    return Json(new { success = false, message = "An account with this email already exists" });
                }
                // Validate password complexity: min 12 chars, 1 uppercase, 1 lowercase, 1 digit, 1 special
                if (request.Password.Length < 12)
                {
                    return Json(new { success = false, message = "Password must be at least 12 characters long" });
                }
                if (!request.Password.Any(char.IsUpper))
                {
                    return Json(new { success = false, message = "Password must contain at least one uppercase letter" });
                }
                if (!request.Password.Any(char.IsLower))
                {
                    return Json(new { success = false, message = "Password must contain at least one lowercase letter" });
                }
                if (!request.Password.Any(char.IsDigit))
                {
                    return Json(new { success = false, message = "Password must contain at least one number" });
                }
                if (!request.Password.Any(c => !char.IsLetterOrDigit(c)))
                {
                    return Json(new { success = false, message = "Password must contain at least one special character" });
                }

                if (request.Password != request.ConfirmPassword)
                {
                    return Json(new { success = false, message = "Passwords do not match" });
                }

                // Create user account first
                var salt = Guid.NewGuid().ToString("N")[..16];
                var user = new User
                {
                    Username = request.Email.Split('@')[0] + DateTime.Now.Ticks.ToString()[10..],
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Phone = request.Phone,
                    PasswordHash = Convert.ToBase64String(
                        SHA256.HashData(
                            Encoding.UTF8.GetBytes(request.Password + salt))),
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
                    BillingAddress = request.Address,
                    ShippingAddress = request.Address,
                    BillingCity = request.Address.Split(',')[0].Trim(),
                    ShippingCity = request.Address.Split(',')[0].Trim(),
                    BillingCountry = "Philippines",
                    ShippingCountry = "Philippines",
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
        public async Task<IActionResult> Logout()
        {
            var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var userName = HttpContext.Session.GetString("UserName") ?? HttpContext.Session.GetString("CustomerName") ?? "User";

            if (userId > 0)
            {
                await _auditService.LogLogoutAsync(userId, userName);
            }

            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // One-time: Reset all user passwords to a default value
        [HttpPost]
        public async Task<IActionResult> ResetAllPasswords([FromBody] ResetAllPasswordsRequest request)
        {
            if (string.IsNullOrEmpty(request?.AdminKey) || request.AdminKey != "CompuGear2024ResetKey")
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var defaultPassword = "Password123!";
            var users = await _context.Users.ToListAsync();
            int count = 0;

            foreach (var user in users)
            {
                var salt = Guid.NewGuid().ToString("N")[..16];
                user.Salt = salt;
                user.PasswordHash = Convert.ToBase64String(
                    SHA256.HashData(
                        Encoding.UTF8.GetBytes(defaultPassword + salt)));
                user.UpdatedAt = DateTime.UtcNow;
                count++;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = $"Successfully reset passwords for {count} users" });
        }
    }

    // Request Models
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
        public string RecaptchaToken { get; set; } = string.Empty;
    }

    public class RecaptchaVerifyResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("challenge_ts")]
        public string? ChallengeTimestamp { get; set; }

        [JsonPropertyName("hostname")]
        public string? Hostname { get; set; }

        [JsonPropertyName("error-codes")]
        public string[] ErrorCodes { get; set; } = [];
    }

    public class RegisterRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ResetAllPasswordsRequest
    {
        public string AdminKey { get; set; } = string.Empty;
    }
}
