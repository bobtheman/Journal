using Journal.Components.Shared;
using Journal.Services;
using Journal.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Journal.Components.Pages
{
    public partial class Login : ComponentBase
    {
        [Inject]
        private IAuthService AuthService { get; set; } = default!;

        [Inject]
        private ISessionState SessionState { get; set; } = default!;

        [Inject]
        private NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        private IDialogService DialogService { get; set; } = default!;

        private bool _hasAccount;
        private string _username = string.Empty;
        private string _password = string.Empty;
        private string _confirmPassword = string.Empty;
        private string? _error;
        private bool _busy;
        private bool _showPassword;
        private bool _showConfirmPassword;
        private bool _biometricAvailable;
        private bool _biometricPromptedOnLoad;

        protected override async Task OnInitializedAsync()
        {
            if (SessionState.IsAuthenticated)
            {
                NavigationManager.NavigateTo("");
                return;
            }

            _hasAccount = AuthService.HasAccount;
            if (_hasAccount)
            {
                _biometricAvailable = await AuthService.IsBiometricUnlockEnabledAsync()
                    && await AuthService.IsBiometricAvailableAsync();
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender && _biometricAvailable && !_biometricPromptedOnLoad)
            {
                _biometricPromptedOnLoad = true;
                await BiometricLoginAsync();
            }
        }

        private async Task OnKeyUp(KeyboardEventArgs args)
        {
            if (args.Key == "Enter")
            {
                await LoginAsync();
            }
        }

        private async Task LoginAsync()
        {
            _error = null;
            _busy = true;
            try
            {
                var success = await AuthService.LoginAsync(_password);
                if (!success)
                {
                    _error = "Incorrect password.";
                    return;
                }

                NavigationManager.NavigateTo("");
            }
            finally
            {
                _busy = false;
            }
        }

        private async Task BiometricLoginAsync()
        {
            _error = null;
            _busy = true;
            StateHasChanged();
            try
            {
                var success = await AuthService.TryBiometricLoginAsync();
                if (!success)
                {
                    _error = "Fingerprint authentication failed.";
                    return;
                }

                NavigationManager.NavigateTo("");
            }
            finally
            {
                _busy = false;
                StateHasChanged();
            }
        }

        private async Task OnSetupKeyUp(KeyboardEventArgs args)
        {
            if (args.Key == "Enter")
            {
                await SetupAsync();
            }
        }

        private async Task SetupAsync()
        {
            _error = null;

            if (string.IsNullOrWhiteSpace(_username))
            {
                _error = "Username is required.";
                return;
            }

            var validationError = PasswordPolicy.GetValidationError(_password);
            if (validationError is not null)
            {
                _error = validationError;
                return;
            }

            if (_password != _confirmPassword)
            {
                _error = "Passwords do not match.";
                return;
            }

            _busy = true;
            try
            {
                await AuthService.SetupAsync(_username, _password);

                if (await AuthService.IsBiometricAvailableAsync())
                {
                    var parameters = new DialogParameters
                    {
                        [nameof(ConfirmDialog.Title)] = "Enable fingerprint unlock?",
                        [nameof(ConfirmDialog.Message)] = "You can unlock Journal with your fingerprint instead of typing your password every time.",
                        [nameof(ConfirmDialog.ConfirmText)] = "Enable",
                        [nameof(ConfirmDialog.Color)] = MudBlazor.Color.Success
                    };
                    var dialog = await DialogService.ShowAsync<ConfirmDialog>(string.Empty, parameters);
                    var result = await dialog.Result;
                    if (result is { Canceled: false })
                    {
                        NavigationManager.NavigateTo("settings?enableBiometric=true");
                        return;
                    }
                }

                NavigationManager.NavigateTo("");
            }
            finally
            {
                _busy = false;
            }
        }
    }
}
