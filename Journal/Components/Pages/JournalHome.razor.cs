using Journal.Models;
using Journal.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Journal.Components.Pages
{
    public partial class JournalHome : ComponentBase
    {
        [Inject]
        private IJournalRepository JournalRepository { get; set; } = default!;

        [Inject]
        private ISessionState SessionState { get; set; } = default!;

        [Inject]
        private IDialogService DialogService { get; set; } = default!;

        private List<JournalEntrySummary> _summaries = [];

        protected override async Task OnInitializedAsync()
        {
            if (!SessionState.IsAuthenticated)
            {
                // MainLayout redirects to /login once rendered; skip DB access until then.
                return;
            }

            await LoadSummariesAsync();
        }

        private async Task LoadSummariesAsync()
        {
            _summaries = await JournalRepository.GetAllSummariesAsync();
        }

        private async Task AddEntryAsync()
        {
            await OpenEntryAsync(null);
        }

        private async Task OpenEntryAsync(DateTime? date)
        {
            var options = new DialogOptions { FullScreen = true, CloseButton = false };
            var dialog = await DialogService.ShowAsync<JournalEntryDialog>(
                string.Empty,
                new DialogParameters { [nameof(JournalEntryDialog.Date)] = date },
                options);

            var result = await dialog.Result;
            if (result is { Canceled: false })
            {
                await LoadSummariesAsync();
            }
        }
    }
}
