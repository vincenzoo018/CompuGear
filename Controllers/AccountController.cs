using System.Security.Cryptography;
using System.Text;
using CompuGear.Data;
using CompuGear.Models.ViewModels;
using CompuGear.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CompuGear.Controllers
{
	[RequireHttps]
	public class AccountController(
		CompuGearDbContext context,
		IOtpService otpService,
		IEmailService emailService,
		ILogger<AccountController> logger) : Controller
	{
		private readonly CompuGearDbContext _context = context;
		private readonly IOtpService _otpService = otpService;
		private readonly IEmailService _emailService = emailService;
		private readonly ILogger<AccountController> _logger = logger;

		[HttpGet]
		public IActionResult ForgotPassword()
		{
			return View(new ForgotPasswordViewModel());
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
		{
			model.Email = _otpService.NormalizeEmail(model.Email);

			if (!ModelState.IsValid)
			{
				return View(model);
			}

			var userExists = await _context.Users
				.AsNoTracking()
				.AnyAsync(u => u.IsActive && u.Email.ToLower() == model.Email);

			if (userExists)
			{
				var otpResult = _otpService.GenerateOtp(model.Email);
				if (!otpResult.Success)
				{
					if (otpResult.FailureReason == OtpIssueFailureReason.Cooldown)
					{
						TempData["InfoMessage"] =
							$"An OTP was already sent. Please check your inbox or spam. You can resend after {otpResult.CooldownSecondsRemaining} seconds.";
						return RedirectToAction(nameof(VerifyOtp), new { email = model.Email });
					}

					ModelState.AddModelError(string.Empty, "Unable to create OTP right now. Please try again.");
					return View(model);
				}

				try
				{
					await _emailService.SendOtpAsync(
						model.Email,
						otpResult.OtpCode!,
						otpResult.ExpirationTimeUtc!.Value);
				}
				catch (InvalidOperationException ex)
				{
					_otpService.ClearOtp(model.Email);
					ModelState.AddModelError(string.Empty, ex.Message);
					return View(model);
				}
				catch (Exception ex)
				{
					_otpService.ClearOtp(model.Email);
					_logger.LogError(ex, "ForgotPassword OTP sending failed for a user email.");
					ModelState.AddModelError(string.Empty, "Unable to send OTP email right now. Please try again.");
					return View(model);
				}
			}

			TempData["InfoMessage"] = "If your email is registered, an OTP has been sent. It expires in 5 minutes.";
			return RedirectToAction(nameof(VerifyOtp), new { email = model.Email });
		}

		[HttpGet]
		public IActionResult VerifyOtp(string? email)
		{
			var normalizedEmail = _otpService.NormalizeEmail(email ?? string.Empty);
			if (string.IsNullOrWhiteSpace(normalizedEmail))
			{
				return RedirectToAction(nameof(ForgotPassword));
			}

			ViewBag.CooldownSeconds = _otpService.GetResendCooldownSeconds(normalizedEmail);
			return View(new VerifyOtpViewModel
			{
				Email = normalizedEmail
			});
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult VerifyOtp(VerifyOtpViewModel model)
		{
			model.Email = _otpService.NormalizeEmail(model.Email);
			model.OtpCode = (model.OtpCode ?? string.Empty).Trim();

			if (!ModelState.IsValid)
			{
				ViewBag.CooldownSeconds = _otpService.GetResendCooldownSeconds(model.Email);
				return View(model);
			}

			var verifyResult = _otpService.VerifyOtp(model.Email, model.OtpCode);
			if (!verifyResult.Success)
			{
				var message = verifyResult.FailureReason switch
				{
					OtpVerificationFailureReason.ExpiredOtp => "OTP expired. Please request a new OTP.",
					OtpVerificationFailureReason.TooManyAttempts => "Too many failed attempts. Please request a new OTP.",
					_ => verifyResult.Message
				};

				ModelState.AddModelError(string.Empty, message);
				ViewBag.CooldownSeconds = _otpService.GetResendCooldownSeconds(model.Email);
				return View(model);
			}

			TempData["SuccessMessage"] = "OTP verified. You can now reset your password.";
			return RedirectToAction(nameof(ResetPassword), new { email = model.Email });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ResendOtp(string email)
		{
			var normalizedEmail = _otpService.NormalizeEmail(email);
			if (string.IsNullOrWhiteSpace(normalizedEmail))
			{
				TempData["ErrorMessage"] = "Invalid email address.";
				return RedirectToAction(nameof(ForgotPassword));
			}

			var userExists = await _context.Users
				.AsNoTracking()
				.AnyAsync(u => u.IsActive && u.Email.ToLower() == normalizedEmail);

			if (userExists)
			{
				var otpResult = _otpService.ResendOtp(normalizedEmail);
				if (!otpResult.Success)
				{
					TempData["ErrorMessage"] = otpResult.FailureReason == OtpIssueFailureReason.Cooldown
						? $"Please wait {otpResult.CooldownSecondsRemaining} seconds before resending OTP."
						: "Unable to resend OTP right now.";

					return RedirectToAction(nameof(VerifyOtp), new { email = normalizedEmail });
				}

				try
				{
					await _emailService.SendOtpAsync(
						normalizedEmail,
						otpResult.OtpCode!,
						otpResult.ExpirationTimeUtc!.Value);

					TempData["InfoMessage"] = "A new OTP has been sent to your email.";
				}
				catch (InvalidOperationException ex)
				{
					_otpService.ClearOtp(normalizedEmail);
					TempData["ErrorMessage"] = ex.Message;
				}
				catch (Exception ex)
				{
					_otpService.ClearOtp(normalizedEmail);
					_logger.LogError(ex, "ResendOtp email sending failed for a user email.");
					TempData["ErrorMessage"] = "Unable to send OTP email right now. Please try again.";
				}
			}
			else
			{
				TempData["InfoMessage"] = "If your email is registered, an OTP has been sent.";
			}

			return RedirectToAction(nameof(VerifyOtp), new { email = normalizedEmail });
		}

		[HttpGet]
		public IActionResult ResetPassword(string? email)
		{
			var normalizedEmail = _otpService.NormalizeEmail(email ?? string.Empty);
			if (string.IsNullOrWhiteSpace(normalizedEmail) || !_otpService.CanResetPassword(normalizedEmail))
			{
				TempData["ErrorMessage"] = "Please verify OTP first before resetting your password.";
				return RedirectToAction(nameof(ForgotPassword));
			}

			return View(new ResetPasswordViewModel
			{
				Email = normalizedEmail
			});
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
		{
			model.Email = _otpService.NormalizeEmail(model.Email);

			if (!ModelState.IsValid)
			{
				return View(model);
			}

			if (!MeetsPasswordPolicy(model.NewPassword))
			{
				ModelState.AddModelError(nameof(model.NewPassword),
					"Password must be at least 12 characters and include uppercase, lowercase, number, and special character.");
				return View(model);
			}

			if (!_otpService.CanResetPassword(model.Email))
			{
				ModelState.AddModelError(string.Empty, "OTP verification is required before resetting password.");
				return View(model);
			}

			var user = await _context.Users.FirstOrDefaultAsync(u => u.IsActive && u.Email.ToLower() == model.Email);
			if (user == null)
			{
				_otpService.ConsumeResetAuthorization(model.Email);
				ModelState.AddModelError(string.Empty, "Unable to reset password for this account.");
				return View(model);
			}

			var salt = Guid.NewGuid().ToString("N")[..16];
			user.Salt = salt;
			user.PasswordHash = Convert.ToBase64String(
				SHA256.HashData(
					Encoding.UTF8.GetBytes(model.NewPassword + salt)));
			user.PasswordChangedAt = DateTime.UtcNow;
			user.UpdatedAt = DateTime.UtcNow;

			await _context.SaveChangesAsync();

			_otpService.ConsumeResetAuthorization(model.Email);

			TempData["SuccessMessage"] = "Password reset successful. Please sign in with your new password.";
			return RedirectToAction("Login", "Auth");
		}

		private static bool MeetsPasswordPolicy(string password)
		{
			if (string.IsNullOrWhiteSpace(password) || password.Length < 12)
			{
				return false;
			}

			var hasUpper = password.Any(char.IsUpper);
			var hasLower = password.Any(char.IsLower);
			var hasDigit = password.Any(char.IsDigit);
			var hasSpecial = password.Any(ch => !char.IsLetterOrDigit(ch));

			return hasUpper && hasLower && hasDigit && hasSpecial;
		}
	}
}
