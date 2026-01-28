using BlazorDematReports.Dto;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;


namespace BlazorDematReports.ValidationProvider
{
    public class PageNormalizzazioneOperatoreValidationProvider : ComponentBase
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

                var operatoriNormalizzatiDto = editContext.Model as OperatoriNormalizzatiDto;

                if (operatoriNormalizzatiDto!.OperatoreNormalizzato is null)
                {
                    messages.Add(editContext.Field("OperatoreNormalizzato"), "OperatoreNormalizzato is required");
                }

                if (operatoriNormalizzatiDto!.OperatoreDaNormalizzare is null)
                {
                    messages.Add(editContext.Field("OperatoreDaNormalizzare"), "OperatoreDaNormalizzare is required");
                }

            }
        }


    }
}
