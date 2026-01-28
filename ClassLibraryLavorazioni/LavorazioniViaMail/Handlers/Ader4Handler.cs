using LibraryLavorazioni.LavorazioniViaMail.Constants;
using LibraryLavorazioni.LavorazioniViaMail.Interfaces;
using LibraryLavorazioni.LavorazioniViaMail.Models;
using LibraryLavorazioni.LavorazioniViaMail.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryLavorazioni.LavorazioniViaMail.Handlers
{
    /// <summary>
    /// Handler per l'importazione dati dal servizio ADER4/Equitalia via Exchange Web Services.
    /// Gestisce email da Verona e Genova inserendo in ProduzioneSistema.
    /// </summary>
    public sealed class Ader4Handler : IMailImportHandler
    {
        /// <inheritdoc />
        public string ServiceCode => JobConstants.MailServiceCodes.Ader4;

        /// <inheritdoc />
        public Task<int> ExecuteAsync(IServiceProvider sp, MailImportExecutionContext ctx, CancellationToken ct)
            => sp.GetRequiredService<UnifiedMailProduzioneService>().ProcessAder4Async(ct);
    }
}
