using BlazorDematReports.Core.Constants;
using BlazorDematReports.Core.Lavorazioni.Interfaces;
using BlazorDematReports.Core.Lavorazioni.Models;
using BlazorDematReports.Core.Utility;
using BlazorDematReports.Core.Utility.Interfaces;
using BlazorDematReports.Core.Utility.Models;
using Entities.Helpers;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace BlazorDematReports.Core.Handlers.LavorazioniHandlers
{
    /// <summary>
    /// Handler per la lavorazione RDMKT_RSP seguendo il pattern registry.
    /// Gestisce l'elaborazione dei dati di produzione da tabelle dinamiche SQL Server
    /// relative alla procedura RDMKT_RSP.
    /// </summary>
    public sealed class Rdmkt_RSPHandler : IProductionDataHandler
    {
        private readonly ILavorazioniConfigManager _configManager;
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Inizializza una nuova istanza di <see cref="Rdmkt_RSPHandler"/>.
        /// </summary>
        public Rdmkt_RSPHandler(
            ILavorazioniConfigManager configManager,
            ILoggerFactory loggerFactory)
        {
            _configManager = configManager;
            _loggerFactory = loggerFactory;
        }

        /// <summary>Codice identificativo univoco della lavorazione.</summary>
        public string HandlerCode => LavorazioniCodes.RDMKT_RSP;

        /// <summary>Esegue la lavorazione RDMKT_RSP.</summary>
        public async Task<List<DatiLavorazione>> ExecuteAsync(ProductionExecutionContext context, CancellationToken ct = default)
        {
            var lavorazione = new RDMKT_RSPProcessor(
                _configManager,
                _loggerFactory.CreateLogger<RDMKT_RSPProcessor>());

            lavorazione.NomeProcedura          = context.NomeProcedura;
            lavorazione.IDFaseLavorazione      = context.IDFaseLavorazione;
            lavorazione.IDProceduraLavorazione = context.IDProceduraLavorazione;
            lavorazione.IDCentro               = context.IDCentro;
            lavorazione.StartDataLavorazione   = context.StartDataLavorazione;
            lavorazione.EndDataLavorazione     = context.EndDataLavorazione;

            return await lavorazione.SetDatiDematAsync(ct);
        }
    }

    /// <summary>
    /// Classe di lavorazione per la procedura RDMKT_RSP.
    /// Legge dati da tabelle dinamiche SQL Server con pattern <c>*_RSP_*_UDA_DETTAGLIO</c>,
    /// gestendo sia la fase di scansione (IDFase=4) che di indicizzazione (IDFase=5).
    /// </summary>
    internal sealed class RDMKT_RSPProcessor : BaseLavorazione
    {
        private readonly ILogger<RDMKT_RSPProcessor> _logger;
        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;

        /// <summary>
        /// Inizializza una nuova istanza di <see cref="RDMKT_RSPProcessor"/>.
        /// </summary>
        /// <param name="lavorazioniConfigManager">Servizio di configurazione lavorazioni.</param>
        /// <param name="logger">Logger per la classe.</param>
        public RDMKT_RSPProcessor(
            ILavorazioniConfigManager lavorazioniConfigManager,
            ILogger<RDMKT_RSPProcessor> logger)
        {
            _lavorazioniConfigManager = lavorazioniConfigManager;
            _logger = logger;
        }

        /// <summary>
        /// Recupera e aggrega i dati di lavorazione per la procedura RDMKT_RSP.
        /// Scopre dinamicamente le tabelle attive con il pattern <c>*_RSP_*_UDA_DETTAGLIO</c>.
        /// </summary>
        /// <param name="ct">Token di cancellazione.</param>
        /// <returns>Lista di <see cref="DatiLavorazione"/> acquisiti dalle fonti dati.</returns>
        public override async Task<List<DatiLavorazione>> SetDatiDematAsync(CancellationToken ct = default)
        {
            QueryLoggingHelper.LogQueryExecution(
                logger: _logger,
                queryType: "SELECT",
                entityName: "RDMKT_RSP_Dynamic_Tables");

            var startDate = StartDataLavorazione;
            var endDate   = EndDataLavorazione ?? StartDataLavorazione;

            _logger.LogInformation(
                "[RDMKT_RSP] Elaborazione dati per IDFaseLavorazione: {IdFase}, Periodo: {Start:d} - {End:d}",
                IDFaseLavorazione, startDate, endDate);

            var result = new List<DatiLavorazione>();
            try
            {
                var tableNames = await GetNonEmptyTableNamesAsync(ct);
                _logger.LogInformation("[RDMKT_RSP] Trovate {Count} tabelle RSP con dati", tableNames.Count);

                foreach (var tabName in tableNames)
                {
                    var dati = IDFaseLavorazione switch
                    {
                        // DATA_INDEX
                        5 => await EseguiQueryAsync(
                            $"""
                            SELECT
                                OP_INDEX                    AS operatore,
                                CONVERT(date, DATA_INDEX)   AS DataLavorazione,
                                COUNT(*)                    AS Documenti,
                                SUM(CONVERT(int, ISNULL(NUM_PAG, 0))) / 2 AS Fogli,
                                SUM(CONVERT(int, ISNULL(NUM_PAG, 0))) AS Pagine,
                                OP_SCAN
                            FROM {tabName}
                            WHERE DATA_INDEX >= @startDate
                              AND convert(date, DATA_INDEX) <= @endDate
                            GROUP BY OP_INDEX, CONVERT(date, DATA_INDEX)
                            """,
                            tabName, includeOperatoreScan: true, startDate, endDate, ct),

                        //Scan
                        4 => await EseguiQueryAsync(
                            $"""
                            SELECT
                                OP_SCAN                     AS operatore,
                                CONVERT(date, DATA_SCAN)    AS DataLavorazione,
                                COUNT(*)                    AS Documenti,
                                SUM(CONVERT(int, ISNULL(NUM_PAG, 0))) / 2 AS Fogli,
                                SUM(CONVERT(int, ISNULL(NUM_PAG, 0))) AS Pagine
                            FROM {tabName}
                            WHERE DATA_SCAN >= @startDate
                              AND convert(date, DATA_SCAN) <= @endDate
                            GROUP BY OP_SCAN, CONVERT(date, DATA_SCAN)
                            ORDER BY CONVERT(date, DATA_SCAN)
                            """,
                            tabName, includeOperatoreScan: false, startDate, endDate, ct),

                        _ => LogFaseNonGestita(IDFaseLavorazione)
                    };

                    result.AddRange(dati);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RDMKT_RSP] Errore durante l'elaborazione delle tabelle dinamiche");
                throw;
            }

            _logger.LogInformation(
                "[RDMKT_RSP] Elaborazione completata. Record totali ottenuti: {Count}", result.Count);

            return result;
        }

        /// <summary>Logga un avviso per una fase non gestita e restituisce una lista vuota.</summary>
        private List<DatiLavorazione> LogFaseNonGestita(int idFase)
        {
            _logger.LogWarning("[RDMKT_RSP] IDFaseLavorazione {IdFase} non gestito", idFase);
            return [];
        }

        /// <summary>
        /// Trova tutte le tabelle con nome che contiene <c>_RSP_</c> e <c>_UDA_DETTAGLIO</c> e con almeno una riga.
        /// Il nome della tabella proviene da <c>sys.tables</c> — non è input utente, non c'è rischio SQL injection.
        /// </summary>
        private async Task<List<string>> GetNonEmptyTableNamesAsync(CancellationToken ct = default)
        {
            QueryLoggingHelper.LogQueryExecution(
                logger: _logger,
                queryType: "METADATA",
                additionalInfo: "Ricerca tabelle dinamiche RSP");

            const string queryTabelle = """
                SELECT DISTINCT T.name AS TableName
                FROM sys.tables     T
                JOIN sys.sysindexes I ON T.OBJECT_ID = I.ID
                WHERE T.name LIKE '%_RSP_%_UDA_DETTAGLIO'
                  AND I.Rows > 0
                """;

            var tableNames = new List<string>();
            var stopwatch  = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                await using var connection = new SqlConnection(_lavorazioniConfigManager.CnxnCaptiva206);
                await connection.OpenAsync(ct);

                await using var cmd = new SqlCommand(queryTabelle, connection);
                cmd.CommandTimeout = 30;

                _logger.LogDebug("[RDMKT_RSP] Ricerca tabelle dinamiche in corso");

                await using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                    tableNames.Add(reader.GetString(0));

                stopwatch.Stop();
                _logger.LogInformation(
                    "[RDMKT_RSP] Ricerca tabelle completata. Trovate: {Count}, Tempo: {Ms}ms",
                    tableNames.Count, stopwatch.ElapsedMilliseconds);

                if (tableNames.Count > 0)
                    _logger.LogDebug("[RDMKT_RSP] Tabelle: {Tables}", string.Join(", ", tableNames));
            }
            catch (SqlException sqlEx)
            {
                stopwatch.Stop();
                _logger.LogError(sqlEx,
                    "[RDMKT_RSP] Errore SQL ricerca tabelle. Tempo: {Ms}ms, Numero: {ErrNum}, Severita: {Class}, Stato: {State}",
                    stopwatch.ElapsedMilliseconds, sqlEx.Number, sqlEx.Class, sqlEx.State);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "[RDMKT_RSP] Errore generico ricerca tabelle. Tempo: {Ms}ms",
                    stopwatch.ElapsedMilliseconds);
                throw;
            }

            return tableNames;
        }

        /// <summary>
        /// Esegue la query di lavorazione su una tabella dinamica SQL Server.
        /// </summary>
        /// <param name="query">Query SQL da eseguire (tabella già interpolata).</param>
        /// <param name="tableName">Nome della tabella (solo per logging).</param>
        /// <param name="includeOperatoreScan">Se <c>true</c>, legge il campo <c>OP_SCAN</c> opzionale.</param>
        /// <param name="startDate">Data di inizio periodo.</param>
        /// <param name="endDate">Data di fine periodo.</param>
        /// <param name="ct">Token di cancellazione.</param>
        private async Task<List<DatiLavorazione>> EseguiQueryAsync(
            string query,
            string tableName,
            bool includeOperatoreScan,
            DateTime startDate,
            DateTime endDate,
            CancellationToken ct = default)
        {
            QueryLoggingHelper.LogQueryExecution(logger: _logger);

            var result    = new List<DatiLavorazione>();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                await using var connection = new SqlConnection(_lavorazioniConfigManager.CnxnCaptiva206);
                await connection.OpenAsync(ct);

                await using var cmd = new SqlCommand(query, connection);
                cmd.CommandTimeout = 60;
                cmd.Parameters.Add("@startDate", SqlDbType.DateTime2).Value = startDate;
                cmd.Parameters.Add("@endDate",   SqlDbType.DateTime2).Value = endDate;

                _logger.LogDebug("[RDMKT_RSP] Esecuzione query su {Table} con timeout: {Timeout}s",
                    tableName, cmd.CommandTimeout);

                await using var reader = await cmd.ExecuteReaderAsync(ct);

                // Ordinal pre-calcolati una sola volta per performance
                var ordOp   = reader.GetOrdinal("operatore");
                var ordData = reader.GetOrdinal("DataLavorazione");
                var ordDoc  = reader.GetOrdinal("Documenti");
                var ordFog  = reader.GetOrdinal("Fogli");
                var ordPag  = reader.GetOrdinal("Pagine");

                // OP_SCAN è opzionale: presente solo nella query fase 5
                int? ordOpScan = includeOperatoreScan ? TryGetOrdinal(reader, "OP_SCAN") : null;

                int recordCount = 0;
                while (await reader.ReadAsync(ct))
                {
                    var dato = new DatiLavorazione
                    {
                        Operatore       = reader.IsDBNull(ordOp)  ? null : reader.GetString(ordOp).Trim(),
                        DataLavorazione = reader.GetDateTime(ordData),
                        Documenti       = reader.IsDBNull(ordDoc) ? null : reader.GetInt32(ordDoc),
                        Fogli           = reader.IsDBNull(ordFog) ? null : reader.GetInt32(ordFog),
                        Pagine          = reader.IsDBNull(ordPag) ? null : reader.GetInt32(ordPag),
                        // Le tabelle RSP sono specifiche per centro (nessun filtro multi-centro)
                        AppartieneAlCentroSelezionato = true
                    };

                    if (ordOpScan.HasValue && !reader.IsDBNull(ordOpScan.Value))
                        dato.OperatoreScan = reader.GetString(ordOpScan.Value).Trim();

                    result.Add(dato);
                    recordCount++;
                }

                stopwatch.Stop();
                _logger.LogInformation(
                    "[RDMKT_RSP] Query su {Table} completata. Record letti: {Count}, Tempo: {Ms}ms",
                    tableName, recordCount, stopwatch.ElapsedMilliseconds);
            }
            catch (SqlException sqlEx)
            {
                stopwatch.Stop();
                _logger.LogError(sqlEx,
                    "[RDMKT_RSP] Errore SQL su {Table}. Tempo: {Ms}ms, Numero: {ErrNum}, Severita: {Class}, Stato: {State}",
                    tableName, stopwatch.ElapsedMilliseconds, sqlEx.Number, sqlEx.Class, sqlEx.State);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "[RDMKT_RSP] Errore generico su {Table}. Tempo: {Ms}ms",
                    tableName, stopwatch.ElapsedMilliseconds);
                throw;
            }

            return result;
        }

        /// <summary>
        /// Cerca l'ordinal di una colonna opzionale senza lanciare eccezioni.
        /// Restituisce <c>null</c> se la colonna non è presente.
        /// </summary>
        private static int? TryGetOrdinal(SqlDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return null;
        }
    }
}