using BlazorDematReports.Dto;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;


namespace BlazorDematReports.ValidationProvider
{
    public class PageTaskValidationProvider : ComponentBase
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

                var SearchDataDto = editContext.Model as SearchDataDto;

                if (SearchDataDto!.Idcentro is null)
                {
                    messages.Add(editContext.Field("Idcentro"), "Centro is required");
                }

                if (SearchDataDto!.NomeProcedura is null)
                {
                    messages.Add(editContext.Field("NomeProcedura"), "Lavorazione is required");
                }

                if (SearchDataDto!.Fase is null)
                {
                    messages.Add(editContext.Field("Fase"), "Fase is required");
                }

                if (SearchDataDto!.StartDate is null)
                {
                    messages.Add(editContext.Field("StartDate"), "StartDate is required");
                }

                if (SearchDataDto!.EndDate is null)
                {
                    messages.Add(editContext.Field("EndDate"), "EndDate is required");
                }



            }
        }


    }
}
