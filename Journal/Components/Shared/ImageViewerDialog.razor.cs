using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Journal.Components.Shared
{
    public partial class ImageViewerDialog : ComponentBase
    {
        [CascadingParameter]
        private IMudDialogInstance MudDialog { get; set; } = default!;

        [Parameter]
        public string ImageDataUri { get; set; } = string.Empty;

        [Inject]
        private IDialogService DialogService { get; set; } = default!;

        private async Task DeleteAsync()
        {
            var parameters = new DialogParameters
            {
                [nameof(ConfirmDialog.Title)] = "Delete image?",
                [nameof(ConfirmDialog.Message)] = "This will permanently delete this image from the entry.",
                [nameof(ConfirmDialog.ConfirmText)] = "Delete"
            };
            var dialog = await DialogService.ShowAsync<ConfirmDialog>(string.Empty, parameters);
            var result = await dialog.Result;
            if (result is null || result.Canceled)
            {
                return;
            }

            MudDialog.Close(DialogResult.Ok(true));
        }

        private void Close()
        {
            MudDialog.Close(DialogResult.Cancel());
        }
    }
}
