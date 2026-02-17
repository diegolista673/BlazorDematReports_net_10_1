using LibraryLavorazioni.Lavorazioni.Constants;
using LibraryLavorazioni.Lavorazioni.Interfaces;
using LibraryLavorazioni.Lavorazioni.Models;
using LibraryLavorazioni.Utility.Models;
using System.ComponentModel;

namespace LibraryLavorazioni.Lavorazioni.Handlers.MailHandlers.Hera16
{
    /// <summary>
    /// Handler per l'importazione dati dal servizio Hera16 via Exchange Web Services.
    /// Gestisce la lettura e processamento delle email Hera16 inserendo in ProduzioneSistema.
    /// NOTA: La logica di elaborazione mail è gestita da job Hangfire dedicati.
    /// Questo handler è un placeholder per il registry system.
    /// </summary>
    [Description("Import dati HERA16 da allegati email CSV")]
    public sealed class Hera16EwsHandler : ILavorazioneHandler
    {
        /// <inheritdoc />
        public string LavorazioneCode => LavorazioniCodes.HERA16;

        /// <inheritdoc />
        public string? GetServiceCode() => LavorazioniCodes.HERA16;

        /// <inheritdoc />
        public HandlerMetadata GetMetadata() => new()
        {
            ServiceCode = LavorazioniCodes.HERA16,
            RequiresEmailService = true,
            Category = "Mail Import"
        };

        /// <inheritdoc />
        public Task<List<DatiLavorazione>> ExecuteAsync(LavorazioneExecutionContext context, CancellationToken ct = default)
        {
            // La logica di elaborazione mail HERA16 è gestita da job Hangfire dedicati
            // che accedono direttamente ai servizi mail tramite il service provider.
            // Questo handler ritorna lista vuota in quanto non gestisce direttamente l'elaborazione.
            return Task.FromResult(new List<DatiLavorazione>());
        }
    }
}
