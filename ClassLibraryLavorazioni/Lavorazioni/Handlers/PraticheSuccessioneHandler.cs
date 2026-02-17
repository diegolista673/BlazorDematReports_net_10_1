using Entities.Helpers;
using LibraryLavorazioni.Lavorazioni.Constants;
using LibraryLavorazioni.Lavorazioni.Interfaces;
using LibraryLavorazioni.Lavorazioni.Models;
using LibraryLavorazioni.Utility;
using LibraryLavorazioni.Utility.Interfaces;
using LibraryLavorazioni.Utility.Models;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using Oracle.ManagedDataAccess.Client;

namespace LibraryLavorazioni.Lavorazioni.Handlers
{
    /// <summary>
    /// Handler per la lavorazione PRATICHE_SUCCESSIONE seguendo il pattern registry.
    /// Gestisce l'elaborazione dei dati di produzione dalla sorgente Oracle
    /// relativa alle pratiche di successione.
    /// </summary>
    public sealed class PraticheSuccessioneHandler : ILavorazioneHandler
    {
        /// <summary>
        /// Codice identificativo univoco della lavorazione.
        /// </summary>
        public string LavorazioneCode => LavorazioniCodes.PRATICHE_SUCCESSIONE;

        /// <summary>
        /// Esegue la lavorazione PRATICHE_SUCCESSIONE utilizzando il pattern registry e dependency injection.
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
            var lavorazione = new PRATICHE_SUCCESSIONEProcessor(normalizzatore, gestoreOperatori, elaboratore, configManager);
            
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
    /// Classe di lavorazione per la procedura PRATICHE_SUCCESSIONE.
    /// Implementa la logica di lettura e aggregazione dei dati di produzione dalla sorgente Oracle
    /// relativa alle pratiche di successione, gestendo le diverse fasi di lavorazione.
    /// </summary>
    internal sealed class PRATICHE_SUCCESSIONEProcessor : BaseLavorazione
    {
        private readonly Logger _logger;
        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;

