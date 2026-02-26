using BlazorDematReports.Core.Constants;
using BlazorDematReports.Core.Lavorazioni.Interfaces;
using BlazorDematReports.Core.Lavorazioni.Models;
using BlazorDematReports.Core.Utility;
using BlazorDematReports.Core.Utility.Interfaces;
using BlazorDematReports.Core.Utility.Models;
using Entities.Helpers;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;

namespace BlazorDematReports.Core.Handlers.LavorazioniHandlers
{
    /// <summary>
    /// Handler per la lavorazione Z0082041_SOFTLINE seguendo il pattern registry.
    /// Gestisce l'elaborazione dei dati di produzione da tabelle Oracle
    /// relative alla procedura Z0082041_SOFTLINE.
    /// </summary>
    public sealed class Z0082041_SoftlineHandler : IProductionDataHandler
    {
        private readonly ILavorazioniConfigManager _configManager;
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Inizializza una nuova istanza di <see cref="Z0082041_SoftlineHandler"/>.
        /// </summary>
        public Z0082041_SoftlineHandler(
            ILavorazioniConfigManager configManager,
            ILoggerFactory loggerFactory)
        {
            _configManager = configManager;
            _loggerFactory = loggerFactory;
        }

        /// <summary>Codice identificativo univoco della lavorazione.</summary>
        public string HandlerCode => LavorazioniCodes.Z0082041_SOFTLINE;

