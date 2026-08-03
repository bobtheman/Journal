using Journal.Components.Shared;
using Journal.Models;
using Journal.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
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

        [Inject]
        private IJSRuntime JS { get; set; } = default!;

        [Inject]
        private ILoadingService LoadingService { get; set; } = default!;

        private List<JournalEntrySummary> _summaries = [];
        private Dictionary<DateTime, int> _dayMoodRollup = [];

        protected override async Task OnInitializedAsync()
        {
            if (!SessionState.IsAuthenticated)
            {
                // MainLayout redirects to /login once rendered; skip DB access until then.
                return;
            }

            await LoadSummariesAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await JS.InvokeVoidAsync("swipeList.initAll", ".journal-swipe-row");
        }

        private async Task LoadSummariesAsync()
        {
            _summaries = await JournalRepository.GetAllSummariesAsync();

            _dayMoodRollup = _summaries
                .GroupBy(s => s.EntryDate.Date)
                .Where(g => g.Any(s => s.Mood.HasValue))
                .ToDictionary(
                    g => g.Key,
                    g => MoodIcons.RoundToNearestBand(g.Where(s => s.Mood.HasValue).Average(s => s.Mood!.Value)));
        }

        private async Task AddEntryAsync()
        {
            await OpenEntryAsync(null);
        }

        private async Task OpenEntryAsync(int? id)
        {
            IDialogReference dialog;
            using (LoadingService.BeginLoading())
            {
                await Task.Delay(1); 
                var options = new DialogOptions { FullScreen = true, CloseButton = false };
                dialog = await DialogService.ShowAsync<JournalEntryDialog>(
                    string.Empty,
                    new DialogParameters { [nameof(JournalEntryDialog.EntryId)] = id },
                    options);
            }

            var result = await dialog.Result;
            if (result is { Canceled: false })
            {
                await LoadSummariesAsync();
            }
        }

        private async Task DeleteEntryAsync(JournalEntrySummary summary)
        {
            var parameters = new DialogParameters
            {
                [nameof(ConfirmDialog.Title)] = "Delete entry?",
                [nameof(ConfirmDialog.Message)] = $"This will permanently delete the entry from {summary.EntryDate:d MMMM yyyy, h:mm tt}.",
                [nameof(ConfirmDialog.ConfirmText)] = "Delete"
            };
            var dialog = await DialogService.ShowAsync<ConfirmDialog>(string.Empty, parameters);
            var result = await dialog.Result;
            if (result is null || result.Canceled)
            {
                await JS.InvokeVoidAsync("swipeList.resetAll", ".journal-swipe-row");
                return;
            }

            using (LoadingService.BeginLoading())
            {
                await Task.Delay(1); 
                await JournalRepository.DeleteAsync(summary.Id);
                await LoadSummariesAsync();
            }

            await JS.InvokeVoidAsync("swipeList.resetAll", ".journal-swipe-row");
        }
    }
}
