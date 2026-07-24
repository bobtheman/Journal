using Journal.Components.Shared;
using Journal.Models;
using Journal.Services;
using Journal.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Journal.Components.Pages
{
    public partial class Settings : ComponentBase
    {
        [Inject]
        private IGoogleDriveService GoogleDriveService { get; set; } = default!;

        [Inject]
        private ISettingsService SettingsService { get; set; } = default!;

        [Inject]
        private IAuthService AuthService { get; set; } = default!;

        [Inject]
        private IJournalRepository JournalRepository { get; set; } = default!;

        [Inject]
        private IDialogService DialogService { get; set; } = default!;

        [Inject]
        private NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        private IThemeService ThemeService { get; set; } = default!;

        [Inject]
        private IUpdateService UpdateService { get; set; } = default!;

        [Parameter]
        [SupplyParameterFromQuery(Name = "enableBiometric")]
        public bool EnableBiometricRequested { get; set; }

        private ThemeMode _themeMode;

        private bool _signedIn;
        private bool _autoSync;
        private bool _backupNotifications;
        private bool _busy;
        private string? _status;
        private string _oldPassword = string.Empty;
        private string _newPassword = string.Empty;
        private string? _passwordError;
        private bool _showOldPassword;
        private bool _showNewPassword;
        private bool _biometricHwAvailable;
        private bool _biometricEnabled;
        private bool _showBiometricPasswordPrompt;
        private string _biometricPassword = string.Empty;
        private string? _biometricError;
        private bool _showBiometricPassword;
        private AppUpdateInfo? _updateAvailable;
        private bool _updateBusy;
        private string? _updateStatus;
        private double _downloadProgress;

        protected override async Task OnInitializedAsync()
        {
            _themeMode = ThemeService.Mode;
            _signedIn = await GoogleDriveService.IsSignedInAsync();
            _autoSync = SettingsService.AutoSyncEnabled;
            _backupNotifications = SettingsService.BackupNotificationsEnabled;
            _biometricHwAvailable = await AuthService.IsBiometricAvailableAsync();
            _biometricEnabled = await AuthService.IsBiometricUnlockEnabledAsync();

            if (EnableBiometricRequested && _biometricHwAvailable && !_biometricEnabled)
            {
                _showBiometricPasswordPrompt = true;
            }
        }

        private async Task SignInAsync()
        {
            _busy = true;
            _status = null;
            try
            {
                _signedIn = await GoogleDriveService.SignInAsync();
                _status = _signedIn ? "Signed in." : "Sign-in was cancelled or failed.";
            }
            finally
            {
                _busy = false;
            }
        }

        private async Task SignOutAsync()
        {
            var parameters = new DialogParameters
            {
                [nameof(ConfirmDialog.Title)] = "Sign out of Google Drive?",
                [nameof(ConfirmDialog.Message)] = "You'll need to sign in again to backup or restore.",
                [nameof(ConfirmDialog.ConfirmText)] = "Sign out"
            };
            var dialog = await DialogService.ShowAsync<ConfirmDialog>(string.Empty, parameters);
            var result = await dialog.Result;
            if (result is null || result.Canceled)
            {
                return;
            }

            await GoogleDriveService.SignOutAsync();
            _signedIn = false;
        }

        private async Task BackupAsync()
        {
            _busy = true;
            _status = null;
            try
            {
                await GoogleDriveService.BackupAsync();
                _status = "Backup complete.";
            }
            catch (Exception ex)
            {
                _status = $"Backup failed: {ex.Message}";
            }
            finally
            {
                _busy = false;
            }
        }

        private async Task RestoreAsync()
        {
            _busy = true;
            _status = null;
            try
            {
                var restored = await GoogleDriveService.RestoreLatestAsync();
                _status = restored ? "Restored latest backup. Please log in again." : "No backup found.";
                if (restored)
                {
                    await AuthService.LogoutAsync();
                }
            }
            catch (Exception ex)
            {
                _status = $"Restore failed: {ex.Message}";
            }
            finally
            {
                _busy = false;
            }
        }

        private void OnAutoSyncChanged(bool value)
        {
            _autoSync = value;
            SettingsService.AutoSyncEnabled = value;
        }

        private void OnBackupNotificationsChanged(bool value)
        {
            _backupNotifications = value;
            SettingsService.BackupNotificationsEnabled = value;
        }

        private async Task ChangePasswordAsync()
        {
            _passwordError = PasswordPolicy.GetValidationError(_newPassword);
            if (_passwordError is not null)
            {
                return;
            }

            var success = await AuthService.ChangePasswordAsync(_oldPassword, _newPassword);
            _passwordError = success ? null : "Current password is incorrect.";
            if (success)
            {
                _oldPassword = string.Empty;
                _newPassword = string.Empty;
            }
        }

        private async Task OnBiometricSwitchChanged(bool value)
        {
            _biometricError = null;

            if (value)
            {
                _showBiometricPasswordPrompt = true;
                return;
            }

            await AuthService.DisableBiometricUnlockAsync();
            _biometricEnabled = false;
            _showBiometricPasswordPrompt = false;
            _showBiometricPassword = false;
        }

        private async Task OnBiometricPasswordKeyUp(KeyboardEventArgs args)
        {
            if (args.Key == "Enter")
            {
                await ConfirmEnableBiometricAsync();
            }
        }

        private async Task ConfirmEnableBiometricAsync()
        {
            var success = await AuthService.EnableBiometricUnlockAsync(_biometricPassword);
            _biometricPassword = string.Empty;

            if (!success)
            {
                _biometricError = "Current password is incorrect.";
                return;
            }

            _biometricEnabled = true;
            _showBiometricPasswordPrompt = false;
            _showBiometricPassword = false;
            _biometricError = null;
        }

        private void CancelEnableBiometric()
        {
            _biometricPassword = string.Empty;
            _showBiometricPasswordPrompt = false;
            _showBiometricPassword = false;
            _biometricError = null;
        }

        private void OnThemeModeChanged(ThemeMode value)
        {
            _themeMode = value;
            ThemeService.Mode = value;
        }

        private async Task Logout()
        {
            var parameters = new DialogParameters
            {
                [nameof(ConfirmDialog.Title)] = "Log out?",
                [nameof(ConfirmDialog.Message)] = "You'll need your password (or fingerprint) to unlock again.",
                [nameof(ConfirmDialog.ConfirmText)] = "Log out"
            };
            var dialog = await DialogService.ShowAsync<ConfirmDialog>(string.Empty, parameters);
            var result = await dialog.Result;
            if (result is null || result.Canceled)
            {
                return;
            }

            await AuthService.LogoutAsync();
            NavigationManager.NavigateTo("login");
        }

        private async Task DeleteAllDataAsync()
        {
            var parameters = new DialogParameters
            {
                [nameof(ConfirmDialog.Title)] = "Delete all data?",
                [nameof(ConfirmDialog.Message)] = "This will permanently delete every journal entry on this device. This cannot be undone.",
                [nameof(ConfirmDialog.ConfirmText)] = "Delete everything"
            };
            var dialog = await DialogService.ShowAsync<ConfirmDialog>(string.Empty, parameters);
            var result = await dialog.Result;
            if (result is null || result.Canceled)
            {
                return;
            }

            _busy = true;
            try
            {
                await JournalRepository.DeleteAllAsync();
                _status = "All journal entries deleted.";
            }
            finally
            {
                _busy = false;
            }
        }

        private async Task CheckForUpdateAsync()
        {
            _updateBusy = true;
            _updateStatus = null;
            try
            {
                _updateAvailable = await UpdateService.CheckForUpdateAsync();
                _updateStatus = _updateAvailable is null ? "You're up to date." : null;
            }
            catch (Exception ex)
            {
                _updateStatus = $"Update check failed: {ex.Message}";
            }
            finally
            {
                _updateBusy = false;
            }
        }

        private async Task InstallUpdateAsync()
        {
            if (_updateAvailable is null)
            {
                return;
            }

            _updateBusy = true;
            _updateStatus = null;
            var progress = new Progress<double>(value =>
            {
                _downloadProgress = value;
                StateHasChanged();
            });
            try
            {
                await UpdateService.DownloadAndInstallAsync(_updateAvailable, progress);
            }
            catch (Exception ex)
            {
                _updateStatus = $"Update failed: {ex.Message}";
            }
            finally
            {
                _updateBusy = false;
            }
        }
    }
}
