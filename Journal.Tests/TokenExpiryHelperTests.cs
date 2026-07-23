using Journal.Services;
using Xunit;

namespace Journal.Tests
{
    public class TokenExpiryHelperTests
    {
        [Fact]
        public void IsExpiredOrExpiring_FarFutureExpiry_ReturnsFalse()
        {
            var expiresAt = DateTime.UtcNow.AddHours(1);

            Assert.False(TokenExpiryHelper.IsExpiredOrExpiring(expiresAt));
        }

        [Fact]
        public void IsExpiredOrExpiring_AlreadyPast_ReturnsTrue()
        {
            var expiresAt = DateTime.UtcNow.AddMinutes(-1);

            Assert.True(TokenExpiryHelper.IsExpiredOrExpiring(expiresAt));
        }

        [Fact]
        public void IsExpiredOrExpiring_WithinRefreshBuffer_ReturnsTrue()
        {
            var expiresAt = DateTime.UtcNow.AddSeconds(30);

            Assert.True(TokenExpiryHelper.IsExpiredOrExpiring(expiresAt));
        }
    }
}
