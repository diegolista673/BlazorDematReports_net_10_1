using MudBlazor;



namespace BlazorDematReports.Components.Dialog
{
    public class NotificationDialog
    {
        IDialogService DialogService;

        public NotificationDialog(IDialogService DialogService)
        {
            this.DialogService = DialogService;
        }


        public async Task<DialogResult?> ShowNotification(string context, string buttonText, MudBlazor.Color color, string title)
        {
            var parameters1 = new DialogParameters();
            parameters1.Add("ContentText", context);
            parameters1.Add("ButtonText", buttonText);
            parameters1.Add("Color", color);

            var options1 = new DialogOptions() { CloseButton = true, MaxWidth = MaxWidth.Medium };
            var dialog = await DialogService!.ShowAsync<Dialog>(title, parameters1, options1);
            var result = await dialog.Result;

            return result;
        }

    }
}
