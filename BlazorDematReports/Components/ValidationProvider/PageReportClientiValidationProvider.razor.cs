using BlazorDematReports.Dto;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;


namespace BlazorDematReports.ValidationProvider
{
    public class PageReportClientiValidationProvider : ComponentBase
    {
        [CascadingParameter]
        public EditContext? EditContext { get; set; }


        protected override void OnInitialized()
        {
            var messages = new ValidationMessageStore(EditContext!);

            EditContext!.OnValidationRequested += (sender, eventArgs)
                => ValidateModel((EditContext)sender!, messages);
        }


        private void ValidateModel(EditContext editContext, ValidationMessageStore messages)
        {
            messages.Clear();

            if (editContext.Model != null)
            {

                var reportAnnualeDto = editContext.Model as ReportAnnualeDto;

                if (reportAnnualeDto!.IdCentro is null)
                {
                    messages.Add(editContext.Field("IdCentro"), "Centro is required");
                }


            }
        }


    }
}
