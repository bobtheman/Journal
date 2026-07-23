using Journal.Services;
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
        private SessionState SessionState { get; set; } = default!;

        [Inject]
        private NavigationManager NavigationManager { get; set; } = default!;

        private bool _drawerOpen = true;

        protected override void OnInitialized()
        {
            SessionState.Changed += OnSessionChanged;
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

        public void Dispose()
        {
            SessionState.Changed -= OnSessionChanged;
        }
    }
}
