using BlazorDematReports.Dto;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;


namespace BlazorDematReports.ValidationProvider
{
    public class PageOperatoriValidationProvider : ComponentBase
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

                var OperatoriDto = editContext.Model as OperatoriDto;

                if (OperatoriDto!.Operatore is null)
                {
                    messages.Add(editContext.Field("Operatore"), "Operatore is required");
                }

                if (OperatoriDto!.IdRuolo is null)
                {
                    messages.Add(editContext.Field("Ruolo"), "Ruolo is required");
                }

                if (OperatoriDto!.Azienda is null)
                {
                    messages.Add(editContext.Field("Azienda"), "Azienda is required");
                }

                if (OperatoriDto!.Idcentro is null)
                {
                    messages.Add(editContext.Field("Idcentro"), "Centro is required");
                }

                if (OperatoriDto!.CentriVisibiliDto != null)
                {
                    if (OperatoriDto!.CentriVisibiliDto.All(x => x.FlagVisibile == false))
                    {
                        messages.Add(editContext.Field("FlagVisibile"), "FlagVisibile is required");
                    }
                }

            }
        }


    }
}
