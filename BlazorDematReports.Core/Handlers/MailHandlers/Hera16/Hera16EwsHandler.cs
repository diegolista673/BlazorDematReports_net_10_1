using BlazorDematReports.Core.Constants;
using BlazorDematReports.Core.Lavorazioni.Interfaces;
using BlazorDematReports.Core.Lavorazioni.Models;
using BlazorDematReports.Core.Utility.Models;
using System.ComponentModel;

namespace BlazorDematReports.Core.Handlers.MailHandlers.Hera16
{
    /// <summary>
    /// Handler per l'importazione dati dal servizio Hera16 via Exchange Web Services.
    /// Gestisce la lettura e processamento delle email Hera16 inserendo in ProduzioneSistema.
    /// NOTA: La logica di elaborazione mail è gestita da job Hangfire dedicati.
    /// Questo handler è un placeholder per il registry system.
    /// </summary>
    [Description("Import dati HERA16 da allegati email CSV")]
    public sealed class Hera16EwsHandler : IProductionDataHandler
    {
        /// <inheritdoc />
        public string HandlerCode => LavorazioniCodes.HERA16;

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
        public Task<List<DatiLavorazione>> ExecuteAsync(ProductionExecutionContext context, CancellationToken ct = default)
        {
            // La logica di elaborazione mail HERA16 è gestita da job Hangfire dedicati
            // che accedono direttamente ai servizi mail tramite il service provider.
            // Questo handler ritorna lista vuota in quanto non gestisce direttamente l'elaborazione.
            return Task.FromResult(new List<DatiLavorazione>());
        }
    }
}
