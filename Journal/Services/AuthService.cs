using Journal.Data;
using Journal.Services.Interfaces;
using Plugin.Maui.Biometric;
using SQLite;

namespace Journal.Services
{
    public class AuthService : IAuthService
    {
        private const string SaltKey = "auth_salt";
        private const string UsernameKey = "auth_username";
        private const string BiometricPasswordKey = "auth_biometric_password";

        private readonly JournalDbContext _dbContext;
        private readonly SessionState _sessionState;

        public AuthService(JournalDbContext dbContext, SessionState sessionState)
        {
            _dbContext = dbContext;
            _sessionState = sessionState;
        }

        public bool HasAccount =>
            _dbContext.DatabaseFileExists && Preferences.Default.ContainsKey(SaltKey);

        public string? Username => Preferences.Default.Get(UsernameKey, (string?)null);

        public async Task SetupAsync(string username, string password)
        {
            var salt = PasswordKeyDerivation.GenerateSalt();
            var key = PasswordKeyDerivation.DeriveKeyHex(password, salt);

            Preferences.Default.Set(SaltKey, Convert.ToHexString(salt));
            Preferences.Default.Set(UsernameKey, username);

            await _dbContext.OpenAsync(key);
            _sessionState.SetAuthenticated(true);
        }

        public async Task<bool> LoginAsync(string password)
        {
            var saltHex = Preferences.Default.Get(SaltKey, string.Empty);
            if (string.IsNullOrEmpty(saltHex))
            {
                return false;
            }

            var salt = Convert.FromHexString(saltHex);
            var key = PasswordKeyDerivation.DeriveKeyHex(password, salt);

            try
            {
                await _dbContext.OpenAsync(key);
                _sessionState.SetAuthenticated(true);
                return true;
            }
            catch (SQLiteException)
            {
                return false;
            }
        }

        public async Task<bool> ChangePasswordAsync(string oldPassword, string newPassword)
        {
            var saltHex = Preferences.Default.Get(SaltKey, string.Empty);
            if (string.IsNullOrEmpty(saltHex))
            {
                return false;
            }

            var salt = Convert.FromHexString(saltHex);
            var oldKey = PasswordKeyDerivation.DeriveKeyHex(oldPassword, salt);

            try
            {
                await _dbContext.OpenAsync(oldKey);
            }
            catch (SQLiteException)
            {
                return false;
            }

            var newSalt = PasswordKeyDerivation.GenerateSalt();
            var newKey = PasswordKeyDerivation.DeriveKeyHex(newPassword, newSalt);

            await _dbContext.RekeyAsync(newKey);
            Preferences.Default.Set(SaltKey, Convert.ToHexString(newSalt));
            return true;
        }

        public async Task LogoutAsync()
        {
            await _dbContext.CloseAsync();
            _sessionState.SetAuthenticated(false);
        }

        public async Task<bool> IsBiometricAvailableAsync()
        {
            var status = await BiometricAuthenticationService.Default.GetAuthenticationStatusAsync(AuthenticatorStrength.Weak);
            return status == BiometricHwStatus.Success;
        }

        public async Task<bool> IsBiometricUnlockEnabledAsync()
        {
            var password = await SecureStorage.Default.GetAsync(BiometricPasswordKey);
            return !string.IsNullOrEmpty(password);
        }

        public async Task<bool> EnableBiometricUnlockAsync(string password)
        {
            var verified = await LoginAsync(password);
            if (!verified)
            {
                return false;
            }

            await SecureStorage.Default.SetAsync(BiometricPasswordKey, password);
            return true;
        }

        public Task DisableBiometricUnlockAsync()
        {
            SecureStorage.Default.Remove(BiometricPasswordKey);
            return Task.CompletedTask;
        }

        public async Task<bool> TryBiometricLoginAsync()
        {
            var password = await SecureStorage.Default.GetAsync(BiometricPasswordKey);
            if (string.IsNullOrEmpty(password))
            {
                return false;
            }

            var request = new AuthenticationRequest
            {
                Title = "Unlock Journal",
                Subtitle = "Use your fingerprint to unlock",
                NegativeText = "Cancel",
                AllowPasswordAuth = false
            };

            var response = await BiometricAuthenticationService.Default.AuthenticateAsync(request, CancellationToken.None);
            if (response.Status != BiometricResponseStatus.Success)
            {
                return false;
            }

            return await LoginAsync(password);
        }
    }
}
