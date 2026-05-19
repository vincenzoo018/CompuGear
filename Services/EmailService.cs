using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CompuGear.Services
{
	public interface IEmailService
	{
		Task SendOtpAsync(string recipientEmail, string otpCode, DateTimeOffset expirationTimeUtc);
		Task SendLoginOtpAsync(string recipientEmail, string otpCode, DateTimeOffset expirationTimeUtc);
	}

	public sealed class EmailService(
		IOptions<GmailSmtpOptions> smtpOptionsAccessor,
		IConfiguration configuration,
		ILogger<EmailService> logger) : IEmailService
	{
		private readonly GmailSmtpOptions _smtpOptions = smtpOptionsAccessor.Value;
		private readonly IConfiguration _configuration = configuration;
		private readonly ILogger<EmailService> _logger = logger;

		public async Task SendOtpAsync(string recipientEmail, string otpCode, DateTimeOffset expirationTimeUtc)
		{
			await SendOtpCoreAsync(
				recipientEmail,
				otpCode,
				expirationTimeUtc,
				"CompuGear Password Recovery OTP",
				BuildPasswordResetOtpEmailHtml);
		}

		public async Task SendLoginOtpAsync(string recipientEmail, string otpCode, DateTimeOffset expirationTimeUtc)
		{
			await SendOtpCoreAsync(
				recipientEmail,
				otpCode,
				expirationTimeUtc,
				"CompuGear Login Verification OTP",
				BuildLoginOtpEmailHtml);
		}

		private async Task SendOtpCoreAsync(
			string recipientEmail,
			string otpCode,
			DateTimeOffset expirationTimeUtc,
			string subject,
			Func<string, DateTimeOffset, string> emailBodyBuilder)
		{
			var config = ValidateAndGetConfiguration();
			var recipient = (recipientEmail ?? string.Empty).Trim();

			if (string.IsNullOrWhiteSpace(recipient))
			{
				throw new InvalidOperationException("Recipient email is invalid.");
			}

			using var message = new MailMessage
			{
				From = new MailAddress(config.FromEmail, config.DisplayName),
				Subject = subject,
				IsBodyHtml = true,
				Body = emailBodyBuilder(otpCode, expirationTimeUtc)
			};

			message.To.Add(recipient);

			using var smtpClient = new SmtpClient(config.Host, config.Port)
			{
				EnableSsl = true,
				UseDefaultCredentials = false,
				DeliveryMethod = SmtpDeliveryMethod.Network,
				Credentials = new NetworkCredential(config.FromEmail, config.AppPassword),
				Timeout = 15000
			};

			try
			{
				await smtpClient.SendMailAsync(message);
			}
			catch (SmtpException ex)
			{
				_logger.LogError(ex, "SMTP OTP email sending failed. StatusCode: {StatusCode}", ex.StatusCode);

				var friendlyMessage = ex.StatusCode switch
				{
					SmtpStatusCode.ClientNotPermitted or SmtpStatusCode.MustIssueStartTlsFirst =>
						"SMTP authentication failed. Check Gmail address, App Password, and 2-Step Verification.",
					_ => "Unable to connect to Gmail SMTP right now. Please try again."
				};

				throw new InvalidOperationException(friendlyMessage);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Unexpected OTP email sending failure.");
				throw new InvalidOperationException("Unable to send OTP email right now. Please try again.");
			}
		}

		private ResolvedSmtpConfiguration ValidateAndGetConfiguration()
		{
			var host = string.IsNullOrWhiteSpace(_smtpOptions.Host)
				? "smtp.gmail.com"
				: _smtpOptions.Host.Trim();

			var port = _smtpOptions.Port <= 0 ? 587 : _smtpOptions.Port;
			var fromEmail = ResolveFromEmail();
			var appPassword = ResolveAppPassword();
			var displayName = string.IsNullOrWhiteSpace(_smtpOptions.DisplayName)
				? "CompuGear Security"
				: _smtpOptions.DisplayName.Trim();

			if (IsPlaceholder(fromEmail) || IsPlaceholder(appPassword))
			{
				throw new InvalidOperationException(
					"Gmail SMTP is not configured. Set GmailSmtp:FromEmail and GmailSmtp:AppPassword in appsettings/user-secrets, or set environment variables like GmailSmtp__FromEmail/GmailSmtp__AppPassword (ASP.NET Core style) or GMAIL_SMTP_FROM_EMAIL/GMAIL_SMTP_APP_PASSWORD.");
			}

			return new ResolvedSmtpConfiguration
			{
				Host = host,
				Port = port,
				FromEmail = fromEmail,
				AppPassword = appPassword,
				DisplayName = displayName
			};
		}

		private string ResolveFromEmail()
		{
			var fromEmail = (_smtpOptions.FromEmail ?? string.Empty).Trim();
			if (!IsPlaceholder(fromEmail))
			{
				return fromEmail;
			}

			fromEmail = ResolveFromConfiguration(
				NormalizePlainText,
				"GmailSmtp:FromEmail",
				"GmailSmtp__FromEmail",
				"GMAIL_SMTP_FROM_EMAIL");
			if (!IsPlaceholder(fromEmail))
			{
				return fromEmail;
			}

			return ResolveFromEnvironment(
				NormalizePlainText,
				"GmailSmtp__FromEmail",
				"GmailSmtp:FromEmail",
				"GMAIL_SMTP_FROM_EMAIL");
		}

		private string ResolveAppPassword()
		{
			var appPassword = NormalizeSecret(_smtpOptions.AppPassword);
			if (!IsPlaceholder(appPassword))
			{
				return appPassword;
			}

			appPassword = ResolveFromConfiguration(
				NormalizeSecret,
				"GmailSmtp:AppPassword",
				"GmailSmtp__AppPassword",
				"GMAIL_SMTP_APP_PASSWORD",
				"GMAIL_SMTP_PASSWORD");
			if (!IsPlaceholder(appPassword))
			{
				return appPassword;
			}

			return ResolveFromEnvironment(
				NormalizeSecret,
				"GmailSmtp__AppPassword",
				"GmailSmtp:AppPassword",
				"GMAIL_SMTP_APP_PASSWORD",
				"GMAIL_SMTP_PASSWORD");
		}

		private string ResolveFromConfiguration(Func<string?, string> normalize, params string[] keys)
		{
			foreach (var key in keys)
			{
				var value = normalize(_configuration[key]);
				if (!IsPlaceholder(value))
				{
					return value;
				}
			}

			return string.Empty;
		}

		private static string ResolveFromEnvironment(Func<string?, string> normalize, params string[] names)
		{
			foreach (var name in names)
			{
				var value = normalize(Environment.GetEnvironmentVariable(name));
				if (!IsPlaceholder(value))
				{
					return value;
				}
			}

			return string.Empty;
		}

		private static string BuildPasswordResetOtpEmailHtml(string otpCode, DateTimeOffset expirationTimeUtc)
		{
			var expiresLocalText = expirationTimeUtc.ToLocalTime().ToString("yyyy-MM-dd hh:mm tt");
			var safeOtp = WebUtility.HtmlEncode(otpCode);

			return $@"
				<div style='font-family:Segoe UI,Arial,sans-serif;line-height:1.5;color:#1f2937;'>
					<h2 style='margin-bottom:8px;'>CompuGear Password Recovery</h2>
					<p>You requested a password reset. Use this one-time password (OTP):</p>
					<p style='font-size:30px;font-weight:700;letter-spacing:4px;margin:16px 0;color:#0f766e;'>{safeOtp}</p>
					<p>This OTP expires in <strong>5 minutes</strong> (until {expiresLocalText}).</p>
					<p>If you did not request this, please ignore this email.</p>
					<hr style='border:none;border-top:1px solid #e5e7eb;margin:20px 0;' />
					<p style='font-size:12px;color:#6b7280;'>For your security, do not share this code with anyone.</p>
				</div>";
		}

		private static string BuildLoginOtpEmailHtml(string otpCode, DateTimeOffset expirationTimeUtc)
		{
			var expiresLocalText = expirationTimeUtc.ToLocalTime().ToString("yyyy-MM-dd hh:mm tt");
			var safeOtp = WebUtility.HtmlEncode(otpCode);

			return $@"
				<div style='font-family:Segoe UI,Arial,sans-serif;line-height:1.5;color:#1f2937;'>
					<h2 style='margin-bottom:8px;'>CompuGear Login Verification</h2>
					<p>A login attempt was made on your account. Enter this one-time password (OTP) to continue:</p>
					<p style='font-size:30px;font-weight:700;letter-spacing:4px;margin:16px 0;color:#0f766e;'>{safeOtp}</p>
					<p>This OTP expires in <strong>10 minutes</strong> (until {expiresLocalText}).</p>
					<p>If this wasn't you, change your password immediately and contact support.</p>
					<hr style='border:none;border-top:1px solid #e5e7eb;margin:20px 0;' />
					<p style='font-size:12px;color:#6b7280;'>For your security, do not share this code with anyone.</p>
				</div>";
		}

		private static bool IsPlaceholder(string? value)
		{
			var normalized = (value ?? string.Empty).Trim();
			if (string.IsNullOrWhiteSpace(normalized))
			{
				return true;
			}

			return normalized.Equals("yourgmail@gmail.com", StringComparison.OrdinalIgnoreCase)
				|| normalized.Equals("your-16-char-app-password", StringComparison.OrdinalIgnoreCase);
		}

		private static string NormalizeSecret(string? secret)
		{
			return (secret ?? string.Empty).Replace(" ", string.Empty).Trim();
		}

		private static string NormalizePlainText(string? value)
		{
			return (value ?? string.Empty).Trim();
		}
	}

	internal sealed class ResolvedSmtpConfiguration
	{
		public string Host { get; init; } = "smtp.gmail.com";
		public int Port { get; init; } = 587;
		public string FromEmail { get; init; } = string.Empty;
		public string AppPassword { get; init; } = string.Empty;
		public string DisplayName { get; init; } = "CompuGear Security";
	}

	public sealed class GmailSmtpOptions
	{
		public string Host { get; set; } = "smtp.gmail.com";
		public int Port { get; set; } = 587;
		public string FromEmail { get; set; } = string.Empty;
		public string AppPassword { get; set; } = string.Empty;
		public string DisplayName { get; set; } = "CompuGear Security";
	}
}
