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
    /// Handler per la lavorazione Z0072370_28AUT seguendo il pattern registry.
    /// Gestisce l'elaborazione dei dati di produzione per la procedura Z0072370_28AUT,
    /// sia per la fase di scansione (IDFase=4) che di indicizzazione (IDFase=5).
    /// </summary>
    public sealed class Z0072370_28AutHandler : IProductionDataHandler
    {
        private readonly ILavorazioniConfigManager _configManager;
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Inizializza una nuova istanza di <see cref="Z0072370_28AutHandler"/>.
        /// </summary>
        public Z0072370_28AutHandler(
            ILavorazioniConfigManager configManager,
            ILoggerFactory loggerFactory)
        {
            _configManager = configManager;
            _loggerFactory = loggerFactory;
        }

        /// <summary>Codice identificativo univoco della lavorazione.</summary>
        public string HandlerCode => LavorazioniCodes.Z0072370_28AUT;

        /// <summary>Esegue la lavorazione Z0072370_28AUT.</summary>
        public async Task<List<DatiLavorazione>> ExecuteAsync(ProductionExecutionContext context, CancellationToken ct = default)
        {
            var lavorazione = new Z0072370_28AUTProcessor(
                _configManager,
                _loggerFactory.CreateLogger<Z0072370_28AUTProcessor>());

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
    /// Classe di lavorazione per la procedura Z0072370_28AUT.
    /// Legge dati dalla tabella SQL Server Z0072370_RDMKT_28AUT_GE_UDA_DETTAGLIO
    /// filtrati per department='GENOVA', garantendo l'appartenenza al centro selezionato.
    /// </summary>
    internal sealed class Z0072370_28AUTProcessor : BaseLavorazione
    {
        private readonly ILogger<Z0072370_28AUTProcessor> _logger;
        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;

        /// <summary>
        /// Inizializza una nuova istanza di <see cref="Z0072370_28AUTProcessor"/>.
        /// </summary>
        /// <param name="lavorazioniConfigManager">Servizio di configurazione lavorazioni.</param>
        /// <param name="logger">Logger per la classe.</param>
        public Z0072370_28AUTProcessor(
            ILavorazioniConfigManager lavorazioniConfigManager,
            ILogger<Z0072370_28AUTProcessor> logger)
        {
            _lavorazioniConfigManager = lavorazioniConfigManager;
            _logger = logger;
        }

        /// <summary>
        /// Recupera e aggrega i dati di lavorazione per la procedura Z0072370_28AUT.
        /// Gestisce le fasi 4 (scansione) e 5 (indicizzazione).
        /// </summary>
        /// <param name="ct">Token di cancellazione.</param>
        /// <returns>Lista di <see cref="DatiLavorazione"/> acquisiti dalla fonte dati.</returns>
        public override async Task<List<DatiLavorazione>> SetDatiDematAsync(CancellationToken ct = default)
        {
            QueryLoggingHelper.LogQueryExecution(
                logger: _logger,
                queryType: "SELECT",
                entityName: "Z0072370_RDMKT_28AUT_GE_UDA_DETTAGLIO");

            var startDate = StartDataLavorazione;
            var endDate   = EndDataLavorazione ?? StartDataLavorazione;

            _logger.LogInformation(
                "[Z0072370_28AUT] Elaborazione dati per IDFaseLavorazione: {IdFase}, Periodo: {Start:dd/MM/yyyy} - {End:dd/MM/yyyy}",
                IDFaseLavorazione, startDate, endDate);

            var result = IDFaseLavorazione switch
            {
                4 => await EseguiQueryAsync(QueryFase4, startDate, endDate, ct),
                5 => await EseguiQueryAsync(QueryFase5, startDate, endDate, ct),
                _ => LogFaseNonGestita(IDFaseLavorazione)
            };

            _logger.LogInformation(
                "[Z0072370_28AUT] Elaborazione completata. Record ottenuti: {Count}", result.Count);

            return result;
        }

        // Query fase 4: scansione documenti.
        private const string QueryFase4 = """
            SELECT
                OP_SCAN                     AS operatore,
                CONVERT(date, DATA_SCAN)    AS DataLavorazione,
                COUNT(*)                    AS Documenti,
                SUM(CONVERT(int, NUM_PAG)) / 2 AS Fogli,
                SUM(CONVERT(int, NUM_PAG)) AS Pagine
            FROM Z0072370_RDMKT_28AUT_GE_UDA_DETTAGLIO
            WHERE DATA_SCAN >= @startDate
              AND convert(date, DATA_SCAN) <= @endDate
              AND department  = 'GENOVA'
            GROUP BY OP_SCAN, CONVERT(date, DATA_SCAN)
            """;

        // Query fase 5: indicizzazione documenti.
        private const string QueryFase5 = """
            SELECT
                OP_INDEX                    AS operatore,
                CONVERT(date, DATA_INDEX)   AS DataLavorazione,
                COUNT(*)                    AS Documenti,
                SUM(CONVERT(int, NUM_PAG)) / 2 AS Fogli,
                SUM(CONVERT(int, NUM_PAG)) AS Pagine
            FROM Z0072370_RDMKT_28AUT_GE_UDA_DETTAGLIO
            WHERE DATA_INDEX >= @startDate
              AND convert(date, DATA_INDEX) <= @endDate
              AND department   = 'GENOVA'
            GROUP BY OP_INDEX, CONVERT(date, DATA_INDEX)
            """;

        /// <summary>
        /// Logga un avviso per una fase non gestita e restituisce una lista vuota.
        /// </summary>
        private List<DatiLavorazione> LogFaseNonGestita(int idFase)
        {
            _logger.LogWarning("[Z0072370_28AUT] IDFaseLavorazione {IdFase} non gestito", idFase);
            return [];
        }

        /// <summary>
        /// Esegue una query SQL su SQL Server e restituisce i dati di lavorazione.
        /// I parametri <c>@startDate</c> e <c>@endDate</c> sono tipizzati come DateTime2.
        /// La colonna <c>AppartieneAlCentroSelezionato</c> è sempre <c>true</c>
        /// perché il filtro <c>department='GENOVA'</c> garantisce l'appartenenza al centro.
        /// </summary>
        /// <param name="query">Query SQL da eseguire.</param>
        /// <param name="startDate">Data di inizio periodo.</param>
        /// <param name="endDate">Data di fine periodo.</param>
        /// <param name="ct">Token di cancellazione.</param>
        /// <returns>Lista di <see cref="DatiLavorazione"/> ottenuta dalla query.</returns>
        private async Task<List<DatiLavorazione>> EseguiQueryAsync(
            string query,
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
                cmd.CommandTimeout = 30;
                cmd.Parameters.Add("@startDate", SqlDbType.DateTime2).Value = startDate;
                cmd.Parameters.Add("@endDate",   SqlDbType.DateTime2).Value = endDate;

                _logger.LogDebug("[Z0072370_28AUT] Esecuzione query con timeout: {Timeout}s", cmd.CommandTimeout);

                await using var reader = await cmd.ExecuteReaderAsync(ct);

                // Ordinal pre-calcolati una sola volta per performance su grandi dataset
                var ordOp   = reader.GetOrdinal("operatore");
                var ordData = reader.GetOrdinal("DataLavorazione");
                var ordDoc  = reader.GetOrdinal("Documenti");
                var ordFog  = reader.GetOrdinal("Fogli");
                var ordPag  = reader.GetOrdinal("Pagine");

                while (await reader.ReadAsync(ct))
                {
                    result.Add(new DatiLavorazione
                    {
                        Operatore       = reader.IsDBNull(ordOp)   ? null : reader.GetString(ordOp).Trim(),
                        DataLavorazione = reader.GetDateTime(ordData),
                        Documenti       = reader.IsDBNull(ordDoc)  ? null : reader.GetInt32(ordDoc),
                        Fogli           = reader.IsDBNull(ordFog)  ? null : reader.GetInt32(ordFog),
                        Pagine          = reader.IsDBNull(ordPag)  ? null : reader.GetInt32(ordPag),
                        // department='GENOVA' garantisce che tutti i risultati appartengano al centro
                        AppartieneAlCentroSelezionato = true
                    });
                }

                stopwatch.Stop();
                _logger.LogInformation(
                    "[Z0072370_28AUT] Query eseguita con successo. Record letti: {Count}, Tempo: {Ms}ms",
                    result.Count, stopwatch.ElapsedMilliseconds);
            }
            catch (SqlException sqlEx)
            {
                stopwatch.Stop();
                _logger.LogError(sqlEx,
                    "[Z0072370_28AUT] Errore SQL. Tempo: {Ms}ms, Numero: {ErrNum}, Severita: {Class}, Stato: {State}",
                    stopwatch.ElapsedMilliseconds, sqlEx.Number, sqlEx.Class, sqlEx.State);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "[Z0072370_28AUT] Errore generico. Tempo: {Ms}ms", stopwatch.ElapsedMilliseconds);
                throw;
            }

            return result;
        }
    }
}
