using BlazorDematReports.Core.DataReading.Interfaces;
using BlazorDematReports.Core.DataReading.Models;
using Entities.Helpers;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace BlazorDematReports.Core.DataReading.Services
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
        /// Esegue una query di produzione e restituisce risultati tipizzati con schema standard.
        /// Le colonne Operatore, DataLavorazione, Documenti, Fogli, Pagine sono cercate case-insensitive.
        /// Gli ordinal sono risolti una sola volta per massimizzare le performance su grandi dataset.
        /// <para>
        /// Verifica dell'appartenenza al centro — tre livelli in ordine di priorità:
        /// <list type="number">
        ///   <item><c>IdCentro</c> nel SELECT — confronto numerico per riga con <paramref name="idCentroAtteso"/>.</item>
        ///   <item><c>NomeCentro</c> nel SELECT — confronto testuale per riga con <paramref name="nomeCentroAtteso"/> (case-insensitive).</item>
        ///   <item>Euristica WHERE — se nessuna colonna è presente, cerca <paramref name="nomeCentroAtteso"/> come
        ///   valore stringa SQL nel testo della query (es. <c>department = 'GENOVA'</c>).
        ///   Se trovato → tutti i record appartengono al centro.
        ///   Se non trovato o <paramref name="nomeCentroAtteso"/> è null → assume appartenenza (comportamento legacy).</item>
        /// </list>
        /// </para>
        /// </summary>
        public async Task<List<ProductionQueryResult>> ExecuteProductionQueryAsync(
            string connectionString,
            string queryString,
            DateTime startDate,
            DateTime endDate,
            int idCentroAtteso,
            string? nomeCentroAtteso = null,
            CancellationToken cancellationToken = default)
        {
            QueryLoggingHelper.LogQueryExecution(_logger);

            var results = new List<ProductionQueryResult>();
            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync(cancellationToken);

                await using var command = new SqlCommand(queryString, connection);
                command.CommandTimeout = 60;

                command.Parameters.Add("@startDate", SqlDbType.DateTime2).Value = startDate;
                command.Parameters.Add("@endDate",   SqlDbType.DateTime2).Value = endDate;

                await using var reader = await command.ExecuteReaderAsync(cancellationToken);

                var ordOp   = GetOrdinalCaseInsensitive(reader, "Operatore");
                var ordData = GetOrdinalCaseInsensitive(reader, "DataLavorazione");
                var ordDoc  = GetOrdinalCaseInsensitive(reader, "Documenti");
                var ordFog  = GetOrdinalCaseInsensitive(reader, "Fogli");
                var ordPag  = GetOrdinalCaseInsensitive(reader, "Pagine");

                while (await reader.ReadAsync(cancellationToken))
                {

                    // Logica priorità — tre livelli:
                    // 1. IdCentro nel SELECT  → verifica numerica per riga
                    // 2. NomeCentro nel SELECT → verifica testuale per riga (case-insensitive)
                    // 3. Nessuna colonna      → euristica WHERE: cerca il nome del centro come
                    //    valore stringa SQL nel testo della query (es. department = 'GENOVA').
                    //    Se il nome è presente → tutti i record appartengono al centro.
                    //    Se assente o nomeCentroAtteso è null → legacy: assume appartenenza.
                    bool appartieneAlCentro = nomeCentroAtteso is null || QueryContienNomeCentro(queryString, nomeCentroAtteso);


                    results.Add(new ProductionQueryResult
                    {
                        Operatore          = reader.GetString(ordOp).Trim(),
                        DataLavorazione    = reader.GetDateTime(ordData),
                        Documenti          = reader.IsDBNull(ordDoc) ? 0 : reader.GetInt32(ordDoc),
                        Fogli              = reader.IsDBNull(ordFog) ? 0 : reader.GetInt32(ordFog),
                        Pagine             = reader.IsDBNull(ordPag) ? 0 : reader.GetInt32(ordPag),
                        AppartieneAlCentro = appartieneAlCentro
                    });
                }
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Errore nell'esecuzione della query di produzione.");
                throw;
            }

            return results;
        }

        /// <summary>
        /// Risolve l'ordinal di una colonna in modo case-insensitive.
        /// Lancia <see cref="InvalidOperationException"/> se la colonna non è presente.
        /// </summary>
        private static int GetOrdinalCaseInsensitive(SqlDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            throw new InvalidOperationException(
                $"Colonna '{columnName}' mancante nel risultato della query. " +
                "Le query di produzione devono restituire: Operatore, DataLavorazione, Documenti, Fogli, Pagine.");
        }


        /// <summary>
        /// Verifica euristicamente se una query filtra per un nome di centro specifico.
        /// Cerca <paramref name="nomeCentro"/> come valore stringa SQL (<c>'VALORE'</c>) nel testo della query,
        /// indipendentemente dal nome della colonna usata nel WHERE (<c>department</c>, <c>sede</c>, <c>filiale</c>, ecc.).
        /// <example>
        /// Restituisce <c>true</c> per tutti questi pattern:
        /// <code>
        /// WHERE department = 'GENOVA'
        /// WHERE sede='verona'
        /// WHERE filiale = 'Pomezia'
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="queryText">Testo SQL della query.</param>
        /// <param name="nomeCentro">Nome del centro da cercare (es. 'GENOVA').</param>
        /// <returns><c>true</c> se il nome del centro è presente come valore stringa nella query.</returns>
        private static bool QueryContienNomeCentro(string queryText, string nomeCentro)
            => queryText.Contains($"'{nomeCentro}'", StringComparison.OrdinalIgnoreCase);
    }
}
