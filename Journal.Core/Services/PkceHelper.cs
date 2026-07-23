using System.Security.Cryptography;
using System.Text;

namespace Journal.Services
{
    /// <summary>Pure PKCE verifier/challenge generation, kept MAUI-free for unit testing.</summary>
    public static class PkceHelper
    {
        public static string GenerateCodeVerifier()
        {
            return Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        }

        public static string GenerateCodeChallenge(string codeVerifier)
        {
            var hash = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
            return Base64UrlEncode(hash);
        }

        private static string Base64UrlEncode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }
}
