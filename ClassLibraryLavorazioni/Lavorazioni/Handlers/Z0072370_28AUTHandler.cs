using Entities.Helpers;
using LibraryLavorazioni.Lavorazioni.Constants;
using LibraryLavorazioni.Lavorazioni.Interfaces;
using LibraryLavorazioni.Lavorazioni.Models;
using LibraryLavorazioni.Utility;
using LibraryLavorazioni.Utility.Interfaces;
using LibraryLavorazioni.Utility.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace LibraryLavorazioni.Lavorazioni.Handlers
{
    /// <summary>
    /// Handler per la lavorazione Z0072370_28AUT seguendo il pattern registry.
    /// Gestisce l'elaborazione dei dati di produzione per la procedura Z0072370_28AUT,
    /// sia per la fase di scansione che di indicizzazione.
    /// </summary>
    public sealed class Z0072370_28AUTHandler : ILavorazioneHandler
    {
        /// <summary>
        /// Codice identificativo univoco della lavorazione.
        /// </summary>
        public string LavorazioneCode => LavorazioniCodes.Z0072370_28AUT;

        /// <summary>
        /// Esegue la lavorazione Z0072370_28AUT utilizzando il pattern registry e dependency injection.
        /// </summary>
        /// <param name="context">Contesto di esecuzione contenente parametri e service provider.</param>
        /// <param name="ct">Token di cancellazione per gestire l'interruzione dell'operazione.</param>
        /// <returns>Lista dei dati di lavorazione elaborati.</returns>
        public async Task<List<DatiLavorazione>> ExecuteAsync(LavorazioneExecutionContext context, CancellationToken ct = default)
        {
            // Risolve le dipendenze tramite service provider
            var normalizzatore = context.ServiceProvider.GetRequiredService<INormalizzatoreOperatori>();
            var gestoreOperatori = context.ServiceProvider.GetRequiredService<IGestoreOperatoriDatiLavorazione>();
            var elaboratore = context.ServiceProvider.GetRequiredService<IElaboratoreDatiLavorazione>();
            var configManager = context.ServiceProvider.GetRequiredService<ILavorazioniConfigManager>();

            // Crea l'istanza della lavorazione specifica
            var lavorazione = new Z0072370_28AUTProcessor(normalizzatore, gestoreOperatori, elaboratore, configManager);
            
            // Imposta il contesto della lavorazione
            lavorazione.NomeProcedura = context.NomeProcedura;
            lavorazione.IDFaseLavorazione = context.IDFaseLavorazione;
            lavorazione.IDProceduraLavorazione = context.IDProceduraLavorazione;
            lavorazione.IDCentro = context.IDCentro;
            lavorazione.StartDataLavorazione = context.StartDataLavorazione;
            lavorazione.EndDataLavorazione = context.EndDataLavorazione;

            // Esegue la lavorazione
            return await lavorazione.SetDatiDematAsync();
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
        /// <param name="normalizzatoreOperatori">Servizio di normalizzazione operatori.</param>
        /// <param name="gestoreOperatoriDati">Servizio di gestione operatori dati lavorazione.</param>
        /// <param name="elaboratoreDati">Servizio di elaborazione dati lavorazione.</param>
        /// <param name="lavorazioniConfigManager">Servizio di configurazione lavorazioni.</param>
        public Z0072370_28AUTProcessor(
            INormalizzatoreOperatori normalizzatoreOperatori,
            IGestoreOperatoriDatiLavorazione gestoreOperatoriDati,
            IElaboratoreDatiLavorazione elaboratoreDati,
            ILavorazioniConfigManager lavorazioniConfigManager
        ) : base(normalizzatoreOperatori, gestoreOperatoriDati, elaboratoreDati)
        {
            _lavorazioniConfigManager = lavorazioniConfigManager;
            _logger = LogManager.Setup().LoadConfigurationFromFile("nlog.config").GetCurrentClassLogger();
        }

        /// <summary>
        /// Recupera e aggrega i dati di lavorazione per la procedura Z0072370_28AUT.
        /// </summary>
        /// <returns>Lista di DatiLavorazione contenente i dati acquisiti dalla fonte dati.</returns>
        public override async Task<List<DatiLavorazione>> SetDatiDematAsync()
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

                result.AddRange(await EseguiQueryAsync(query, startData, endData));
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

                result.AddRange(await EseguiQueryAsync(query, startData, endData));
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
        /// <returns>Lista di DatiLavorazione ottenuta dalla query.</returns>
        private async Task<List<DatiLavorazione>> EseguiQueryAsync(string query, string startData, string endData)
        {
            QueryLoggingHelper.LogQueryExecution(nlogLogger: _logger, queryType: "SELECT", additionalInfo: $"Parametri: startData={startData}, endData={endData}");

            var result = new List<DatiLavorazione>();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                await using var connection = new SqlConnection(_lavorazioniConfigManager.CnxnCaptiva206);
                await connection.OpenAsync();

                await using var cmd = new SqlCommand(query, connection);
                cmd.CommandTimeout = 30;
                cmd.Parameters.AddWithValue("@startData", startData);
                cmd.Parameters.AddWithValue("@endData", endData);

                _logger.Debug($"[Z0072370_28AUT] Esecuzione query con timeout: {cmd.CommandTimeout}s");

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
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




