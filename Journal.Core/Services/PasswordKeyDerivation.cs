using System.Security.Cryptography;

namespace Journal.Services
{
    /// <summary>Pure key-derivation logic, kept free of MAUI Essentials so it is unit-testable.</summary>
    public static class PasswordKeyDerivation
    {
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 200_000;

        public static byte[] GenerateSalt()
        {
            return RandomNumberGenerator.GetBytes(SaltSize);
        }

        public static string DeriveKeyHex(string password, byte[] salt)
        {
            var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
            return Convert.ToHexString(key).ToLowerInvariant();
        }
    }
}
