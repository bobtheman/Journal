using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Journal.Components.Shared
{
    public partial class ConfirmDialog : ComponentBase
    {
        [CascadingParameter]
        private IMudDialogInstance MudDialog { get; set; } = default!;

        [Parameter]
        public string Title { get; set; } = string.Empty;

        [Parameter]
        public string Message { get; set; } = string.Empty;

        [Parameter]
        public string ConfirmText { get; set; } = "Confirm";

        [Parameter]
        public string CancelText { get; set; } = "Cancel";

        [Parameter]
        public MudBlazor.Color Color { get; set; } = MudBlazor.Color.Error;

        private void Confirm()
        {
            MudDialog.Close(DialogResult.Ok(true));
        }

        private void Cancel()
        {
            MudDialog.Close(DialogResult.Cancel());
        }
    }
}
