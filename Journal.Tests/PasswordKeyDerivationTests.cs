using Journal.Services;
using Xunit;

namespace Journal.Tests
{
    public class PasswordKeyDerivationTests
    {
        [Fact]
        public void DeriveKeyHex_SamePasswordAndSalt_ReturnsSameKey()
        {
            var salt = PasswordKeyDerivation.GenerateSalt();

            var key1 = PasswordKeyDerivation.DeriveKeyHex("correct horse battery staple", salt);
            var key2 = PasswordKeyDerivation.DeriveKeyHex("correct horse battery staple", salt);

            Assert.Equal(key1, key2);
        }

        [Fact]
        public void DeriveKeyHex_DifferentPasswords_ReturnsDifferentKeys()
        {
            var salt = PasswordKeyDerivation.GenerateSalt();

            var key1 = PasswordKeyDerivation.DeriveKeyHex("password-one", salt);
            var key2 = PasswordKeyDerivation.DeriveKeyHex("password-two", salt);

            Assert.NotEqual(key1, key2);
        }

        [Fact]
        public void DeriveKeyHex_DifferentSalts_ReturnsDifferentKeys()
        {
            var key1 = PasswordKeyDerivation.DeriveKeyHex("same-password", PasswordKeyDerivation.GenerateSalt());
            var key2 = PasswordKeyDerivation.DeriveKeyHex("same-password", PasswordKeyDerivation.GenerateSalt());

            Assert.NotEqual(key1, key2);
        }

        [Fact]
        public void GenerateSalt_ReturnsSixteenBytes()
        {
            var salt = PasswordKeyDerivation.GenerateSalt();

            Assert.Equal(16, salt.Length);
        }
    }
}
