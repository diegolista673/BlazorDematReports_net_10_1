using Entities.Models.DbApplication;

namespace BlazorDematReports.Core.Interfaces.IDataService
{
    /// <summary>
    /// Interfaccia per la gestione dell'invio email e configurazioni servizi mail.
    /// Fornisce metodi per inviare email e interrogare le configurazioni EmailCSV.
    /// </summary>
    public interface IServiceMail
    {
        /// <summary>
        /// Conta il numero totale di servizi mail configurati (configurazioni EmailCSV attive).
        /// </summary>
        /// <returns>Numero di configurazioni EmailCSV attive.</returns>
        Task<int> GetMailServicesCountAsync();

        /// <summary>
        /// Conta i servizi mail associati a una specifica procedura di lavorazione.
        /// </summary>
        /// <param name="idProceduraLavorazione">ID della procedura di lavorazione.</param>
        /// <returns>Numero di configurazioni EmailCSV attive per la procedura specificata.</returns>
        Task<int> GetMailServicesCountByProceduraAsync(int idProceduraLavorazione);

        /// <summary>
        /// Restituisce tutte le configurazioni EmailCSV attive.
        /// </summary>
        /// <returns>Lista di configurazioni fonti dati di tipo EmailCSV.</returns>
        Task<List<ConfigurazioneFontiDati>> GetMailServicesAsync();

        /// <summary>
        /// Restituisce le configurazioni EmailCSV per una specifica procedura.
        /// </summary>
        /// <param name="idProceduraLavorazione">ID della procedura di lavorazione.</param>
        /// <returns>Lista di configurazioni EmailCSV per la procedura specificata.</returns>
        Task<List<ConfigurazioneFontiDati>> GetMailServicesByProceduraAsync(int idProceduraLavorazione);

        /// <summary>
        /// Invia una email semplice.
        /// </summary>
        /// <param name="to">Indirizzo email destinatario.</param>
        /// <param name="subject">Oggetto dell'email.</param>
        /// <param name="body">Corpo dell'email.</param>
        /// <returns>True se l'invio è riuscito, false altrimenti.</returns>
        Task<bool> SendEmailAsync(string to, string subject, string body);

        /// <summary>
        /// Invia una email con mittente, destinatario, oggetto e corpo specificati.
        /// </summary>
        /// <param name="from">Indirizzo email mittente.</param>
        /// <param name="to">Indirizzo email destinatario.</param>
        /// <param name="toName">Nome del destinatario.</param>
        /// <param name="subject">Oggetto dell'email.</param>
        /// <param name="body">Corpo dell'email.</param>
        /// <returns>True se l'invio è riuscito, false altrimenti.</returns>
        Task<bool> SendEmailAsync(string from, string to, string toName, string subject, string body);
    }
}
