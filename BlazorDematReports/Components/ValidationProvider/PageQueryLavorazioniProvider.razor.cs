using BlazorDematReports.Dto;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;


namespace BlazorDematReports.ValidationProvider
{
    public class PageQueryLavorazioniProvider : ComponentBase
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

                var queryProcedureLavorazioniDto = editContext.Model as QueryProcedureLavorazioniDto;


                if (queryProcedureLavorazioniDto!.NomeProcedura is null)
                {
                    messages.Add(editContext.Field("NomeProcedura"), "Lavorazione is required");
                }

                if (queryProcedureLavorazioniDto!.Titolo is null)
                {
                    messages.Add(editContext.Field("Query"), "Titolo is required");
                }

                if (queryProcedureLavorazioniDto!.Descrizione is null)
                {
                    messages.Add(editContext.Field("Descrizione"), "Descrizione is required");
                }



            }
        }


    }
}
