using LibraryLavorazioni.LavorazioniViaMail.Models;

namespace LibraryLavorazioni.LavorazioniViaMail.Interfaces
{
    /// <summary>
    /// Interfaccia per gli handler di importazione dati via mail.
    /// Ogni implementazione gestisce un servizio mail specifico.
    /// </summary>
    public interface IMailImportHandler
    {
        /// <summary>
        /// Codice identificativo univoco del servizio gestito dall'handler.
        /// </summary>
        string ServiceCode { get; }

        /// <summary>
        /// Esegue l'importazione dati dal servizio mail specificato.
        /// </summary>
        /// <param name="sp">Service provider per accedere ai servizi DI.</param>
        /// <param name="ctx">Contesto di esecuzione contenente parametri specifici.</param>
        /// <param name="ct">Token di cancellazione per gestire l'interruzione dell'operazione.</param>
        /// <returns>Numero di elementi processati con successo.</returns>
        Task<int> ExecuteAsync(IServiceProvider sp, MailImportExecutionContext ctx, CancellationToken ct);
    }
}