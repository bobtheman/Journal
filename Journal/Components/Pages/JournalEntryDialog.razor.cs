using Journal.Components.Shared;
using Journal.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Journal.Components.Pages
{
    public partial class JournalEntryDialog : ComponentBase
    {
        [CascadingParameter]
        private IMudDialogInstance MudDialog { get; set; } = default!;

        [Parameter]
        public DateTime? Date { get; set; }

        [Inject]
        private IJournalRepository JournalRepository { get; set; } = default!;

        [Inject]
        private IGoogleDriveService GoogleDriveService { get; set; } = default!;

        [Inject]
        private ISettingsService SettingsService { get; set; } = default!;

        [Inject]
        private ISyncNotificationService SyncNotificationService { get; set; } = default!;

        [Inject]
        private IDialogService DialogService { get; set; } = default!;

        [Inject]
        private IJSRuntime JS { get; set; } = default!;

        private string _title = string.Empty;
        private string _content = string.Empty;
        private int? _mood;
        private bool _entryExists;
        private ElementReference _contentRef;

        private string _loadedTitle = string.Empty;
        private string _loadedContent = string.Empty;
        private int? _loadedMood;

        private bool IsDirty => _title != _loadedTitle || _content != _loadedContent || _mood != _loadedMood;

        protected override async Task OnInitializedAsync()
        {
            if (Date.HasValue)
            {
                await LoadEntryForDateAsync(Date.Value);
            }
        }

        private async Task LoadEntryForDateAsync(DateTime date)
        {
            var entry = await JournalRepository.GetByDateAsync(date);
            _entryExists = entry is not null;
            _title = entry?.Title ?? string.Empty;
            _content = entry?.Content ?? string.Empty;
            _mood = entry?.Mood;
            _loadedTitle = _title;
            _loadedContent = _content;
            _loadedMood = _mood;
        }

        private async Task<bool> ConfirmDiscardIfDirtyAsync()
        {
            if (!IsDirty)
            {
                return true;
            }

            var parameters = new DialogParameters
            {
                [nameof(ConfirmDialog.Title)] = "Discard changes?",
                [nameof(ConfirmDialog.Message)] = "You have unsaved changes that will be lost.",
                [nameof(ConfirmDialog.ConfirmText)] = "Discard"
            };
            var dialog = await DialogService.ShowAsync<ConfirmDialog>(string.Empty, parameters);
            var result = await dialog.Result;
            return result is { Canceled: false };
        }

        private async Task OnDateChanged(DateTime? date)
        {
            if (!await ConfirmDiscardIfDirtyAsync())
            {
                return;
            }

            Date = date;
            if (date.HasValue)
            {
                await LoadEntryForDateAsync(date.Value);
            }
            else
            {
                _entryExists = false;
                _title = string.Empty;
                _content = string.Empty;
                _mood = null;
                _loadedTitle = string.Empty;
                _loadedContent = string.Empty;
                _loadedMood = null;
            }
        }

        private async Task SaveAsync()
        {
            if (!Date.HasValue)
            {
                return;
            }

            await JournalRepository.UpsertAsync(Date.Value, _title, _content, _mood);
            TriggerAutoSync();
            MudDialog.Close(DialogResult.Ok(true));
        }

        private async Task DeleteAsync()
        {
            if (!Date.HasValue)
            {
                return;
            }

            var parameters = new DialogParameters
            {
                [nameof(ConfirmDialog.Title)] = "Delete entry?",
                [nameof(ConfirmDialog.Message)] = $"This will permanently delete the entry for {Date:d MMMM yyyy}.",
                [nameof(ConfirmDialog.ConfirmText)] = "Delete"
            };
            var dialog = await DialogService.ShowAsync<ConfirmDialog>(string.Empty, parameters);
            var result = await dialog.Result;
            if (result is null || result.Canceled)
            {
                return;
            }

            await JournalRepository.DeleteAsync(Date.Value);
            TriggerAutoSync();
            MudDialog.Close(DialogResult.Ok(true));
        }

        // Fire-and-forget: auto backup shouldn't make the user wait on a network round trip
        // just to close the entry dialog, and a failed background sync isn't worth surfacing.
        private void TriggerAutoSync()
        {
            if (!SettingsService.AutoSyncEnabled)
            {
                return;
            }

            if (SettingsService.WifiOnlyBackup && !Connectivity.Current.ConnectionProfiles.Contains(ConnectionProfile.WiFi))
            {
                return;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    if (await GoogleDriveService.IsSignedInAsync())
                    {
                        await GoogleDriveService.BackupAsync();
                        SyncNotificationService.NotifyBackupCompleted();
                    }
                }
                catch (Exception)
                {
                    // Best-effort background sync; the user can always "Backup now" manually.
                }
            });
        }

        private async Task Cancel()
        {
            if (!await ConfirmDiscardIfDirtyAsync())
            {
                return;
            }

            MudDialog.Close(DialogResult.Cancel());
        }

        private async Task WrapSelectionAsync(string prefix, string suffix)
        {
            var selection = await JS.InvokeAsync<TextSelection>("textEditor.getSelection", _contentRef);
            var start = Math.Min(selection.Start, selection.End);
            var end = Math.Max(selection.Start, selection.End);
            var selectedText = _content[start..end];

            _content = _content[..start] + prefix + selectedText + suffix + _content[end..];
            StateHasChanged();
            await Task.Yield();

            var caret = selectedText.Length == 0
                ? start + prefix.Length
                : start + prefix.Length + selectedText.Length + suffix.Length;
            await JS.InvokeVoidAsync("textEditor.setSelection", _contentRef, caret, caret);
        }

        private async Task InsertBulletAsync()
        {
            var selection = await JS.InvokeAsync<TextSelection>("textEditor.getSelection", _contentRef);
            var caret = selection.Start;
            var lineStart = _content.LastIndexOf('\n', Math.Max(caret - 1, 0)) + 1;

            const string bullet = "- ";
            _content = _content[..lineStart] + bullet + _content[lineStart..];
            StateHasChanged();
            await Task.Yield();

            var newCaret = caret + bullet.Length;
            await JS.InvokeVoidAsync("textEditor.setSelection", _contentRef, newCaret, newCaret);
        }

        private class TextSelection
        {
            public int Start { get; set; }
            public int End { get; set; }
        }
    }
}
