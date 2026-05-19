using System.Security.Cryptography;
using System.Text;

namespace CompuGear.Services
{
    public interface IPasswordSecurityService
    {
        string HashPassword(string password);
        PasswordVerificationOutcome VerifyPassword(string password, string storedHash, string? legacySalt);
        bool IsStrongPassword(string password, out string errorMessage);
    }

    public readonly record struct PasswordVerificationOutcome(bool Success, bool NeedsRehash);

    public sealed class PasswordSecurityService : IPasswordSecurityService
    {
        private const int CurrentIterations = 120_000;
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const string Prefix = "PBKDF2";

        public string HashPassword(string password)
        {
            var saltBytes = RandomNumberGenerator.GetBytes(SaltSize);
            var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
                password,
                saltBytes,
                CurrentIterations,
                HashAlgorithmName.SHA256,
                KeySize);

            return $"{Prefix}${CurrentIterations}${Convert.ToBase64String(saltBytes)}${Convert.ToBase64String(hashBytes)}";
        }

        public PasswordVerificationOutcome VerifyPassword(string password, string storedHash, string? legacySalt)
        {
            if (TryParsePbkdf2(storedHash, out var iterations, out var saltBytes, out var storedHashBytes))
            {
                var derivedHash = Rfc2898DeriveBytes.Pbkdf2(
                    password,
                    saltBytes,
                    iterations,
                    HashAlgorithmName.SHA256,
                    storedHashBytes.Length);

                var success = CryptographicOperations.FixedTimeEquals(derivedHash, storedHashBytes);
                return new PasswordVerificationOutcome(success, success && iterations < CurrentIterations);
            }

            if (string.IsNullOrWhiteSpace(legacySalt))
            {
                return new PasswordVerificationOutcome(false, false);
            }

            if (!TryDecodeBase64(storedHash, out var legacyStoredHashBytes))
            {
                return new PasswordVerificationOutcome(false, false);
            }

            var legacyComputedHash = SHA256.HashData(Encoding.UTF8.GetBytes(password + legacySalt));
            var isLegacyMatch = CryptographicOperations.FixedTimeEquals(legacyComputedHash, legacyStoredHashBytes);
            return new PasswordVerificationOutcome(isLegacyMatch, isLegacyMatch);
        }

        public bool IsStrongPassword(string password, out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 12)
            {
                errorMessage = "Password must be at least 12 characters long.";
                return false;
            }

            if (!password.Any(char.IsUpper))
            {
                errorMessage = "Password must contain at least one uppercase letter.";
                return false;
            }

            if (!password.Any(char.IsLower))
            {
                errorMessage = "Password must contain at least one lowercase letter.";
                return false;
            }

            if (!password.Any(char.IsDigit))
            {
                errorMessage = "Password must contain at least one number.";
                return false;
            }

            if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
            {
                errorMessage = "Password must contain at least one special character.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        private static bool TryParsePbkdf2(string value, out int iterations, out byte[] saltBytes, out byte[] hashBytes)
        {
            iterations = 0;
            saltBytes = [];
            hashBytes = [];

            var parts = (value ?? string.Empty).Split('$', StringSplitOptions.None);
            if (parts.Length != 4 || !string.Equals(parts[0], Prefix, StringComparison.Ordinal))
            {
                return false;
            }

            if (!int.TryParse(parts[1], out iterations) || iterations <= 0)
            {
                return false;
            }

            if (!TryDecodeBase64(parts[2], out saltBytes) || saltBytes.Length == 0)
            {
                return false;
            }

            if (!TryDecodeBase64(parts[3], out hashBytes) || hashBytes.Length == 0)
            {
                return false;
            }

            return true;
        }

        private static bool TryDecodeBase64(string base64, out byte[] bytes)
        {
            try
            {
                bytes = Convert.FromBase64String(base64);
                return true;
            }
            catch
            {
                bytes = [];
                return false;
            }
        }
    }
}
