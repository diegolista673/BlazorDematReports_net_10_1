using BlazorDematReports.Core.Application.Dto;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Reflection.Metadata;

namespace BlazorDematReports.ValidationProvider
{
    /// <summary>
    /// Componente per la validazione personalizzata dei dati di produzione operatori.
    /// Utilizza un ValidationMessageStore per gestire i messaggi di errore.
    /// </summary>
    public class PageProduzioneOperatoriValidationProvider : ComponentBase
    {
        /// <summary>
        /// Contesto di modifica per il modulo.
        /// </summary>
        [CascadingParameter]
        public EditContext? EditContext { get; set; }

        /// <summary>
        /// Flag per verificare se è necessario controllare l'utenza alternativa.
        /// </summary>
        [Parameter]
        public bool? CheckAltraUtenza { get; set; }

        /// <summary>
        /// Metodo chiamato durante l'inizializzazione del componente.
        /// Configura la validazione personalizzata.
        /// </summary>
        protected override void OnInitialized()
        {
            var messages = new ValidationMessageStore(EditContext!);

            EditContext!.OnValidationRequested += (sender, eventArgs)
                => ValidateModel((EditContext)sender!, messages);

            EditContext!.OnFieldChanged += (sender, eventArgs)
                => ValidateModel((EditContext)sender!, messages);
        }

        /// <summary>
        /// Esegue la validazione personalizzata del modello.
        /// </summary>
        /// <param name="editContext">Contesto di modifica corrente.</param>
        /// <param name="messages">Store per i messaggi di validazione.</param>
        private void ValidateModel(EditContext editContext, ValidationMessageStore messages)
        {
            messages.Clear();

            if (editContext.Model != null)
            {
                var operatoriDto = editContext.Model as ProduzioneOperatoriDto;

                if (operatoriDto!.AltraUtenza is null & CheckAltraUtenza == true)
                {
                    messages.Add(editContext.Field("AltraUtenza"), "AltraUtenza is required");
                }

                if (operatoriDto!.IdOperatore is null)
                {
                    messages.Add(editContext.Field("Operatore"), "Operatore is required");
                }

                if (operatoriDto.CheckLavoratoConAltraUtenza == true & string.IsNullOrEmpty(operatoriDto.AltraUtenza))
                {
                    messages.Add(editContext.Field("AltraUtenza"), "AltraUtenza is required");
                }

                if (operatoriDto!.IdTurno is null)
                {
                    messages.Add(editContext.Field("Turno"), "Turno is required");
                }

                if (operatoriDto!.IdProceduraLavorazione is null)
                {
                    messages.Add(editContext.Field("Lavorazione"), "Lavorazione is required");
                }

                if (operatoriDto!.IdFaseLavorazione is null)
                {
                    messages.Add(editContext.Field("Fase"), "Fase is required");
                }

                if (operatoriDto!.IdReparti is null)
                {
                    messages.Add(editContext.Field("Reparto"), "Reparto is required");
                }

                if (operatoriDto.Minuti == 0 & operatoriDto.Ore == 0)
                {
                    messages.Add(editContext.Field("TempoLavOreCent"), "TempoLavOreCent is required");
                }



            }

            editContext.NotifyValidationStateChanged();
        }
    }
}

