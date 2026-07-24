using Journal.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Journal.Components.Layout
{
    public partial class MainLayout : LayoutComponentBase, IDisposable
    {
        public static readonly MudTheme AppTheme = new()
        {
            PaletteLight = new PaletteLight
            {
                Primary = "#4C5FD5",
                Secondary = "#8E7CC3",
                AppbarBackground = "#4C5FD5"
            },
            PaletteDark = new PaletteDark
            {
                Primary = "#8B9CFF",
                Secondary = "#B7A6E0"
            }
        };

        [Inject]
        private ISessionState SessionState { get; set; } = default!;

        [Inject]
        private ISyncNotificationService SyncNotificationService { get; set; } = default!;

        [Inject]
        private ISnackbar Snackbar { get; set; } = default!;

        [Inject]
        private ISettingsService SettingsService { get; set; } = default!;

        [Inject]
        private NavigationManager NavigationManager { get; set; } = default!;

        private bool _drawerOpen = true;

        protected override void OnInitialized()
        {
            SessionState.Changed += OnSessionChanged;
            SyncNotificationService.BackupCompleted += OnBackupCompleted;
        }

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                EnsureRoute();
            }
        }

        private void OnSessionChanged()
        {
            InvokeAsync(() =>
            {
                EnsureRoute();
                StateHasChanged();
            });
        }

        private void EnsureRoute()
        {
            if (!SessionState.IsAuthenticated)
            {
                NavigationManager.NavigateTo("login");
            }
        }

        // Fires from a background sync thread (see JournalEntryDialog.TriggerAutoSync),
        // so it has to be marshalled onto the render dispatcher via InvokeAsync.
        private void OnBackupCompleted()
        {
            if (!SettingsService.BackupNotificationsEnabled)
            {
                return;
            }

            InvokeAsync(() => Snackbar.Add("Backup complete.", Severity.Success));
        }

        public void Dispose()
        {
            SessionState.Changed -= OnSessionChanged;
            SyncNotificationService.BackupCompleted -= OnBackupCompleted;
        }
    }
}
