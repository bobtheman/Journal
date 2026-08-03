using Journal.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Journal.Components.Shared
{
    public partial class UpdateAvailableDialog : ComponentBase
    {
        [CascadingParameter]
        private IMudDialogInstance MudDialog { get; set; } = default!;

        [Parameter]
        public AppUpdateInfo Update { get; set; } = default!;

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
