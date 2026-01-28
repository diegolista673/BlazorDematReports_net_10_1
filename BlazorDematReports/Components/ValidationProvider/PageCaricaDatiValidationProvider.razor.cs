using BlazorDematReports.Dto;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;


namespace BlazorDematReports.ValidationProvider
{
    public class PageCaricaDatiValidationProvider : ComponentBase
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

                var produzioneSistemaDto = editContext.Model as ProduzioneSistemaDto;

                if (produzioneSistemaDto!.IdOperatore is null)
                {
                    messages.Add(editContext.Field("Operatore"), "Operatore is required");
                }


                if (produzioneSistemaDto!.IdProceduraLavorazione is null)
                {
                    messages.Add(editContext.Field("Lavorazione"), "Lavorazione is required");
                }


                if (produzioneSistemaDto!.IdFaseLavorazione is null)
                {
                    messages.Add(editContext.Field("Fase"), "Fase is required");
                }


                if (produzioneSistemaDto.Documenti == 0 & produzioneSistemaDto.Fogli == 0 & produzioneSistemaDto.Pagine == 0 & produzioneSistemaDto.Scarti == 0 & produzioneSistemaDto.PagineSenzaBianco == 0)
                {
                    messages.Add(editContext.Field("IdProduzioneSistema"), "totals are required");
                }

            }
        }


    }
}
