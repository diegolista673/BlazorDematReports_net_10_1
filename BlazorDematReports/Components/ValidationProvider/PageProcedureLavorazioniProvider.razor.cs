using BlazorDematReports.Core.Application.Dto;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;


namespace BlazorDematReports.ValidationProvider
{
    public class PageProcedureLavorazioniProvider : ComponentBase
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

                var procedureLavorazioniDto = editContext.Model as ProcedureLavorazioniDto;


                if (string.IsNullOrEmpty(procedureLavorazioniDto!.NomeProcedura))
                {
                    messages.Add(editContext.Field("Lavorazione"), "Lavorazione is required");
                }

                if (procedureLavorazioniDto!.NomeProcedura is null)
                {
                    messages.Add(editContext.Field("Lavorazione"), "Lavorazione is required");
                }

                if (procedureLavorazioniDto!.Idcentro is null)
                {
                    messages.Add(editContext.Field("Idcentro"), "Centro is required");
                }

                if (procedureLavorazioniDto!.ProceduraCliente is null)
                {
                    messages.Add(editContext.Field("ProceduraCliente"), "ProceduraCliente is required");
                }

                if (procedureLavorazioniDto!.Reparto is null)
                {
                    messages.Add(editContext.Field("Reparto"), "Reparto is required");
                }

                if (procedureLavorazioniDto!.FormatoDatiProduzione is null)
                {
                    messages.Add(editContext.Field("FormatoDatiProduzione"), "Formato Dati Produzione is required");
                }

            }
        }


    }
}
