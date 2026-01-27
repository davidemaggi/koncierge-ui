using KonciergeUi.Client.Components.Layout.Modals;
using MudBlazor;

namespace KonciergeUi.Client.Extensions;

public static class IDialogExtensions
{
    public static async Task<bool> ShowConfirmationAsync(this IDialogService dialog, 
        string title = "", string message = "", string confirmText = "", string cancelText = "")
    {
        var parameters = new DialogParameters<ConfirmationDialog>
        {
            { x => x.Title, title },
            { x => x.ContentText, message },
            { x => x.ConfirmButtonText, confirmText },
            { x => x.CancelButtonText, cancelText }
            // Drop OnConfirm—handle outside!
        };
    
        var dialogReference = await dialog.ShowAsync<ConfirmationDialog>("confirm", parameters);
        var dialogResult = await dialogReference.Result;  // CRITICAL: Await here!
        return dialogResult.Canceled ? false : true;  // Cleaner, explicit
    }

    public static async Task ShowConfirmationActionAsync(this IDialogService dialog, 
        Func<Task> onConfirm,  string title= "", string message= "",
        string confirmText = "", string cancelText = "")
    {
        var confirmed = await dialog.ShowConfirmationAsync(title, message, confirmText, cancelText);
        if (confirmed) {
            await onConfirm();  // Safe now—dialog fully closed
        }
    }

}

// Bool result
//var confirmed = await Dialog.ShowConfirmationAsync("Delete?", "Really delete?");
//if (confirmed) await DeleteAsync();

// Auto-action
//await Dialog.ShowConfirmationActionAsync("Delete?", "Forever?", DeleteAsync);