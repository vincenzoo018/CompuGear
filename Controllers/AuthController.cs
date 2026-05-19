using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CompuGear.Data;
using CompuGear.Models;
using CompuGear.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CompuGear.Controllers
{
    /// <summary>
    /// Authentication Controller for Customer and User Login/Register
    /// </summary>
    [AutoValidateAntiforgeryToken]
    public class AuthController(
        CompuGearDbContext context,
        IAuditService auditService,
        ILogger<AuthController> logger,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        IOtpService otpService,
        IEmailService emailService,
        IPasswordSecurityService passwordSecurity) : Controller
    {
        private readonly CompuGearDbContext _context = context;
        private readonly IAuditService _auditService = auditService;
        private readonly ILogger<AuthController> _logger = logger;
        private readonly IConfiguration _configuration = configuration;
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly IOtpService _otpService = otpService;
        private readonly IEmailService _emailService = emailService;
        private readonly IPasswordSecurityService _passwordSecurity = passwordSecurity;
        private const string PendingLoginUserIdKey = "PendingLoginUserId";
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
        [Authorize]
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
        [Authorize(Policy = "SuperAdminOnly")]
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
        [AllowAnonymous]
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
            var recaptchaConfig = GetRecaptchaConfig();
            ViewData["ReCaptchaEnabled"] = recaptchaConfig.Enabled;
            ViewData["ReCaptchaSiteKey"] = recaptchaConfig.SiteKey;
            ViewData["ReCaptchaVersion"] = recaptchaConfig.Version;
            ViewData["ReCaptchaAction"] = recaptchaConfig.Action;

            return View();
        }

        // Register Page
        [AllowAnonymous]
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
            var recaptchaConfig = GetRecaptchaConfig();
            ViewData["ReCaptchaEnabled"] = recaptchaConfig.Enabled;
            ViewData["ReCaptchaSiteKey"] = recaptchaConfig.SiteKey;
            ViewData["ReCaptchaVersion"] = recaptchaConfig.Version;
            ViewData["ReCaptchaAction"] = recaptchaConfig.Action;
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
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        [HttpPost]
        public async Task<IActionResult> ProcessLogin([FromBody] LoginRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                {
                    return Json(new { success = false, message = "Email/Username and password are required" });
                }

                var recaptchaConfig = GetRecaptchaConfig();
                if (recaptchaConfig.Enabled)
                {
                    var (isValid, message) = await VerifyRecaptchaAsync(
                        request.RecaptchaToken,
                        recaptchaConfig.Version,
                        recaptchaConfig.Action,
                        recaptchaConfig.ScoreThreshold);
                    if (!isValid)
                    {
                        return Json(new { success = false, message });
                    }
                }

                var loginInput = request.Email.Trim();
                ClearPendingLoginSession();

                var user = await ResolveUserForLoginAsync(loginInput);
                if (user == null)
                {
                    await TryLogLoginAsync(0, request.Email, false, "Invalid credentials");
                    return Json(new { success = false, message = "Invalid email/username or password. Please check your credentials and try again." });
                }

                if (!user.IsActive)
                {
                    await TryLogLoginAsync(user.UserId, user.Email, false, "Account is inactive");
                    return Json(new { success = false, message = "This account is inactive or permanently locked. Contact admin for reactivation." });
                }

                if (user.RoleId == 7)
                {
                    var activeCustomerExists = await _context.Customers.AnyAsync(c =>
                        ((c.UserId.HasValue && c.UserId == user.UserId) || c.Email == user.Email)
                        && c.Status == "Active");
                    if (!activeCustomerExists)
                    {
                        await TryLogLoginAsync(user.UserId, user.Email, false, "Customer account inactive");
                        return Json(new { success = false, message = "Customer account is inactive. Contact support for assistance." });
                    }
                }

                if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
                {
                    var minutesLeft = Math.Max(1, (int)Math.Ceiling((user.LockoutEnd.Value - DateTime.UtcNow).TotalMinutes));
                    await TryLogLoginAsync(user.UserId, user.Email, false, "Temporary lockout active");
                    return Json(new { success = false, message = $"Account is temporarily locked. Try again in {minutesLeft} minute(s)." });
                }

                var verify = _passwordSecurity.VerifyPassword(request.Password, user.PasswordHash, user.Salt);
                if (!verify.Success)
                {
                    return await RegisterFailedLoginAsync(user, loginInput);
                }

                if (verify.NeedsRehash)
                {
                    user.PasswordHash = _passwordSecurity.HashPassword(request.Password);
                    user.Salt = string.Empty;
                    user.PasswordChangedAt ??= DateTime.UtcNow;
                    user.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                var loginOtpEnabled = _configuration.GetValue("Security:LoginOtpEnabled", false);
                if (!loginOtpEnabled)
                {
                    var redirectUrl = await CompleteLoginAsync(user);
                    return Json(new { success = true, message = "Login successful", redirectUrl });
                }

                var otpIssue = _otpService.GenerateLoginOtp(user.Email);
                if (!otpIssue.Success || string.IsNullOrWhiteSpace(otpIssue.OtpCode))
                {
                    var redirectUrl = await CompleteLoginAsync(user);
                    return Json(new { success = true, message = "Login successful", redirectUrl });
                }

                if (!otpIssue.ExpirationTimeUtc.HasValue)
                {
                    return Json(new { success = false, message = "Unable to generate OTP at this time. Please try again." });
                }

                var otpCode = otpIssue.OtpCode!;
                var otpExpiry = otpIssue.ExpirationTimeUtc.Value;

                try
                {
                    await _emailService.SendLoginOtpAsync(user.Email, otpCode, otpExpiry);
                }
                catch
                {
                    _otpService.ClearOtp(user.Email);
                    var redirectUrl = await CompleteLoginAsync(user);
                    return Json(new { success = true, message = "Login successful", redirectUrl });
                }

                HttpContext.Session.SetInt32(PendingLoginUserIdKey, user.UserId);
                return Json(new
                {
                    success = true,
                    requiresOtp = true,
                    message = $"A verification code was sent to {MaskEmail(user.Email)}. Enter it to continue."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for {Email}", request?.Email ?? "unknown");
                var loginIdentifier = request?.Email ?? "unknown";
                try { await TryLogLoginAsync(0, loginIdentifier, false, ex.Message); } catch { /* ignore audit failure */ }
                return Json(new { success = false, message = "An error occurred. Please try again later." });
            }
        }

        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        [HttpPost]
        public async Task<IActionResult> VerifyLoginOtp([FromBody] VerifyLoginOtpRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.OtpCode))
                {
                    return Json(new { success = false, message = "OTP code is required." });
                }

                var pendingUserId = HttpContext.Session.GetInt32(PendingLoginUserIdKey);
                if (!pendingUserId.HasValue)
                {
                    return Json(new { success = false, message = "Login session expired. Please login again." });
                }

                var user = await _context.Users
                    .Include(u => u.Role)
                    .Include(u => u.Company)
                    .FirstOrDefaultAsync(u => u.UserId == pendingUserId.Value);

                if (user == null || !user.IsActive)
                {
                    ClearPendingLoginSession();
                    return Json(new { success = false, message = "Account is inactive or unavailable. Please login again." });
                }

                if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
                {
                    var minutesLeft = Math.Max(1, (int)Math.Ceiling((user.LockoutEnd.Value - DateTime.UtcNow).TotalMinutes));
                    return Json(new { success = false, message = $"Account is temporarily locked. Try again in {minutesLeft} minute(s)." });
                }

                var verification = _otpService.VerifyLoginOtp(user.Email, request.OtpCode);
                if (!verification.Success)
                {
                    if (verification.FailureReason is OtpVerificationFailureReason.ExpiredOtp or OtpVerificationFailureReason.TooManyAttempts)
                    {
                        ClearPendingLoginSession();
                    }

                    var message = verification.FailureReason switch
                    {
                        OtpVerificationFailureReason.ExpiredOtp => "OTP expired. Please login again to request a new code.",
                        OtpVerificationFailureReason.TooManyAttempts => "Too many incorrect OTP attempts. Please login again.",
                        _ => "Invalid OTP. Please try again."
                    };

                    return Json(new { success = false, message });
                }

                ClearPendingLoginSession();
                var redirectUrl = await CompleteLoginAsync(user);
                return Json(new { success = true, message = "Login successful", redirectUrl });
            }
            catch (Exception ex)
            {
                await TryLogLoginAsync(0, "otp_verification", false, ex.Message);
                return Json(new { success = false, message = "Unable to verify OTP. Please try again." });
            }
        }

        private async Task<User?> ResolveUserForLoginAsync(string loginInput)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Email == loginInput || u.Username == loginInput);
            if (user != null)
            {
                return user;
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email == loginInput && c.Status == "Active");
            if (customer == null)
            {
                return null;
            }

            return await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u =>
                    (customer.UserId.HasValue && u.UserId == customer.UserId.Value)
                    || u.Email == loginInput
                    || u.Username == loginInput);
        }

        private async Task<IActionResult> RegisterFailedLoginAsync(User user, string loginIdentifier)
        {
            user.FailedLoginAttempts += 1;
            user.UpdatedAt = DateTime.UtcNow;

            string message;
            if (user.FailedLoginAttempts >= 7)
            {
                user.IsActive = false;
                user.LockoutEnd = null;
                message = "Your account has been permanently locked due to repeated failed attempts. Contact admin to reactivate it.";
            }
            else if (user.FailedLoginAttempts == 6)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(10);
                message = "Too many failed attempts. Your account is locked for 10 minutes.";
            }
            else if (user.FailedLoginAttempts == 5)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(5);
                message = "Too many failed attempts. Your account is locked for 5 minutes.";
            }
            else
            {
                var remaining = 5 - user.FailedLoginAttempts;
                message = $"Invalid email/username or password. You have {remaining} attempt(s) remaining before temporary lockout.";
            }

            await _context.SaveChangesAsync();
            await TryLogLoginAsync(user.UserId, loginIdentifier, false, message);
            return Json(new { success = false, message });
        }

        private async Task<string> CompleteLoginAsync(User user)
        {
            user.LastLoginAt = DateTime.UtcNow;
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            user.UpdatedAt = DateTime.UtcNow;

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

            if (user.RoleId == 7)
            {
                var customer = await _context.Customers.FirstOrDefaultAsync(c =>
                    (c.UserId.HasValue && c.UserId == user.UserId) || c.Email == user.Email);

                if (customer != null)
                {
                    HttpContext.Session.SetInt32("CustomerId", customer.CustomerId);
                    HttpContext.Session.SetString("CustomerName", customer.FullName);
                    HttpContext.Session.SetString("CustomerEmail", customer.Email);

                    if (!user.CompanyId.HasValue && customer.CompanyId.HasValue)
                    {
                        HttpContext.Session.SetInt32("CompanyId", customer.CompanyId.Value);
                    }
                }
            }

            await _context.SaveChangesAsync();
            await TryLogLoginAsync(user.UserId, user.FullName, true);

            return GetRedirectUrl(user.RoleId);
        }

        private void ClearPendingLoginSession()
        {
            HttpContext.Session.Remove(PendingLoginUserIdKey);
        }

        private async Task TryLogLoginAsync(int userId, string userName, bool success, string? reason = null)
        {
            try
            {
                await _auditService.LogLoginAsync(userId, userName, success, reason);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Login audit logging failed for {UserName}", userName);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Login audit logging failed for {UserName}", userName);
            }
        }

        private async Task TryLogLogoutAsync(int userId, string userName)
        {
            try
            {
                await _auditService.LogLogoutAsync(userId, userName);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Logout audit logging failed for {UserName}", userName);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Logout audit logging failed for {UserName}", userName);
            }
        }

        private static string MaskEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            {
                return "your email";
            }

            var parts = email.Split('@');
            var name = parts[0];
            if (name.Length <= 2)
            {
                return $"***@{parts[1]}";
            }

            return $"{name[0]}***{name[^1]}@{parts[1]}";
        }

        private string GetRedirectUrl(int roleId)
        {
            var fallback = Url.Action("Index", "Home") ?? "/Home/Index";
            return roleId switch
            {
                1 => Url.Action("Index", "SuperAdmin"),
                2 => Url.Action("Index", "Home"),
                3 => Url.Action("Index", "SalesStaff"),
                4 => Url.Action("Index", "SupportStaff"),
                5 => Url.Action("Index", "MarketingStaff"),
                6 => Url.Action("Index", "BillingStaff"),
                7 => Url.Action("Index", "CustomerPortal"),
                8 => Url.Action("Index", "InventoryStaff"),
                _ => fallback
            } ?? fallback;
        }

        private async Task<(bool Success, string Message)> VerifyRecaptchaAsync(
            string recaptchaToken,
            string version,
            string expectedAction,
            decimal scoreThreshold)
        {
            if (string.IsNullOrWhiteSpace(recaptchaToken))
            {
                return (false, "Please complete the reCAPTCHA challenge.");
            }

            var recaptchaConfig = GetRecaptchaConfig();
            var secretKey = recaptchaConfig.SecretKey;
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
                if (string.Equals(version, "v3", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(expectedAction)
                        && !string.Equals(verification.Action ?? string.Empty, expectedAction, StringComparison.OrdinalIgnoreCase))
                    {
                        return (false, "reCAPTCHA verification failed. Please try again.");
                    }

                    if (verification.Score < (double)scoreThreshold)
                    {
                        return (false, "reCAPTCHA score too low. Please try again.");
                    }
                }

                return (true, string.Empty);
            }

            return (false, "reCAPTCHA verification failed. Please try again.");
        }

        private (bool Enabled, string SiteKey, string SecretKey, string Version, decimal ScoreThreshold, string Action) GetRecaptchaConfig()
        {
            var enabled = _configuration.GetValue<bool>("ReCaptcha:Enabled");
            var version = NormalizeRecaptchaVersion(_configuration["ReCaptcha:Version"]);
            var action = (_configuration["ReCaptcha:Action"] ?? "login").Trim();
            if (string.IsNullOrWhiteSpace(action))
            {
                action = "login";
            }
            var scoreThreshold = _configuration.GetValue("ReCaptcha:ScoreThreshold", 0.5m);

            var siteKey = _configuration["ReCaptcha:SiteKey"] ?? string.Empty;
            var secretKey = _configuration["ReCaptcha:SecretKey"] ?? string.Empty;

            return (enabled, siteKey, secretKey, version, scoreThreshold, action);
        }

        private static string NormalizeRecaptchaVersion(string? version)
        {
            if (string.IsNullOrWhiteSpace(version))
            {
                return "v2";
            }

            var normalized = version.Trim().ToLowerInvariant();
            if (normalized.StartsWith("v2", StringComparison.Ordinal) || normalized == "2" || normalized == "checkbox")
            {
                return "v2";
            }

            if (normalized.StartsWith("v3", StringComparison.Ordinal) || normalized == "3" || normalized == "score")
            {
                return "v3";
            }

            return "v2";
        }

        // Process Registration
        [AllowAnonymous]
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

                var recaptchaConfig = GetRecaptchaConfig();
                if (recaptchaConfig.Enabled)
                {
                    var (isValid, message) = await VerifyRecaptchaAsync(
                        request.RecaptchaToken,
                        recaptchaConfig.Version,
                        recaptchaConfig.Action,
                        recaptchaConfig.ScoreThreshold);
                    if (!isValid)
                    {
                        return Json(new { success = false, message });
                    }
                }

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
                if (!_passwordSecurity.IsStrongPassword(request.Password, out var passwordError))
                {
                    return Json(new { success = false, message = passwordError });
                }

                if (request.Password != request.ConfirmPassword)
                {
                    return Json(new { success = false, message = "Passwords do not match" });
                }

                // Create user account first
                var user = new User
                {
                    Username = request.Email.Split('@')[0] + DateTime.Now.Ticks.ToString()[10..],
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Phone = request.Phone,
                    PasswordHash = _passwordSecurity.HashPassword(request.Password),
                    Salt = string.Empty,
                    RoleId = 7, // Customer role
                    IsActive = true,
                    IsEmailVerified = true,
                    PasswordChangedAt = DateTime.UtcNow,
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
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var userName = HttpContext.Session.GetString("UserName") ?? HttpContext.Session.GetString("CustomerName") ?? "User";

            if (userId > 0)
            {
                await TryLogLogoutAsync(userId, userName);
            }

            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // One-time: Reset all user passwords to a default value
        [Authorize(Policy = "SuperAdminOnly")]
        [HttpPost]
        public async Task<IActionResult> ResetAllPasswords()
        {
            var defaultPassword = "Password123!";
            var users = await _context.Users.ToListAsync();
            int count = 0;

            foreach (var user in users)
            {
                user.PasswordHash = _passwordSecurity.HashPassword(defaultPassword);
                user.Salt = string.Empty;
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

        [JsonPropertyName("score")]
        public double Score { get; set; }

        [JsonPropertyName("action")]
        public string? Action { get; set; }

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
        public string RecaptchaToken { get; set; } = string.Empty;
    }

    public class VerifyLoginOtpRequest
    {
        public string OtpCode { get; set; } = string.Empty;
    }
}
