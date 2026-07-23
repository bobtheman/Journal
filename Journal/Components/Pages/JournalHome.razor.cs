using Journal.Models;
using Journal.Services;
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
        private SessionState SessionState { get; set; } = default!;

        [Inject]
        private IDialogService DialogService { get; set; } = default!;

        private DateTime? _selectedDate = DateTime.Today;
        private List<JournalEntrySummary> _summaries = [];

        private bool _selectedDateHasEntry =>
            _selectedDate is not null && _summaries.Any(s => s.EntryDate.Date == _selectedDate.Value.Date);

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

        private void OnDateChanged(DateTime? date)
        {
            _selectedDate = date;
        }

        private async Task AddEntryAsync()
        {
            await OpenEntryAsync(_selectedDate ?? DateTime.Today);
        }

        private async Task OpenEntryAsync(DateTime date)
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
