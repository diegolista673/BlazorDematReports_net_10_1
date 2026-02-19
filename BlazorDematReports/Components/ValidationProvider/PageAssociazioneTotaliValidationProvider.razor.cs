using BlazorDematReports.Core.Application.Dto;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;


namespace BlazorDematReports.ValidationProvider
{
    public class PageAssociazioneTotaliValidationProvider : ComponentBase
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

                var lavorazioniFasiTipoTotaleDto = editContext.Model as LavorazioniFasiTipoTotaleDto;

                if (lavorazioniFasiTipoTotaleDto!.TipologiaTotale is null)
                {
                    messages.Add(editContext.Field("TipologiaTotale"), "TipologiaTotale is required");
                }

                if (lavorazioniFasiTipoTotaleDto!.NomeProcedura is null)
                {
                    messages.Add(editContext.Field("NomeProcedura"), "NomeProcedura is required");
                }

                if (lavorazioniFasiTipoTotaleDto!.Fase is null)
                {
                    messages.Add(editContext.Field("Fase"), "Fase is required");
                }


            }
        }


    }
}
