using DataReading.Interfaces;
using Entities.Helpers;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace DataReading.Services
{
    /// <summary>
    /// Servizio per l'esecuzione di query SQL su un database.
    /// Le validazioni sono state migrate a SqlValidationService.
    /// </summary>
    public class QueryService : IQueryService
    {
        private readonly ILogger<QueryService> _logger;

        /// <summary>
        /// Costruttore che accetta un logger per la classe QueryService.
        /// </summary>
        /// <param name="logger">Logger per la gestione degli errori di query.</param>
        public QueryService(ILogger<QueryService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Esegue una query SQL asincrona su un database e restituisce i risultati in un DataTable.
        /// </summary>
        /// <param name="connectionString">Stringa di connessione al database.</param>
        /// <param name="queryString">Query SQL da eseguire.</param>
        /// <param name="startDate">Data di inizio per il filtro della query.</param>
        /// <param name="endDate">Data di fine per il filtro della query.</param>
        /// <returns>Oggetto DataTable con i risultati della query.</returns>
        /// <exception cref="SqlException">Errore nell'esecuzione della query SQL.</exception>
        public async Task<DataTable> ExecuteQueryAsync(string connectionString, string queryString, DateTime startDate, DateTime endDate)
        {
            QueryLoggingHelper.LogQueryExecution(_logger);

            var table = new DataTable();
            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                using var command = new SqlCommand(queryString, connection);
                command.CommandTimeout = 30;
                
                var startDateParam = command.Parameters.Add("@startDate", SqlDbType.DateTime2);
                startDateParam.Value = startDate;
                
                var endDateParam = command.Parameters.Add("@endDate", SqlDbType.DateTime2);
                endDateParam.Value = endDate;
                
                using var reader = await command.ExecuteReaderAsync();
                table.Load(reader);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Errore nell'esecuzione della query SQL.");
                throw;
            }
            return table;
        }
    }
}
