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
    /// Handler per la lavorazione ANT_ADER4_SORTER_1_2 seguendo il pattern registry.
    /// Gestisce l'elaborazione dei dati di produzione dalle sorgenti dati
    /// associate ai sistemi ADER4 SORTER 1 e 2.
    /// </summary>
    public sealed class Ant_Ader4_Sorter_1_2Handler : ILavorazioneHandler
    {
        private readonly ILavorazioniConfigManager _configManager;

        public Ant_Ader4_Sorter_1_2Handler(
            ILavorazioniConfigManager configManager)
        {
            _configManager    = configManager;
        }

        /// <summary>Codice identificativo univoco della lavorazione.</summary>
        public string LavorazioneCode => LavorazioniCodes.ANT_ADER4_SORTER_1_2;

        /// <summary>Esegue la lavorazione ANT_ADER4_SORTER_1_2.</summary>
        public async Task<List<DatiLavorazione>> ExecuteAsync(LavorazioneExecutionContext context, CancellationToken ct = default)
        {
            var lavorazione = new ANT_ADER4_SORTER_1_2Processor(_configManager);
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
    /// Implementa la logica di lettura e aggregazione dei dati di produzione dalle sorgenti dati
    /// associate ai sistemi ADER4 SORTER 1 e 2.
    /// </summary>
    internal sealed class ANT_ADER4_SORTER_1_2Processor : BaseLavorazione
    {
        private readonly Logger _logger;
        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;

        /// <summary>
        /// Inizializza una nuova istanza di ANT_ADER4_SORTER_1_2Processor.
        /// </summary>
        /// <param name="lavorazioniConfigManager">Servizio di configurazione lavorazioni.</param>
        public ANT_ADER4_SORTER_1_2Processor(
            ILavorazioniConfigManager lavorazioniConfigManager
        )
        {
            _lavorazioniConfigManager = lavorazioniConfigManager;
            _logger = LogManager.Setup().LoadConfigurationFromFile("nlog.config").GetCurrentClassLogger();
        }

        /// <summary>
        /// Recupera e aggrega i dati di lavorazione per la procedura ANT_ADER4_SORTER_1_2.
        /// Esegue la query di produzione su entrambe le connessioni configurate (Sorter 1 e Sorter 2).
        /// </summary>
        /// <param name="ct">Token di cancellazione.</param>
        /// <returns>Lista di DatiLavorazione contenente i dati acquisiti dalle fonti dati.</returns>
        public override async Task<List<DatiLavorazione>> SetDatiDematAsync(CancellationToken ct = default)
        {
            QueryLoggingHelper.LogQueryExecution(nlogLogger: _logger, queryType: "SELECT", entityName: "GesimCheck_Local_Produzione.Tab_Lavorato");

            var result = new List<DatiLavorazione>();
            var startDataScan = StartDataLavorazione.ToString("yyyyMMdd");
            var endDataScan = EndDataLavorazione == null ? startDataScan : EndDataLavorazione.Value.ToString("yyyyMMdd");

            _logger.Info($"[ANT_ADER4_SORTER_1_2] Elaborazione dati per IDFaseLavorazione: {IDFaseLavorazione}, Periodo: {startDataScan} - {endDataScan}");

            if (IDFaseLavorazione == 4)
            {
                string query = @"SELECT Username as Operatore, convert(date, dateTime_acquisizione) as DataLavorazione, 
								 count(coduniF) as Documenti, count(coduniF) as Fogli, (count(coduniF) * 2) as Pagine
								 FROM [GesimCheck_Local_Produzione].[dbo].[Tab_Lavorato]
								 WHERE convert(date, dateTime_acquisizione) >= @startDataScan
								 AND convert(date, dateTime_acquisizione) <= @endDataScan
								 GROUP BY Username, CONVERT(date, dateTime_acquisizione)";

				async Task LeggiDatiAsync(string connectionString, string sourceName, CancellationToken ct)
				{
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    try
                    {
                        await using var connection = new SqlConnection(connectionString);
                        await connection.OpenAsync(ct);

                        await using var command = new SqlCommand(query, connection);
                        command.CommandTimeout = 30;
                        command.Parameters.AddWithValue("@startDataScan", startDataScan);
                        command.Parameters.AddWithValue("@endDataScan", endDataScan);

                        _logger.Debug($"[ANT_ADER4_SORTER_1_2] Esecuzione query su {sourceName} con timeout: {command.CommandTimeout}s");

                        using var reader = await command.ExecuteReaderAsync(ct);
                        int recordCount = 0;
                        while (await reader.ReadAsync(ct))
                        {
                            result.Add(new DatiLavorazione
                            {
                                Operatore = reader["Operatore"] as string,
                                DataLavorazione = reader.GetDateTime(reader.GetOrdinal("DataLavorazione")),
                                Documenti = reader["Documenti"] != DBNull.Value ? Convert.ToInt32(reader["Documenti"]) : null,
                                Fogli = reader["Fogli"] != DBNull.Value ? Convert.ToInt32(reader["Fogli"]) : null,
                                Pagine = reader["Pagine"] != DBNull.Value ? Convert.ToInt32(reader["Pagine"]) : null,
                                AppartieneAlCentroSelezionato = true
                            });
                            recordCount++;
                        }

                        stopwatch.Stop();
                        _logger.Info($"[ANT_ADER4_SORTER_1_2] Query su {sourceName} completata. " +
                                    $"Record letti: {recordCount}, " +
                                    $"Tempo esecuzione: {stopwatch.ElapsedMilliseconds}ms");
                    }
                    catch (SqlException sqlEx)
                    {
                        stopwatch.Stop();
                        _logger.Error(sqlEx, $"[ANT_ADER4_SORTER_1_2] Errore SQL su {sourceName}. " +
                                             $"Tempo trascorso: {stopwatch.ElapsedMilliseconds}ms, " +
                                             $"Numero errore: {sqlEx.Number}, " +
                                             $"Severità: {sqlEx.Class}, " +
                                             $"Stato: {sqlEx.State}");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        stopwatch.Stop();
                        _logger.Error(ex, $"[ANT_ADER4_SORTER_1_2] Errore generico su {sourceName}. " +
                                         $"Tempo trascorso: {stopwatch.ElapsedMilliseconds}ms");
                        throw;
                    }
                }

                // Legge dati da entrambe le sorgenti ADER4
                await LeggiDatiAsync(_lavorazioniConfigManager.CnxnAder4Sorter1!, "ADER4_SORTER_1", ct);
                await LeggiDatiAsync(_lavorazioniConfigManager.CnxnAder4Sorter2!, "ADER4_SORTER_2", ct);
            }
            else
            {
                _logger.Warn($"[ANT_ADER4_SORTER_1_2] IDFaseLavorazione {IDFaseLavorazione} non gestito");
            }

            _logger.Info($"[ANT_ADER4_SORTER_1_2] Elaborazione completata. Record totali ottenuti: {result.Count}");

            return result;
        }
    }
}




