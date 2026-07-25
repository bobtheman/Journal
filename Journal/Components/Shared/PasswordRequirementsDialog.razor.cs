using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Journal.Components.Shared
{
    public partial class PasswordRequirementsDialog
    {
        [CascadingParameter]
        private IMudDialogInstance MudDialog { get; set; } = default!;

        private void Close() => MudDialog.Close();
    }
}