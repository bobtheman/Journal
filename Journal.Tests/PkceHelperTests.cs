using Journal.Services;
using Xunit;

namespace Journal.Tests
{
    public class PkceHelperTests
    {
        [Fact]
        public void GenerateCodeVerifier_ReturnsUrlSafeString()
        {
            var verifier = PkceHelper.GenerateCodeVerifier();

            Assert.True(verifier.Length is >= 43 and <= 128);
            Assert.DoesNotContain('+', verifier);
            Assert.DoesNotContain('/', verifier);
            Assert.DoesNotContain('=', verifier);
        }

        [Fact]
        public void GenerateCodeChallenge_SameVerifier_ReturnsSameChallenge()
        {
            var verifier = PkceHelper.GenerateCodeVerifier();

            var challenge1 = PkceHelper.GenerateCodeChallenge(verifier);
            var challenge2 = PkceHelper.GenerateCodeChallenge(verifier);

            Assert.Equal(challenge1, challenge2);
        }

        [Fact]
        public void GenerateCodeChallenge_DifferentVerifiers_ReturnsDifferentChallenges()
        {
            var challenge1 = PkceHelper.GenerateCodeChallenge(PkceHelper.GenerateCodeVerifier());
            var challenge2 = PkceHelper.GenerateCodeChallenge(PkceHelper.GenerateCodeVerifier());

            Assert.NotEqual(challenge1, challenge2);
        }
    }
}
