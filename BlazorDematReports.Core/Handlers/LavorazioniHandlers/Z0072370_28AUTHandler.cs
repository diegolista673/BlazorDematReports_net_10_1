using BlazorDematReports.Core.Constants;
using BlazorDematReports.Core.Lavorazioni.Interfaces;
using BlazorDematReports.Core.Lavorazioni.Models;
using BlazorDematReports.Core.Utility;
using BlazorDematReports.Core.Utility.Interfaces;
using BlazorDematReports.Core.Utility.Models;
using Entities.Helpers;
using Microsoft.Data.SqlClient;
using NLog;

namespace BlazorDematReports.Core.Handlers.LavorazioniHandlers
{
    /// <summary>
    /// Handler per la lavorazione Z0072370_28AUT seguendo il pattern registry.
    /// Gestisce l'elaborazione dei dati di produzione per la procedura Z0072370_28AUT,
    /// sia per la fase di scansione che di indicizzazione.
    /// </summary>
    public sealed class Z0072370_28AutHandler : IProductionDataHandler
    {
        private readonly ILavorazioniConfigManager _configManager;

        public Z0072370_28AutHandler(ILavorazioniConfigManager configManager)
        {
            _configManager = configManager;
        }

        /// <summary>Codice identificativo univoco della lavorazione.</summary>
        public string HandlerCode => LavorazioniCodes.Z0072370_28AUT;

