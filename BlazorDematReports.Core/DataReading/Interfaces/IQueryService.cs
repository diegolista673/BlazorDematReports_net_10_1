using System.Data;

namespace BlazorDematReports.Core.DataReading.Interfaces
{
    /// <summary>
    /// Interfaccia per l'esecuzione di query SQL su un database.
    /// Le validazioni sono state migrate a SqlValidationService.
    /// </summary>
    public interface IQueryService
    {
        /// <summary>
        /// Esegue una query SQL asincrona su un database e restituisce i risultati in un DataTable.
        /// </summary>
        /// <param name="connectionString">Stringa di connessione al database.</param>
        /// <param name="queryString">Query SQL da eseguire.</param>
        /// <param name="startDate">Data di inizio per il filtro della query.</param>
        /// <param name="endDate">Data di fine per il filtro della query.</param>
        /// <returns>Oggetto DataTable con i risultati della query.</returns>
        Task<DataTable> ExecuteQueryAsync(string connectionString, string queryString, DateTime startDate, DateTime endDate);
    }
}
