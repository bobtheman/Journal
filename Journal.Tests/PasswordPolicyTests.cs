using Journal.Services;
using Xunit;

namespace Journal.Tests
{
    public class PasswordPolicyTests
    {
        [Theory]
        [InlineData("Sh0rt!")]
        [InlineData("nouppercase1!")]
        [InlineData("NOLOWERCASE1!")]
        [InlineData("NoDigitsHere!")]
        [InlineData("NoSpecial1Chars")]
        public void GetValidationError_WeakPassword_ReturnsMessage(string password)
        {
            Assert.NotNull(PasswordPolicy.GetValidationError(password));
        }

        [Fact]
        public void GetValidationError_StrongPassword_ReturnsNull()
        {
            Assert.Null(PasswordPolicy.GetValidationError("Correct1Horse!"));
        }

        [Fact]
        public void IsValid_StrongPassword_ReturnsTrue()
        {
            Assert.True(PasswordPolicy.IsValid("Correct1Horse!"));
        }

        [Fact]
        public void IsValid_EmptyPassword_ReturnsFalse()
        {
            Assert.False(PasswordPolicy.IsValid(string.Empty));
        }
    }
}