        /// <summary>Esegue la lavorazione Z0072370_28AUT.</summary>
        public async Task<List<DatiLavorazione>> ExecuteAsync(ProductionExecutionContext context, CancellationToken ct = default)
        {
            var lavorazione = new Z0072370_28AUTProcessor(_configManager);
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
    /// Implementa la logica di lettura e aggregazione dei dati di produzione dalla tabella SQL Server.
    /// </summary>
    internal sealed class Z0072370_28AUTProcessor : BaseLavorazione
    {
        private readonly Logger _logger;
        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;

        /// <summary>
        /// Inizializza una nuova istanza di Z0072370_28AUTProcessor.
        /// </summary>
        /// <param name="lavorazioniConfigManager">Servizio di configurazione lavorazioni.</param>
        public Z0072370_28AUTProcessor(
            ILavorazioniConfigManager lavorazioniConfigManager
        )
        {
            _lavorazioniConfigManager = lavorazioniConfigManager;
            _logger = LogManager.Setup().LoadConfigurationFromFile("nlog.config").GetCurrentClassLogger();
        }

        /// <summary>
        /// Recupera e aggrega i dati di lavorazione per la procedura Z0072370_28AUT.
        /// </summary>
        /// <param name="ct">Token di cancellazione.</param>
        /// <returns>Lista di DatiLavorazione contenente i dati acquisiti dalla fonte dati.</returns>
        public override async Task<List<DatiLavorazione>> SetDatiDematAsync(CancellationToken ct = default)
        {
            QueryLoggingHelper.LogQueryExecution(nlogLogger: _logger, queryType: "SELECT", entityName: "Z0072370_RDMKT_28AUT_GE_UDA_DETTAGLIO");

            var result = new List<DatiLavorazione>();
            var startData = StartDataLavorazione.ToString("yyyyMMdd");
            var endData = EndDataLavorazione?.ToString("yyyyMMdd") ?? startData;

            _logger.Info($"[Z0072370_28AUT] Elaborazione dati per IDFaseLavorazione: {IDFaseLavorazione}, Periodo: {startData} - {endData}");

            if (IDFaseLavorazione == 4)
            {
                string query = @"
                    select 
                        OP_SCAN as operatore,
                        convert(date, DATA_SCAN) as DataLavorazione,
                        COUNT(*) as Documenti,
                        SUM(convert(int,NUM_PAG))/2 AS Fogli,
                        SUM(convert(int,num_pag)) as Pagine
                    from Z0072370_RDMKT_28AUT_GE_UDA_DETTAGLIO
                    where convert(date, DATA_SCAN) >= @startData and convert(date, DATA_SCAN) <= @endData and department = 'GENOVA'
                    group by OP_SCAN, convert(date, DATA_SCAN)";

                result.AddRange(await EseguiQueryAsync(query, startData, endData, ct));
            }
            else if (IDFaseLavorazione == 5)
            {
                string query = @"
                    select 
                        OP_INDEX as operatore,
                        convert(date, DATA_INDEX) as DataLavorazione,
                        COUNT(*) as Documenti,
                        SUM(convert(int,NUM_PAG))/2 AS Fogli,
                        SUM(convert(int,NUM_PAG)) as Pagine
                    from Z0072370_RDMKT_28AUT_GE_UDA_DETTAGLIO
                    where convert(date, DATA_INDEX) >= @startData and convert(date, DATA_INDEX) <= @endData and department = 'GENOVA'
                    group by OP_INDEX, convert(date, DATA_INDEX), OP_SCAN";

                result.AddRange(await EseguiQueryAsync(query, startData, endData, ct));
            }
            else
            {
                _logger.Warn($"[Z0072370_28AUT] IDFaseLavorazione {IDFaseLavorazione} non gestito");
            }

            _logger.Info($"[Z0072370_28AUT] Elaborazione completata. Record ottenuti: {result.Count}");

            return result;
        }

        /// <summary>
        /// Esegue una query SQL per recuperare i dati di lavorazione dalla tabella specificata.
        /// </summary>
        /// <param name="query">Query SQL da eseguire.</param>
        /// <param name="startData">Data di inizio periodo in formato stringa (yyyyMMdd).</param>
        /// <param name="endData">Data di fine periodo in formato stringa (yyyyMMdd).</param>
        /// <param name="ct">Token di cancellazione.</param>
        /// <returns>Lista di DatiLavorazione ottenuta dalla query.</returns>
        private async Task<List<DatiLavorazione>> EseguiQueryAsync(string query, string startData, string endData, CancellationToken ct = default)
        {
            QueryLoggingHelper.LogQueryExecution(nlogLogger: _logger, queryType: "SELECT", additionalInfo: $"Parametri: startData={startData}, endData={endData}");

            var result = new List<DatiLavorazione>();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                await using var connection = new SqlConnection(_lavorazioniConfigManager.CnxnCaptiva206);
                await connection.OpenAsync(ct);

                await using var cmd = new SqlCommand(query, connection);
                cmd.CommandTimeout = 30;
                cmd.Parameters.AddWithValue("@startData", startData);
                cmd.Parameters.AddWithValue("@endData", endData);

                _logger.Debug($"[Z0072370_28AUT] Esecuzione query con timeout: {cmd.CommandTimeout}s");

                using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    var dati = new DatiLavorazione
                    {
                        Operatore = reader["operatore"] as string,
                        DataLavorazione = reader.GetDateTime(reader.GetOrdinal("DataLavorazione")),
                        Documenti = reader["Documenti"] != DBNull.Value ? Convert.ToInt32(reader["Documenti"]) : null,
                        Fogli = reader["Fogli"] != DBNull.Value ? Convert.ToInt32(reader["Fogli"]) : null,
                        Pagine = reader["Pagine"] != DBNull.Value ? Convert.ToInt32(reader["Pagine"]) : null,
                        // Flag true perché la query ha filtro department='GENOVA' => tutti i risultati appartengono al centro selezionato
                        AppartieneAlCentroSelezionato = true
                    };

                    result.Add(dati);
                }

                stopwatch.Stop();

                _logger.Info($"[Z0072370_28AUT] Query eseguita con successo. " +
                            $"Record letti: {result.Count}, " +
                            $"Tempo esecuzione: {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (SqlException sqlEx)
            {
                stopwatch.Stop();
                _logger.Error(sqlEx, $"[Z0072370_28AUT] Errore SQL durante l'esecuzione della query. " +
                                     $"Tempo trascorso: {stopwatch.ElapsedMilliseconds}ms, " +
                                     $"Numero errore: {sqlEx.Number}, " +
                                     $"Severità: {sqlEx.Class}, " +
                                     $"Stato: {sqlEx.State}");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.Error(ex, $"[Z0072370_28AUT] Errore generico durante l'esecuzione della query. " +
                                 $"Tempo trascorso: {stopwatch.ElapsedMilliseconds}ms");
                throw;
            }

            return result;
        }
    }
}




