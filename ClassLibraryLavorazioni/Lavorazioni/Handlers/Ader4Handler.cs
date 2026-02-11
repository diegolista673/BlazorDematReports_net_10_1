using LibraryLavorazioni.Lavorazioni.Constants;
using LibraryLavorazioni.Lavorazioni.Interfaces;
using LibraryLavorazioni.Lavorazioni.Models;
using LibraryLavorazioni.Utility.Models;
using System.ComponentModel;

namespace LibraryLavorazioni.Lavorazioni.Handlers
{
    /// <summary>
    /// Handler per l'importazione dati dal servizio ADER4/Equitalia via Exchange Web Services.
    /// Gestisce email da Verona e Genova inserendo in ProduzioneSistema.
    /// NOTA: La logica di elaborazione mail è gestita da job Hangfire dedicati.
    /// Questo handler è un placeholder per il registry system.
    /// </summary>
    [Description("Import dati ADER4/Equitalia da allegati email CSV (Verona + Genova)")]
    public sealed class Ader4Handler : ILavorazioneHandler
    {
        /// <inheritdoc />
        public string LavorazioneCode => LavorazioniCodes.ADER4;

        /// <inheritdoc />
        public string? GetServiceCode() => LavorazioniCodes.ADER4;

        /// <inheritdoc />
        public HandlerMetadata GetMetadata() => new()
        {
            ServiceCode = LavorazioniCodes.ADER4,
            RequiresEmailService = true,
            Category = "Mail Import"
        };

        /// <inheritdoc />
        public Task<List<DatiLavorazione>> ExecuteAsync(LavorazioneExecutionContext context, CancellationToken ct = default)
        {
            // La logica di elaborazione mail ADER4 è gestita da job Hangfire dedicati
            // che accedono direttamente ai servizi mail tramite il service provider.
            // Questo handler ritorna lista vuota in quanto non gestisce direttamente l'elaborazione.
            return Task.FromResult(new List<DatiLavorazione>());
        }
    }
}