        /// <summary>
        /// Inizializza una nuova istanza di PRATICHE_SUCCESSIONEProcessor.
        /// </summary>
        /// <param name="normalizzatoreOperatori">Servizio di normalizzazione operatori.</param>
        /// <param name="gestoreOperatoriDati">Servizio di gestione operatori dati lavorazione.</param>
        /// <param name="elaboratoreDati">Servizio di elaborazione dati lavorazione.</param>
        /// <param name="lavorazioniConfigManager">Servizio di configurazione lavorazioni.</param>
        public PRATICHE_SUCCESSIONEProcessor(
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
        /// Recupera e aggrega i dati di lavorazione per la procedura PRATICHE_SUCCESSIONE.
        /// Gestisce le diverse fasi di lavorazione per le pratiche di successione.
        /// </summary>
        /// <returns>Lista di DatiLavorazione contenente i dati acquisiti dalla fonte dati.</returns>
        public override async Task<List<DatiLavorazione>> SetDatiDematAsync()
        {
            QueryLoggingHelper.LogQueryExecution(nlogLogger: _logger, queryType: "SELECT", entityName: "bp_pratichesucc_s");

            var result = new List<DatiLavorazione>();
            var startData = StartDataLavorazione.ToString("yyyyMMdd");
            var endData = EndDataLavorazione?.ToString("yyyyMMdd") ?? startData;

            _logger.Info($"[PRATICHE_SUCCESSIONE] Elaborazione dati per IDFaseLavorazione: {IDFaseLavorazione}, Periodo: {startData} - {endData}");

            if (IDFaseLavorazione == 4)
            {
                // Query per fase di scansione delle pratiche di successione
                string query = @"
                    select pt_operatore_scan as operatore, 
                           TRUNC(pt_data_scan) as datalavorazione, 
                           TRUNC(count(pt_barcode_ad_uso_interno)) as documenti, 
                           TRUNC(SUM(pt_numero_pagine/2)) AS Fogli, 
                           TRUNC(sum(pt_numero_pagine)) as pagine
                    from bp_pratichesucc_s
                    where TRUNC(pt_data_scan) >= to_date(:startData,'yyyymmdd') 
                    and TRUNC(pt_data_scan) <= to_date(:endData,'yyyymmdd')
                    group by pt_operatore_scan, TRUNC(pt_data_scan)";

                result.AddRange(await EseguiQueryAsync(query, startData, endData));
            }
            else if (IDFaseLavorazione == 5)
            {
                // Query per fase di indicizzazione delle pratiche di successione
                string query = @"
                    select pt_operatore_index as operatore, 
                           TRUNC(pt_data_index) as datalavorazione,  
                           TRUNC(count(pt_barcode_ad_uso_interno)) as documenti, 
                           TRUNC(SUM(pt_numero_pagine/2)) AS Fogli, 
                           TRUNC(sum(pt_numero_pagine)) as pagine
                    from bp_pratichesucc_s 
                    where TRUNC(pt_data_index) >= to_date(:startData,'yyyymmdd') 
                    and TRUNC(pt_data_index) <= to_date(:endData,'yyyymmdd') 
                    group by pt_operatore_index, TRUNC(pt_data_index)";

                result.AddRange(await EseguiQueryAsync(query, startData, endData));
            }
            else
            {
                _logger.Warn($"[PRATICHE_SUCCESSIONE] IDFaseLavorazione {IDFaseLavorazione} non gestito");
            }

            _logger.Info($"[PRATICHE_SUCCESSIONE] Elaborazione completata. Record ottenuti: {result.Count}");

            return result;
        }

        /// <summary>
        /// Esegue una query asincrona su Oracle per recuperare i dati di lavorazione delle pratiche di successione.
        /// </summary>
        /// <param name="query">Query Oracle da eseguire.</param>
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
                await using var connection = new OracleConnection(_lavorazioniConfigManager.CnxnPraticheSuccessione);
                await connection.OpenAsync();

                await using var cmd = connection.CreateCommand();
                cmd.CommandTimeout = 30;
                cmd.BindByName = true;
                cmd.CommandText = query;
                cmd.Parameters.Add("startData", startData);
                cmd.Parameters.Add("endData", endData);

                _logger.Debug($"[PRATICHE_SUCCESSIONE] Esecuzione query Oracle con timeout: {cmd.CommandTimeout}s");

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var dati = new DatiLavorazione
                    {
                        Operatore = reader["operatore"] as string,
                        DataLavorazione = reader.GetDateTime(reader.GetOrdinal("datalavorazione")),
                        Documenti = reader["documenti"] != DBNull.Value ? Convert.ToInt32(reader["documenti"]) : null,
                        Fogli = reader["fogli"] != DBNull.Value ? Convert.ToInt32(reader["fogli"]) : null,
                        Pagine = reader["pagine"] != DBNull.Value ? Convert.ToInt32(reader["pagine"]) : null,
                        AppartieneAlCentroSelezionato = true
                    };

                    result.Add(dati);
                }

                stopwatch.Stop();

                _logger.Info($"[PRATICHE_SUCCESSIONE] Query Oracle eseguita con successo. " +
                            $"Record letti: {result.Count}, " +
                            $"Tempo esecuzione: {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (OracleException oracleEx)
            {
                stopwatch.Stop();
                _logger.Error(oracleEx, $"[PRATICHE_SUCCESSIONE] Errore Oracle durante l'esecuzione della query. " +
                                        $"Tempo trascorso: {stopwatch.ElapsedMilliseconds}ms, " +
                                        $"Numero errore: {oracleEx.Number}, " +
                                        $"Messaggio: {oracleEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.Error(ex, $"[PRATICHE_SUCCESSIONE] Errore generico durante l'esecuzione della query. " +
                                 $"Tempo trascorso: {stopwatch.ElapsedMilliseconds}ms");
                throw;
            }

            return result;
        }
    }
}