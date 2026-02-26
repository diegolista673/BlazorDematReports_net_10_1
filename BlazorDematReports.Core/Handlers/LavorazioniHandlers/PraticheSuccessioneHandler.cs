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
    /// Handler per la lavorazione PRATICHE_SUCCESSIONE seguendo il pattern registry.
    /// Gestisce l'elaborazione dei dati di produzione dalla sorgente Oracle
    /// relativa alle pratiche di successione.
    /// </summary>
    public sealed class PraticheSuccessioneHandler : IProductionDataHandler
    {
        private readonly ILavorazioniConfigManager _configManager;
        private readonly ILoggerFactory _loggerFactory;

        public PraticheSuccessioneHandler(
            ILavorazioniConfigManager configManager,
            ILoggerFactory loggerFactory)
        {
            _configManager = configManager;
            _loggerFactory = loggerFactory;
        }

        /// <summary>Codice identificativo univoco della lavorazione.</summary>
        public string HandlerCode => LavorazioniCodes.PRATICHE_SUCCESSIONE;

        /// <summary>Esegue la lavorazione PRATICHE_SUCCESSIONE.</summary>
        public async Task<List<DatiLavorazione>> ExecuteAsync(ProductionExecutionContext context, CancellationToken ct = default)
        {
            var lavorazione = new PRATICHE_SUCCESSIONEProcessor(
                _configManager,
                _loggerFactory.CreateLogger<PRATICHE_SUCCESSIONEProcessor>());

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
    /// Classe di lavorazione per la procedura PRATICHE_SUCCESSIONE.
    /// Implementa la logica di lettura e aggregazione dei dati di produzione dalla sorgente Oracle
    /// relativa alle pratiche di successione, gestendo le diverse fasi di lavorazione.
    /// </summary>
    internal sealed class PRATICHE_SUCCESSIONEProcessor : BaseLavorazione
    {
        private readonly ILogger<PRATICHE_SUCCESSIONEProcessor> _logger;
        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;

        /// <summary>
        /// Inizializza una nuova istanza di PRATICHE_SUCCESSIONEProcessor.
        /// </summary>
        /// <param name="lavorazioniConfigManager">Servizio di configurazione lavorazioni.</param>
        public PRATICHE_SUCCESSIONEProcessor(
            ILavorazioniConfigManager lavorazioniConfigManager,
            ILogger<PRATICHE_SUCCESSIONEProcessor> logger
        )
        {
            _lavorazioniConfigManager = lavorazioniConfigManager;
            _logger = logger;
        }

        /// <summary>
        /// Recupera e aggrega i dati di lavorazione per la procedura PRATICHE_SUCCESSIONE.
        /// Gestisce le fasi 4 (scansione) e 5 (indicizzazione) su Oracle.
        /// </summary>
        /// <param name="ct">Token di cancellazione.</param>
        /// <returns>Lista di <see cref="DatiLavorazione"/> acquisiti dalla fonte dati.</returns>
        public override async Task<List<DatiLavorazione>> SetDatiDematAsync(CancellationToken ct = default)
        {
            QueryLoggingHelper.LogQueryExecution(logger: _logger);

            // Oracle usa TO_DATE con stringa YYYYMMDD — non è possibile usare DateTime2 come in SQL Server
            var startData = StartDataLavorazione.ToString("yyyyMMdd");
            var endData   = (EndDataLavorazione ?? StartDataLavorazione).ToString("yyyyMMdd");

            _logger.LogInformation(
                "[PRATICHE_SUCCESSIONE] Elaborazione dati per IDFaseLavorazione: {IdFase}, Periodo: {Start} - {End}",
                IDFaseLavorazione, startData, endData);

            var result = IDFaseLavorazione switch
            {
                4 => await EseguiQueryAsync(QueryFase4, startData, endData, ct),
                5 => await EseguiQueryAsync(QueryFase5, startData, endData, ct),
                _ => LogFaseNonGestita(IDFaseLavorazione)
            };

            _logger.LogInformation(
                "[PRATICHE_SUCCESSIONE] Elaborazione completata. Record ottenuti: {Count}", result.Count);

            return result;
        }

        // Query fase 4: scansione pratiche di successione da Oracle.
        private const string QueryFase4 = """
            SELECT
                pt_operatore_scan                        AS operatore,
                TRUNC(pt_data_scan)                      AS datalavorazione,
                TRUNC(COUNT(pt_barcode_ad_uso_interno))  AS documenti,
                TRUNC(SUM(pt_numero_pagine / 2))         AS fogli,
                TRUNC(SUM(pt_numero_pagine))             AS pagine
            FROM bp_pratichesucc_s
            WHERE TRUNC(pt_data_scan) >= TO_DATE(:startData, 'YYYYMMDD')
              AND TRUNC(pt_data_scan) <= TO_DATE(:endData,   'YYYYMMDD')
            GROUP BY pt_operatore_scan, TRUNC(pt_data_scan)
            """;

        // Query fase 5: indicizzazione pratiche di successione da Oracle.
        private const string QueryFase5 = """
            SELECT
                pt_operatore_index                       AS operatore,
                TRUNC(pt_data_index)                     AS datalavorazione,
                TRUNC(COUNT(pt_barcode_ad_uso_interno))  AS documenti,
                TRUNC(SUM(pt_numero_pagine / 2))         AS fogli,
                TRUNC(SUM(pt_numero_pagine))             AS pagine
            FROM bp_pratichesucc_s
            WHERE TRUNC(pt_data_index) >= TO_DATE(:startData, 'YYYYMMDD')
              AND TRUNC(pt_data_index) <= TO_DATE(:endData,   'YYYYMMDD')
            GROUP BY pt_operatore_index, TRUNC(pt_data_index)
            """;

        /// <summary>Logga un avviso per una fase non gestita e restituisce una lista vuota.</summary>
        private List<DatiLavorazione> LogFaseNonGestita(int idFase)
        {
            _logger.LogWarning("[PRATICHE_SUCCESSIONE] IDFaseLavorazione {IdFase} non gestito", idFase);
            return [];
        }

        /// <summary>
        /// Esegue una query Oracle per le pratiche di successione.
        /// I parametri <c>:startData</c> e <c>:endData</c> sono stringhe in formato <c>YYYYMMDD</c>
        /// perché Oracle richiede <c>TO_DATE</c> per la conversione.
        /// </summary>
        /// <param name="query">Query Oracle da eseguire.</param>
        /// <param name="startData">Data inizio in formato YYYYMMDD.</param>
        /// <param name="endData">Data fine in formato YYYYMMDD.</param>
        /// <param name="ct">Token di cancellazione.</param>
        private async Task<List<DatiLavorazione>> EseguiQueryAsync(
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
                await using var connection = new OracleConnection(_lavorazioniConfigManager.CnxnPraticheSuccessione);
                await connection.OpenAsync(ct);

                await using var cmd = connection.CreateCommand();
                cmd.CommandTimeout = 30;
                cmd.BindByName     = true;
                cmd.CommandText    = query;
                cmd.Parameters.Add("startData", startData);
                cmd.Parameters.Add("endData",   endData);

                _logger.LogDebug("[PRATICHE_SUCCESSIONE] Esecuzione query Oracle con timeout: {Timeout}s",
                    cmd.CommandTimeout);

                await using var reader = await cmd.ExecuteReaderAsync(ct);

                // Ordinal pre-calcolati una sola volta per performance
                var ordOp   = reader.GetOrdinal("operatore");
                var ordData = reader.GetOrdinal("datalavorazione");
                var ordDoc  = reader.GetOrdinal("documenti");
                var ordFog  = reader.GetOrdinal("fogli");
                var ordPag  = reader.GetOrdinal("pagine");

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
                    "[PRATICHE_SUCCESSIONE] Query Oracle eseguita con successo. Record letti: {Count}, Tempo: {Ms}ms",
                    result.Count, stopwatch.ElapsedMilliseconds);
            }
            catch (OracleException oracleEx)
            {
                stopwatch.Stop();
                _logger.LogError(oracleEx,
                    "[PRATICHE_SUCCESSIONE] Errore Oracle. Tempo: {Ms}ms, Numero: {ErrNum}",
                    stopwatch.ElapsedMilliseconds, oracleEx.Number);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "[PRATICHE_SUCCESSIONE] Errore generico. Tempo: {Ms}ms",
                    stopwatch.ElapsedMilliseconds);
                throw;
            }

            return result;
        }
    }
}