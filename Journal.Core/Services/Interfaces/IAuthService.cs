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

        Task<bool> IsBiometricAvailableAsync();

        Task<bool> IsBiometricUnlockEnabledAsync();

        Task<bool> EnableBiometricUnlockAsync(string password);

        Task DisableBiometricUnlockAsync();

        Task<bool> TryBiometricLoginAsync();
    }
}
