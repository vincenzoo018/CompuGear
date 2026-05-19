using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;

namespace CompuGear.Services
{
	public interface IOtpService
	{
		OtpIssueResult GenerateOtp(string email);
		OtpIssueResult ResendOtp(string email);
		OtpVerificationResult VerifyOtp(string email, string otpCode);
		OtpIssueResult GenerateLoginOtp(string email);
		OtpVerificationResult VerifyLoginOtp(string email, string otpCode);
		int GetResendCooldownSeconds(string email);
		bool CanResetPassword(string email);
		void ClearOtp(string email);
		void ConsumeResetAuthorization(string email);
		string NormalizeEmail(string email);
	}

	public sealed class OtpService(IMemoryCache memoryCache) : IOtpService
	{
		private readonly IMemoryCache _memoryCache = memoryCache;
		private readonly PasswordHasher<string> _passwordHasher = new();

		private const int MaxAttempts = 5;
		private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(10);
		private static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(45);
		private static readonly TimeSpan ResetAuthorizationLifetime = TimeSpan.FromMinutes(10);

		public OtpIssueResult GenerateOtp(string email)
		{
			return GenerateOtpInternal(email, OtpPurpose.PasswordReset);
		}

		public OtpIssueResult ResendOtp(string email)
		{
			return GenerateOtpInternal(email, OtpPurpose.PasswordReset);
		}

		public OtpVerificationResult VerifyOtp(string email, string otpCode)
		{
			return VerifyOtpInternal(email, otpCode, OtpPurpose.PasswordReset);
		}

		public OtpIssueResult GenerateLoginOtp(string email)
		{
			return GenerateOtpInternal(email, OtpPurpose.Login);
		}

		public OtpVerificationResult VerifyLoginOtp(string email, string otpCode)
		{
			return VerifyOtpInternal(email, otpCode, OtpPurpose.Login);
		}

		private OtpIssueResult GenerateOtpInternal(string email, OtpPurpose purpose)
		{
			var normalizedEmail = NormalizeEmail(email);
			if (string.IsNullOrWhiteSpace(normalizedEmail))
			{
				return new OtpIssueResult
				{
					Success = false,
					FailureReason = OtpIssueFailureReason.InvalidEmail,
					Message = "Invalid email address."
				};
			}

			var otpKey = BuildOtpCacheKey(normalizedEmail, purpose);
			if (_memoryCache.TryGetValue<OtpCacheRecord>(otpKey, out var existingRecord) && existingRecord != null)
			{
				var secondsRemaining = GetRemainingCooldownSeconds(existingRecord.LastSentAtUtc);
				if (secondsRemaining > 0)
				{
					return new OtpIssueResult
					{
						Success = false,
						FailureReason = OtpIssueFailureReason.Cooldown,
						CooldownSecondsRemaining = secondsRemaining,
						Message = $"Please wait {secondsRemaining} seconds before requesting another OTP."
					};
				}
			}

			return CreateAndStoreOtp(normalizedEmail, purpose);
		}

