using LibraryLavorazioni.LavorazioniViaMail.Constants;
using LibraryLavorazioni.LavorazioniViaMail.Interfaces;
using LibraryLavorazioni.LavorazioniViaMail.Models;
using LibraryLavorazioni.LavorazioniViaMail.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryLavorazioni.LavorazioniViaMail.Handlers
{
    /// <summary>
    /// Handler per l'importazione dati dal servizio Hera16 via Exchange Web Services.
    /// Gestisce la lettura e processamento delle email Hera16 inserendo in ProduzioneSistema.
    /// </summary>
    public sealed class Hera16EwsHandler : IMailImportHandler
    {
        /// <inheritdoc />
        public string ServiceCode => JobConstants.MailServiceCodes.Hera16;

        /// <inheritdoc />
        public Task<int> ExecuteAsync(IServiceProvider sp, MailImportExecutionContext ctx, CancellationToken ct)
            => sp.GetRequiredService<UnifiedMailProduzioneService>().ProcessHera16Async(ct);
    }
}
