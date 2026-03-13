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
    /// Handler per la lavorazione ANT_ADER4_SORTER_1_2 seguendo il pattern registry.
    /// Gestisce l'elaborazione dei dati di produzione dalle sorgenti dati
    /// associate ai sistemi ADER4 SORTER 1 e 2 (fase 4 - scansione).
    /// </summary>
    public sealed class Ant_Ader4_Sorter_1_2Handler : IProductionDataHandler
    {
        private readonly ILavorazioniConfigManager _configManager;
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Inizializza una nuova istanza di <see cref="Ant_Ader4_Sorter_1_2Handler"/>.
        /// </summary>
        public Ant_Ader4_Sorter_1_2Handler(
            ILavorazioniConfigManager configManager,
            ILoggerFactory loggerFactory)
        {
            _configManager = configManager;
            _loggerFactory = loggerFactory;
        }

        /// <summary>Codice identificativo univoco della lavorazione.</summary>
        public string HandlerCode => LavorazioniCodes.ANT_ADER4_SORTER_1_2;

        /// <summary>Esegue la lavorazione ANT_ADER4_SORTER_1_2.</summary>
        public async Task<List<DatiLavorazione>> ExecuteAsync(ProductionExecutionContext context, CancellationToken ct = default)
        {
            var lavorazione = new ANT_ADER4_SORTER_1_2Processor(
                _configManager,
                _loggerFactory.CreateLogger<ANT_ADER4_SORTER_1_2Processor>());

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
    /// Classe di lavorazione per la procedura ANT_ADER4_SORTER_1_2.
    /// Legge dati dalla tabella <c>GesimCheck_Local_Produzione.dbo.Tab_Lavorato</c>
    /// da due sorgenti SQL Server distinte (ADER4 SORTER 1 e SORTER 2).
    /// </summary>
    internal sealed class ANT_ADER4_SORTER_1_2Processor : BaseLavorazione
    {
        private readonly ILogger<ANT_ADER4_SORTER_1_2Processor> _logger;
        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;

        /// <summary>
        /// Inizializza una nuova istanza di <see cref="ANT_ADER4_SORTER_1_2Processor"/>.
        /// </summary>
        /// <param name="lavorazioniConfigManager">Servizio di configurazione lavorazioni.</param>
        /// <param name="logger">Logger per la classe.</param>
        public ANT_ADER4_SORTER_1_2Processor(
            ILavorazioniConfigManager lavorazioniConfigManager,
            ILogger<ANT_ADER4_SORTER_1_2Processor> logger)
        {
            _lavorazioniConfigManager = lavorazioniConfigManager;
            _logger = logger;
        }

        /// <summary>
        /// Recupera e aggrega i dati di lavorazione per la procedura ANT_ADER4_SORTER_1_2.
        /// Esegue la query di produzione su entrambe le connessioni configurate (Sorter 1 e Sorter 2).
        /// </summary>
        /// <param name="ct">Token di cancellazione.</param>
        /// <returns>Lista di <see cref="DatiLavorazione"/> acquisiti dalle fonti dati.</returns>
        public override async Task<List<DatiLavorazione>> SetDatiDematAsync(CancellationToken ct = default)
        {
            QueryLoggingHelper.LogQueryExecution(
                logger: _logger,
                queryType: "SELECT",
                entityName: "GesimCheck_Local_Produzione.Tab_Lavorato");

            var startDate = StartDataLavorazione;
            var endDate   = EndDataLavorazione ?? StartDataLavorazione;

            _logger.LogInformation(
                "[ANT_ADER4_SORTER_1_2] Elaborazione dati per IDFaseLavorazione: {IdFase}, Periodo: {Start:dd/MM/yyyy} - {End:dd/MM/yyyy}",
                IDFaseLavorazione, startDate, endDate);

            var result = new List<DatiLavorazione>();

            if (IDFaseLavorazione == 4)
            {
                // dateTime_acquisizione usato direttamente senza CONVERT nel WHERE per sfruttare gli indici
                const string query = """
                    SELECT
                        Username                         AS Operatore,
                        CONVERT(date, dateTime_acquisizione) AS DataLavorazione,
                        COUNT(coduniF)                   AS Documenti,
                        COUNT(coduniF)                   AS Fogli,
                        COUNT(coduniF) * 2               AS Pagine
                    FROM [GesimCheck_Local_Produzione].[dbo].[Tab_Lavorato]
                    WHERE CONVERT(date, dateTime_acquisizione) >= @startDate
                      AND CONVERT(date, dateTime_acquisizione) <= @endDate
                    GROUP BY Username, CONVERT(date, dateTime_acquisizione)
                    """;

                // Legge dati da entrambe le sorgenti ADER4 in sequenza
                await LeggiDatiAsync(query, _lavorazioniConfigManager.CnxnAder4Sorter1!, "ADER4_SORTER_1", startDate, endDate, result, ct);
                await LeggiDatiAsync(query, _lavorazioniConfigManager.CnxnAder4Sorter2!, "ADER4_SORTER_2", startDate, endDate, result, ct);
            }
            else
            {
                _logger.LogWarning("[ANT_ADER4_SORTER_1_2] IDFaseLavorazione {IdFase} non gestito", IDFaseLavorazione);
            }

            _logger.LogInformation(
                "[ANT_ADER4_SORTER_1_2] Elaborazione completata. Record totali ottenuti: {Count}", result.Count);

            return result;
        }

        /// <summary>
        /// Esegue la query su una sorgente SQL Server e aggiunge i risultati alla lista condivisa.
        /// </summary>
        /// <param name="query">Query SQL da eseguire.</param>
        /// <param name="connectionString">Stringa di connessione alla sorgente.</param>
        /// <param name="sourceName">Nome della sorgente (per logging).</param>
        /// <param name="startDate">Data di inizio periodo.</param>
        /// <param name="endDate">Data di fine periodo.</param>
        /// <param name="result">Lista di destinazione dei record letti.</param>
        /// <param name="ct">Token di cancellazione.</param>
        private async Task LeggiDatiAsync(
            string query,
            string connectionString,
            string sourceName,
            DateTime startDate,
            DateTime endDate,
            List<DatiLavorazione> result,
            CancellationToken ct)
        {
            QueryLoggingHelper.LogQueryExecution(logger: _logger);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync(ct);

                await using var cmd = new SqlCommand(query, connection);
                cmd.CommandTimeout = 30;
                cmd.Parameters.Add("@startDate", SqlDbType.DateTime2).Value = startDate;
                cmd.Parameters.Add("@endDate",   SqlDbType.DateTime2).Value = endDate;

                _logger.LogDebug("[ANT_ADER4_SORTER_1_2] Esecuzione query su {Source} con timeout: {Timeout}s",
                    sourceName, cmd.CommandTimeout);

                await using var reader = await cmd.ExecuteReaderAsync(ct);

                // Ordinal pre-calcolati una sola volta per performance
                var ordOp   = reader.GetOrdinal("Operatore");
                var ordData = reader.GetOrdinal("DataLavorazione");
                var ordDoc  = reader.GetOrdinal("Documenti");
                var ordFog  = reader.GetOrdinal("Fogli");
                var ordPag  = reader.GetOrdinal("Pagine");

                int recordCount = 0;
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
                    recordCount++;
                }

                stopwatch.Stop();
                _logger.LogInformation(
                    "[ANT_ADER4_SORTER_1_2] Query su {Source} completata. Record letti: {Count}, Tempo: {Ms}ms",
                    sourceName, recordCount, stopwatch.ElapsedMilliseconds);
            }
            catch (SqlException sqlEx)
            {
                stopwatch.Stop();
                _logger.LogError(sqlEx,
                    "[ANT_ADER4_SORTER_1_2] Errore SQL su {Source}. Tempo: {Ms}ms, Numero: {ErrNum}, Severita: {Class}, Stato: {State}",
                    sourceName, stopwatch.ElapsedMilliseconds, sqlEx.Number, sqlEx.Class, sqlEx.State);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "[ANT_ADER4_SORTER_1_2] Errore generico su {Source}. Tempo: {Ms}ms",
                    sourceName, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }
}