		private OtpVerificationResult VerifyOtpInternal(string email, string otpCode, OtpPurpose purpose)
		{
			var normalizedEmail = NormalizeEmail(email);
			if (string.IsNullOrWhiteSpace(normalizedEmail) || string.IsNullOrWhiteSpace(otpCode))
			{
				return new OtpVerificationResult
				{
					Success = false,
					FailureReason = OtpVerificationFailureReason.InvalidOtp,
					Message = "Invalid OTP request."
				};
			}

			var otpKey = BuildOtpCacheKey(normalizedEmail, purpose);
			if (!_memoryCache.TryGetValue<OtpCacheRecord>(otpKey, out var otpRecord) || otpRecord == null)
			{
				return new OtpVerificationResult
				{
					Success = false,
					FailureReason = OtpVerificationFailureReason.ExpiredOtp,
					Message = "OTP expired. Please request a new OTP."
				};
			}

			var now = DateTimeOffset.UtcNow;
			if (otpRecord.ExpirationTimeUtc <= now)
			{
				_memoryCache.Remove(otpKey);
				return new OtpVerificationResult
				{
					Success = false,
					FailureReason = OtpVerificationFailureReason.ExpiredOtp,
					Message = "OTP expired. Please request a new one."
				};
			}

			if (otpRecord.AttemptCount >= MaxAttempts)
			{
				_memoryCache.Remove(otpKey);
				return new OtpVerificationResult
				{
					Success = false,
					FailureReason = OtpVerificationFailureReason.TooManyAttempts,
					Message = "Too many failed attempts. Please request a new OTP."
				};
			}

			var verifyResult = _passwordHasher.VerifyHashedPassword(normalizedEmail, otpRecord.OtpHash, otpCode.Trim());
			if (verifyResult == PasswordVerificationResult.Success || verifyResult == PasswordVerificationResult.SuccessRehashNeeded)
			{
				_memoryCache.Remove(otpKey);

				if (purpose == OtpPurpose.PasswordReset)
				{
					var resetTicket = new ResetTicketCacheRecord
					{
						VerifiedAtUtc = now,
						ExpirationTimeUtc = now.Add(ResetAuthorizationLifetime)
					};

					_memoryCache.Set(
						BuildResetTicketCacheKey(normalizedEmail),
						resetTicket,
						new MemoryCacheEntryOptions
						{
							AbsoluteExpiration = resetTicket.ExpirationTimeUtc
						});
				}

				return new OtpVerificationResult
				{
					Success = true,
					FailureReason = OtpVerificationFailureReason.None,
					AttemptsRemaining = MaxAttempts,
					Message = "OTP verified successfully."
				};
			}

			otpRecord.AttemptCount += 1;
			var attemptsRemaining = MaxAttempts - otpRecord.AttemptCount;

			if (attemptsRemaining <= 0)
			{
				_memoryCache.Remove(otpKey);
				return new OtpVerificationResult
				{
					Success = false,
					FailureReason = OtpVerificationFailureReason.TooManyAttempts,
					Message = "Too many failed attempts. Please request a new OTP.",
					AttemptsRemaining = 0
				};
			}

			_memoryCache.Set(
				otpKey,
				otpRecord,
				new MemoryCacheEntryOptions
				{
					AbsoluteExpiration = otpRecord.ExpirationTimeUtc
				});

			return new OtpVerificationResult
			{
				Success = false,
				FailureReason = OtpVerificationFailureReason.InvalidOtp,
				AttemptsRemaining = attemptsRemaining,
				Message = $"Invalid OTP. You have {attemptsRemaining} attempt(s) remaining."
			};
		}

		public int GetResendCooldownSeconds(string email)
		{
			var normalizedEmail = NormalizeEmail(email);
			if (string.IsNullOrWhiteSpace(normalizedEmail))
			{
				return 0;
			}

			if (!_memoryCache.TryGetValue<OtpCacheRecord>(BuildOtpCacheKey(normalizedEmail, OtpPurpose.PasswordReset), out var otpRecord) || otpRecord == null)
			{
				return 0;
			}

			return GetRemainingCooldownSeconds(otpRecord.LastSentAtUtc);
		}

		public bool CanResetPassword(string email)
		{
			var normalizedEmail = NormalizeEmail(email);
			if (string.IsNullOrWhiteSpace(normalizedEmail))
			{
				return false;
			}

			var resetKey = BuildResetTicketCacheKey(normalizedEmail);
			if (!_memoryCache.TryGetValue<ResetTicketCacheRecord>(resetKey, out var ticket) || ticket == null)
			{
				return false;
			}

			if (ticket.ExpirationTimeUtc <= DateTimeOffset.UtcNow)
			{
				_memoryCache.Remove(resetKey);
				return false;
			}

			return true;
		}

		public void ClearOtp(string email)
		{
			var normalizedEmail = NormalizeEmail(email);
			if (string.IsNullOrWhiteSpace(normalizedEmail))
			{
				return;
			}

			_memoryCache.Remove(BuildOtpCacheKey(normalizedEmail, OtpPurpose.PasswordReset));
		}