        /// <summary>Esegue la lavorazione Z0082041_SOFTLINE.</summary>
        public async Task<List<DatiLavorazione>> ExecuteAsync(ProductionExecutionContext context, CancellationToken ct = default)
        {
            var lavorazione = new Z0082041_SOFTLINEProcessor(
                _configManager,
                _loggerFactory.CreateLogger<Z0082041_SOFTLINEProcessor>());

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
    /// Classe di lavorazione per la procedura Z0082041_SOFTLINE.
    /// Legge dati dalla tabella Oracle <c>Z0082041_SOFTLINE_DETTAGLIO</c>,
    /// gestendo sia la fase di scansione (IDFase=4) che di indicizzazione (IDFase=5).
    /// I parametri data Oracle usano il formato stringa <c>YYYYMMDD</c> con <c>TO_DATE</c>.
    /// </summary>
    internal sealed class Z0082041_SOFTLINEProcessor : BaseLavorazione
    {
        private readonly ILogger<Z0082041_SOFTLINEProcessor> _logger;
        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;

        /// <summary>
        /// Inizializza una nuova istanza di <see cref="Z0082041_SOFTLINEProcessor"/>.
        /// </summary>
        /// <param name="lavorazioniConfigManager">Servizio di configurazione lavorazioni.</param>
        /// <param name="logger">Logger per la classe.</param>
        public Z0082041_SOFTLINEProcessor(
            ILavorazioniConfigManager lavorazioniConfigManager,
            ILogger<Z0082041_SOFTLINEProcessor> logger)
        {
            _lavorazioniConfigManager = lavorazioniConfigManager;
            _logger = logger;
        }

        /// <summary>
        /// Recupera e aggrega i dati di lavorazione per la procedura Z0082041_SOFTLINE.
        /// Gestisce le fasi 4 (scansione) e 5 (indicizzazione) su tabelle Oracle.
        /// </summary>
        /// <param name="ct">Token di cancellazione.</param>
        /// <returns>Lista di <see cref="DatiLavorazione"/> acquisiti dalla fonte dati.</returns>
        public override async Task<List<DatiLavorazione>> SetDatiDematAsync(CancellationToken ct = default)
        {
            QueryLoggingHelper.LogQueryExecution(
                logger: _logger,
                queryType: "SELECT",
                entityName: "Z0082041_SOFTLINE_DETTAGLIO");

            // Oracle usa TO_DATE con stringa YYYYMMDD — non è possibile usare DateTime2 come in SQL Server
            var startData = StartDataLavorazione.ToString("yyyyMMdd");
            var endData   = (EndDataLavorazione ?? StartDataLavorazione).ToString("yyyyMMdd");

            _logger.LogInformation(
                "[Z0082041_SOFTLINE] Elaborazione dati per IDFaseLavorazione: {IdFase}, Periodo: {Start} - {End}",
                IDFaseLavorazione, startData, endData);

            var result = IDFaseLavorazione switch
            {
                4 => await EseguiQueryOracleAsync(QueryFase4, startData, endData, ct),
                5 => await EseguiQueryOracleAsync(QueryFase5, startData, endData, ct),
                _ => LogFaseNonGestita(IDFaseLavorazione)
            };

            _logger.LogInformation(
                "[Z0082041_SOFTLINE] Elaborazione completata. Record ottenuti: {Count}", result.Count);

            return result;
        }

        // Query fase 4: scansione. TRUNC usato per normalizzare la data Oracle.
        private const string QueryFase4 = """
            SELECT
                OP_SCAN                     AS operatore,
                TRUNC(DATA_SCAN)            AS DataLavorazione,
                COUNT(*)                    AS Documenti,
                SUM(NVL(NUM_PAG, 0)) / 2   AS Fogli,
                SUM(NVL(NUM_PAG, 0))        AS Pagine
            FROM Z0082041_SOFTLINE_DETTAGLIO
            WHERE TRUNC(DATA_SCAN) >= TO_DATE(:startData, 'YYYYMMDD')
              AND TRUNC(DATA_SCAN) <= TO_DATE(:endData,   'YYYYMMDD')
            GROUP BY OP_SCAN, TRUNC(DATA_SCAN)
            """;

        // Query fase 5: indicizzazione.
        private const string QueryFase5 = """
            SELECT
                OP_INDEX                    AS operatore,
                TRUNC(DATA_INDEX)           AS DataLavorazione,
                COUNT(*)                    AS Documenti,
                SUM(NVL(NUM_PAG, 0)) / 2   AS Fogli,
                SUM(NVL(NUM_PAG, 0))        AS Pagine
            FROM Z0082041_SOFTLINE_DETTAGLIO
            WHERE TRUNC(DATA_INDEX) >= TO_DATE(:startData, 'YYYYMMDD')
              AND TRUNC(DATA_INDEX) <= TO_DATE(:endData,   'YYYYMMDD')
            GROUP BY OP_INDEX, TRUNC(DATA_INDEX)
            """;

        /// <summary>Logga un avviso per una fase non gestita e restituisce una lista vuota.</summary>
        private List<DatiLavorazione> LogFaseNonGestita(int idFase)
        {
            _logger.LogWarning("[Z0082041_SOFTLINE] IDFaseLavorazione {IdFase} non gestito", idFase);
            return [];
        }

        /// <summary>
        /// Esegue una query Oracle e restituisce i dati di lavorazione.
        /// I parametri <c>:startData</c> e <c>:endData</c> sono stringhe in formato <c>YYYYMMDD</c>
        /// perché Oracle richiede <c>TO_DATE</c> per la conversione.
        /// </summary>
        /// <param name="query">Query Oracle da eseguire.</param>
        /// <param name="startData">Data inizio in formato YYYYMMDD.</param>
        /// <param name="endData">Data fine in formato YYYYMMDD.</param>
        /// <param name="ct">Token di cancellazione.</param>
        private async Task<List<DatiLavorazione>> EseguiQueryOracleAsync(
            string query,
            string startData,
            string endData,
            CancellationToken ct = default)
        {
            QueryLoggingHelper.LogQueryExecution(logger: _logger);

            var result    = new List<DatiLavorazione>();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var connectionString = _lavorazioniConfigManager.GetConnectionString("OracleConnectionString")
                                    ?? _lavorazioniConfigManager.CnxnCaptiva206;

                await using var connection = new OracleConnection(connectionString);
                await connection.OpenAsync(ct);

                await using var cmd = new OracleCommand(query, connection);
                cmd.CommandTimeout = 30;
                cmd.BindByName = true;
                cmd.Parameters.Add("startData", startData);
                cmd.Parameters.Add("endData",   endData);

                _logger.LogDebug("[Z0082041_SOFTLINE] Esecuzione query Oracle con timeout: {Timeout}s",
                    cmd.CommandTimeout);

                await using var reader = await cmd.ExecuteReaderAsync(ct);

                // Ordinal pre-calcolati una sola volta per performance
                var ordOp   = reader.GetOrdinal("operatore");
                var ordData = reader.GetOrdinal("DataLavorazione");
                var ordDoc  = reader.GetOrdinal("Documenti");
                var ordFog  = reader.GetOrdinal("Fogli");
                var ordPag  = reader.GetOrdinal("Pagine");

                while (await reader.ReadAsync(ct))
                {
                    result.Add(new DatiLavorazione
                    {
                        Operatore       = reader.IsDBNull(ordOp)  ? null : reader.GetString(ordOp).Trim(),
                        DataLavorazione = reader.GetDateTime(ordData),
                        Documenti       = reader.IsDBNull(ordDoc) ? null : reader.GetInt32(ordDoc),
                        Fogli           = reader.IsDBNull(ordFog) ? null : reader.GetInt32(ordFog),
                        Pagine          = reader.IsDBNull(ordPag) ? null : reader.GetInt32(ordPag),
                        AppartieneAlCentroSelezionato = true
                    });
                }

                stopwatch.Stop();
                _logger.LogInformation(
                    "[Z0082041_SOFTLINE] Query Oracle eseguita con successo. Record letti: {Count}, Tempo: {Ms}ms",
                    result.Count, stopwatch.ElapsedMilliseconds);
            }
            catch (OracleException oracleEx)
            {
                stopwatch.Stop();
                _logger.LogError(oracleEx,
                    "[Z0082041_SOFTLINE] Errore Oracle. Tempo: {Ms}ms, Numero: {ErrNum}",
                    stopwatch.ElapsedMilliseconds, oracleEx.Number);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "[Z0082041_SOFTLINE] Errore generico. Tempo: {Ms}ms",
                    stopwatch.ElapsedMilliseconds);
                throw;
            }

            return result;
        }
    }
}