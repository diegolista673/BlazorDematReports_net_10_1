using Entities.Models.DbApplication;

namespace BlazorDematReports.Core.Services.Interfaces.IDataService
{
    /// <summary>
    /// Interfaccia per la gestione dell'invio email e configurazioni servizi mail.
    /// Fornisce metodi per inviare email e interrogare le configurazioni EmailCSV.
    /// </summary>
    public interface IServiceMail
    {


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