		public void ConsumeResetAuthorization(string email)
		{
			var normalizedEmail = NormalizeEmail(email);
			if (string.IsNullOrWhiteSpace(normalizedEmail))
			{
				return;
			}

			_memoryCache.Remove(BuildOtpCacheKey(normalizedEmail, OtpPurpose.PasswordReset));
			_memoryCache.Remove(BuildOtpCacheKey(normalizedEmail, OtpPurpose.Login));
			_memoryCache.Remove(BuildResetTicketCacheKey(normalizedEmail));
		}

		public string NormalizeEmail(string email)
		{
			return (email ?? string.Empty).Trim().ToLowerInvariant();
		}

		private OtpIssueResult CreateAndStoreOtp(string normalizedEmail, OtpPurpose purpose)
		{
			var otpCode = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
			var now = DateTimeOffset.UtcNow;
			var expirationTime = now.Add(OtpLifetime);

			var otpRecord = new OtpCacheRecord
			{
				OtpHash = _passwordHasher.HashPassword(normalizedEmail, otpCode),
				ExpirationTimeUtc = expirationTime,
				AttemptCount = 0,
				LastSentAtUtc = now
			};

			_memoryCache.Set(
				BuildOtpCacheKey(normalizedEmail, purpose),
				otpRecord,
				new MemoryCacheEntryOptions
				{
					AbsoluteExpiration = otpRecord.ExpirationTimeUtc
				});

			if (purpose == OtpPurpose.PasswordReset)
			{
				_memoryCache.Remove(BuildResetTicketCacheKey(normalizedEmail));
			}

			return new OtpIssueResult
			{
				Success = true,
				FailureReason = OtpIssueFailureReason.None,
				OtpCode = otpCode,
				ExpirationTimeUtc = expirationTime,
				Message = "OTP generated successfully."
			};
		}

		private static int GetRemainingCooldownSeconds(DateTimeOffset lastSentAt)
		{
			var remaining = lastSentAt.Add(ResendCooldown) - DateTimeOffset.UtcNow;
			return remaining.TotalSeconds > 0
				? (int)Math.Ceiling(remaining.TotalSeconds)
				: 0;
		}

		private static string BuildOtpCacheKey(string normalizedEmail, OtpPurpose purpose)
		{
			return $"otp:{purpose.ToString().ToLowerInvariant()}:{BuildStableHash(normalizedEmail)}";
		}

		private static string BuildResetTicketCacheKey(string normalizedEmail)
		{
			return $"otp-reset:{BuildStableHash(normalizedEmail)}";
		}

		private static string BuildStableHash(string value)
		{
			var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
			return Convert.ToHexString(hash);
		}

		private sealed class OtpCacheRecord
		{
			public string OtpHash { get; init; } = string.Empty;
			public DateTimeOffset ExpirationTimeUtc { get; init; }
			public int AttemptCount { get; set; }
			public DateTimeOffset LastSentAtUtc { get; init; }
		}

		private sealed class ResetTicketCacheRecord
		{
			public DateTimeOffset VerifiedAtUtc { get; init; }
			public DateTimeOffset ExpirationTimeUtc { get; init; }
		}
	}

	public sealed class OtpIssueResult
	{
		public bool Success { get; init; }
		public OtpIssueFailureReason FailureReason { get; init; }
		public string Message { get; init; } = string.Empty;
		public string? OtpCode { get; init; }
		public DateTimeOffset? ExpirationTimeUtc { get; init; }
		public int CooldownSecondsRemaining { get; init; }
	}

	public sealed class OtpVerificationResult
	{
		public bool Success { get; init; }
		public OtpVerificationFailureReason FailureReason { get; init; }
		public string Message { get; init; } = string.Empty;
		public int AttemptsRemaining { get; init; }
	}

	public enum OtpIssueFailureReason
	{
		None,
		InvalidEmail,
		Cooldown
	}

	public enum OtpVerificationFailureReason
	{
		None,
		InvalidOtp,
		ExpiredOtp,
		TooManyAttempts
	}

	public enum OtpPurpose
	{
		PasswordReset,
		Login
	}
}
