namespace LibraryLavorazioni.LavorazioniViaMail.Services
{
    /// <summary>
    /// Interfaccia per il servizio di importazione dati via mail.
    /// </summary>
    public interface IMailImportService
    {
        /// <summary>
        /// Processa le email HERA16 per l'importazione dei dati di produzione.
        /// </summary>
        /// <param name="ct">Token di cancellazione per l'operazione.</param>
        /// <returns>Numero di elementi processati con successo.</returns>
        Task<int> ProcessHera16Async(CancellationToken ct = default);

        /// <summary>
        /// Processa le email per servizi mail generici.
        /// </summary>
        /// <param name="serviceCode">Codice del servizio mail.</param>
        /// <param name="ct">Token di cancellazione per l'operazione.</param>
        /// <returns>Numero di elementi processati con successo.</returns>
        Task<int> ProcessMailServiceAsync(string serviceCode, CancellationToken ct = default);
    }
}