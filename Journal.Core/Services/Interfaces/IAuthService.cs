namespace Journal.Services.Interfaces
{
    public interface IAuthService
    {
        bool HasAccount { get; }

        string? Username { get; }

        Task SetupAsync(string username, string password);

        Task<bool> LoginAsync(string password);

        Task<bool> ChangePasswordAsync(string oldPassword, string newPassword);

        Task LogoutAsync();

        // Wipes the database file plus every local preference/secure-storage value
        // (account credentials, biometric secret, Google tokens, app settings) and
        // logs the session out - a full local reset, not just clearing journal entries.
        Task DeleteAllLocalDataAsync();

        Task<bool> IsBiometricAvailableAsync();

        Task<bool> IsBiometricUnlockEnabledAsync();

        Task<bool> EnableBiometricUnlockAsync(string password);

        Task DisableBiometricUnlockAsync();

        Task<bool> TryBiometricLoginAsync();
    }
}
