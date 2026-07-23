using Journal.Components.Shared;
using Journal.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Journal.Components.Layout
{
    public partial class NavMenu : ComponentBase
    {
        [Inject]
        private IAuthService AuthService { get; set; } = default!;

        [Inject]
        private NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        private IDialogService DialogService { get; set; } = default!;

        private string? Username => AuthService.Username;

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
    }
}
