namespace Journal.Services
{
    public static class TokenExpiryHelper
    {
        private static readonly TimeSpan RefreshBuffer = TimeSpan.FromMinutes(2);

        public static bool IsExpiredOrExpiring(DateTime expiresAtUtc)
        {
            return DateTime.UtcNow >= expiresAtUtc - RefreshBuffer;
        }
    }
}
