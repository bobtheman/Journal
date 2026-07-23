using System.Linq;

namespace Journal.Services
{
    public static class PasswordPolicy
    {
        public const int MinLength = 8;

        public static bool IsValid(string password) => GetValidationError(password) is null;

        public static string? GetValidationError(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < MinLength)
            {
                return $"Password must be at least {MinLength} characters.";
            }

            if (!password.Any(char.IsUpper))
            {
                return "Password must contain an uppercase letter.";
            }

            if (!password.Any(char.IsLower))
            {
                return "Password must contain a lowercase letter.";
            }

            if (!password.Any(char.IsDigit))
            {
                return "Password must contain a number.";
            }

            if (!password.Any(c => !char.IsLetterOrDigit(c)))
            {
                return "Password must contain a special character.";
            }

            return null;
        }
    }
}
