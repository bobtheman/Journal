using Journal.Components.Shared;
using Journal.Models;
using Journal.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;

namespace Journal.Components.Pages
{
    public partial class JournalEntryDialog : ComponentBase
    {
        private const long MaxImageBytes = 15 * 1024 * 1024;
        private const int MaxImageDimension = 1600;

        [CascadingParameter]
        private IMudDialogInstance MudDialog { get; set; } = default!;

        [Parameter]
        public int? EntryId { get; set; }

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
        private ISnackbar Snackbar { get; set; } = default!;

        [Inject]
        private IJSRuntime JS { get; set; } = default!;

        [Inject]
        private ILoadingService LoadingService { get; set; } = default!;

        private string _title = string.Empty;
        private string _content = string.Empty;
        private int? _mood;
        private DateTime? _entryDate;
        private TimeSpan? _entryTime;
        private List<JournalEntryImage> _images = [];
        private bool _entryExists;
        private ElementReference _contentRef;

        private string _loadedTitle = string.Empty;
        private string _loadedContent = string.Empty;
        private int? _loadedMood;
        private DateTime? _loadedDate;
        private TimeSpan? _loadedTime;

        private DateTime? EntryDateTime => _entryDate.HasValue
            ? _entryDate.Value.Date + (_entryTime ?? TimeSpan.Zero)
            : null;

        private bool IsDirty =>
            _title != _loadedTitle ||
            _content != _loadedContent ||
            _mood != _loadedMood ||
            _entryDate != _loadedDate ||
            _entryTime != _loadedTime;

        protected override async Task OnInitializedAsync()
        {
            if (EntryId.HasValue)
            {
                await LoadEntryAsync(EntryId.Value);
            }
            else
            {
                _entryDate = DateTime.Today;
                _entryTime = DateTime.Now.TimeOfDay;
                _loadedDate = _entryDate;
                _loadedTime = _entryTime;
            }
        }

        private async Task LoadEntryAsync(int id)
        {
            var entry = await JournalRepository.GetByIdAsync(id);
            _entryExists = entry is not null;
            _title = entry?.Title ?? string.Empty;
            _content = entry?.Content ?? string.Empty;
            _mood = entry?.Mood;
            _entryDate = entry?.EntryDate.Date;
            _entryTime = entry?.EntryDate.TimeOfDay;
            _images = await JournalRepository.GetImagesAsync(id);

            _loadedTitle = _title;
            _loadedContent = _content;
            _loadedMood = _mood;
            _loadedDate = _entryDate;
            _loadedTime = _entryTime;
        }

        // Images are attached immediately (not staged with the rest of the form), so an
        // unsaved new entry needs a row to hang them off before the first one is added.
        private async Task<int> EnsureEntrySavedAsync()
        {
            if (EntryId.HasValue)
            {
                return EntryId.Value;
            }

            var entry = await JournalRepository.UpsertAsync(null, EntryDateTime ?? DateTime.Now, _title, _content, _mood);
            EntryId = entry.Id;
            _entryExists = true;
            _loadedTitle = _title;
            _loadedContent = _content;
            _loadedMood = _mood;
            _loadedDate = _entryDate;
            _loadedTime = _entryTime;
            return entry.Id;
        }

        private static string GetImageDataUri(JournalEntryImage image) =>
            $"data:{image.ImageMimeType};base64,{Convert.ToBase64String(image.ImageData)}";

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

        private void OnDateChanged(DateTime? date)
        {
            _entryDate = date;
        }

        private void OnTimeChanged(TimeSpan? time)
        {
            _entryTime = time;
        }

        private async Task OnImageSelectedAsync(InputFileChangeEventArgs e)
        {
            var file = e.File.RequestImageFileAsync("image/jpeg", MaxImageDimension, MaxImageDimension);
            IBrowserFile resized;
            try
            {
                resized = await file;
            }
            catch (Exception)
            {
                resized = e.File;
            }

            if (resized.Size > MaxImageBytes)
            {
                Snackbar.Add("Image is too large (max 15 MB).", Severity.Error);
                return;
            }

            await using var stream = resized.OpenReadStream(MaxImageBytes);
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);

            await AddImageAsync(buffer.ToArray(), resized.ContentType);
        }

        private async Task AddImageAsync(byte[] imageData, string imageMimeType)
        {
            using var loading = LoadingService.BeginLoading();
            await Task.Delay(1); 
            var entryId = await EnsureEntrySavedAsync();
            var image = await JournalRepository.AddImageAsync(entryId, imageData, imageMimeType);
            _images.Add(image);
        }

        private async Task TakePhotoAsync()
        {
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                Snackbar.Add("Camera capture isn't supported on this device.", Severity.Warning);
                return;
            }

            try
            {
                var options = new MediaPickerOptions
                {
                    MaximumWidth = MaxImageDimension,
                    MaximumHeight = MaxImageDimension,
                    CompressionQuality = 85,
                    RotateImage = true
                };
                var photo = await MediaPicker.Default.CapturePhotoAsync(options);
                if (photo is null)
                {
                    return;
                }

                await using var stream = await photo.OpenReadAsync();
                using var buffer = new MemoryStream();
                await stream.CopyToAsync(buffer);

                if (buffer.Length > MaxImageBytes)
                {
                    Snackbar.Add("Photo is too large (max 15 MB).", Severity.Error);
                    return;
                }

                await AddImageAsync(buffer.ToArray(), photo.ContentType);
            }
            catch (FeatureNotSupportedException)
            {
                Snackbar.Add("Camera capture isn't supported on this device.", Severity.Warning);
            }
            catch (PermissionException)
            {
                Snackbar.Add("Camera permission is required to take a photo.", Severity.Error);
            }
        }

        private async Task ViewImageAsync(JournalEntryImage image)
        {
            IDialogReference dialog;
            using (LoadingService.BeginLoading())
            {
                await Task.Delay(1); 
                var parameters = new DialogParameters
                {
                    [nameof(ImageViewerDialog.ImageDataUri)] = GetImageDataUri(image)
                };
                var options = new DialogOptions { FullScreen = true };
                dialog = await DialogService.ShowAsync<ImageViewerDialog>(string.Empty, parameters, options);
            }

            var result = await dialog.Result;
            if (result is { Canceled: false })
            {
                await JournalRepository.DeleteImageAsync(image.Id);
                _images.Remove(image);
            }
        }

        private async Task SaveAsync()
        {
            if (!EntryDateTime.HasValue)
            {
                return;
            }

            await JournalRepository.UpsertAsync(EntryId, EntryDateTime.Value, _title, _content, _mood);
            TriggerAutoSync();
            MudDialog.Close(DialogResult.Ok(true));
        }

        private async Task DeleteAsync()
        {
            if (!EntryId.HasValue)
            {
                return;
            }

            var parameters = new DialogParameters
            {
                [nameof(ConfirmDialog.Title)] = "Delete entry?",
                [nameof(ConfirmDialog.Message)] = $"This will permanently delete the entry from {EntryDateTime:d MMMM yyyy, h:mm tt}.",
                [nameof(ConfirmDialog.ConfirmText)] = "Delete"
            };
            var dialog = await DialogService.ShowAsync<ConfirmDialog>(string.Empty, parameters);
            var result = await dialog.Result;
            if (result is null || result.Canceled)
            {
                return;
            }

            await JournalRepository.DeleteAsync(EntryId.Value);
            TriggerAutoSync();
            MudDialog.Close(DialogResult.Ok(true));
        }

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
