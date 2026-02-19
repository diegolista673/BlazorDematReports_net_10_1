using BlazorDematReports.Core.DataReading.Models;

namespace BlazorDematReports.Core.DataReading.Interfaces
{
    /// <summary>
    /// Interfaccia per la gestione dei job Hangfire tramite accesso diretto al database.
    /// </summary>
    public interface IHangfireJobService
    {
        /// <summary>
        /// Ottiene il job Hangfire in formato JSON dalla tabella del database.
        /// </summary>
        /// <param name="keyJob">Chiave del job Hangfire.</param>
        /// <param name="connectionString">Stringa di connessione al database.</param>
        /// <returns>JSON del job Hangfire.</returns>
        Task<string> GetJobJsonAsync(string keyJob, string connectionString);

        /// <summary>
        /// Deserializza il JSON di un job Hangfire in un oggetto JsonJobHangfire.
        /// </summary>
        /// <param name="jsonString">Stringa JSON del job Hangfire.</param>
        /// <returns>Oggetto <see cref="JsonJobHangfire"/> deserializzato.</returns>
        JsonJobHangfire DeserializeJob(string jsonString);
    }
}
