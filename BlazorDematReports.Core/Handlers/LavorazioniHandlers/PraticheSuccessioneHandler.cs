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
    public sealed class PraticheSuccessioneHandler : ILavorazioneHandler
    {
        private readonly INormalizzatoreOperatori _normalizzatore;
        private readonly IGestoreOperatoriDatiLavorazione _gestoreOperatori;
        private readonly IElaboratoreDatiLavorazione _elaboratore;
        private readonly ILavorazioniConfigManager _configManager;
        private readonly ILoggerFactory _loggerFactory;

        public PraticheSuccessioneHandler(
            INormalizzatoreOperatori normalizzatore,
            IGestoreOperatoriDatiLavorazione gestoreOperatori,
            IElaboratoreDatiLavorazione elaboratore,
            ILavorazioniConfigManager configManager,
            ILoggerFactory loggerFactory)
        {
            _normalizzatore   = normalizzatore;
            _gestoreOperatori = gestoreOperatori;
            _elaboratore      = elaboratore;
            _configManager    = configManager;
            _loggerFactory    = loggerFactory;
        }

        /// <summary>Codice identificativo univoco della lavorazione.</summary>
        public string LavorazioneCode => LavorazioniCodes.PRATICHE_SUCCESSIONE;

        /// <summary>Esegue la lavorazione PRATICHE_SUCCESSIONE.</summary>
        public async Task<List<DatiLavorazione>> ExecuteAsync(LavorazioneExecutionContext context, CancellationToken ct = default)
        {
            var lavorazione = new PRATICHE_SUCCESSIONEProcessor(
                _normalizzatore, _gestoreOperatori, _elaboratore, _configManager,
                _loggerFactory.CreateLogger<PRATICHE_SUCCESSIONEProcessor>());

            lavorazione.NomeProcedura          = context.NomeProcedura;
            lavorazione.IDFaseLavorazione      = context.IDFaseLavorazione;
            lavorazione.IDProceduraLavorazione = context.IDProceduraLavorazione;
            lavorazione.IDCentro               = context.IDCentro;
            lavorazione.StartDataLavorazione   = context.StartDataLavorazione;
            lavorazione.EndDataLavorazione     = context.EndDataLavorazione;

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
        private readonly ILogger<PRATICHE_SUCCESSIONEProcessor> _logger;
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
            ILavorazioniConfigManager lavorazioniConfigManager,
            ILogger<PRATICHE_SUCCESSIONEProcessor> logger
        ) : base(normalizzatoreOperatori, gestoreOperatoriDati, elaboratoreDati)
        {
            _lavorazioniConfigManager = lavorazioniConfigManager;
            _logger = logger;
        }

        /// <summary>
        /// Recupera e aggrega i dati di lavorazione per la procedura PRATICHE_SUCCESSIONE.
        /// Gestisce le diverse fasi di lavorazione per le pratiche di successione.
        /// </summary>
        /// <returns>Lista di DatiLavorazione contenente i dati acquisiti dalla fonte dati.</returns>
        public override async Task<List<DatiLavorazione>> SetDatiDematAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger: _logger);

            var result = new List<DatiLavorazione>();
            var startData = StartDataLavorazione.ToString("yyyyMMdd");
            var endData = EndDataLavorazione?.ToString("yyyyMMdd") ?? startData;

            _logger.LogInformation("[PRATICHE_SUCCESSIONE] Elaborazione dati per IDFaseLavorazione: {IdFase}, Periodo: {Start} - {End}", IDFaseLavorazione, startData, endData);

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
                _logger.LogWarning("[PRATICHE_SUCCESSIONE] IDFaseLavorazione {IdFase} non gestito", IDFaseLavorazione);
            }

            _logger.LogInformation("[PRATICHE_SUCCESSIONE] Elaborazione completata. Record ottenuti: {Count}", result.Count);

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
            QueryLoggingHelper.LogQueryExecution(logger: _logger);

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

                _logger.LogDebug("[PRATICHE_SUCCESSIONE] Esecuzione query Oracle con timeout: {Timeout}s", cmd.CommandTimeout);

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

                _logger.LogInformation("[PRATICHE_SUCCESSIONE] Query Oracle eseguita con successo. Record letti: {Count}, Tempo: {Ms}ms", result.Count, stopwatch.ElapsedMilliseconds);
            }
            catch (OracleException oracleEx)
            {
                stopwatch.Stop();
                _logger.LogError(oracleEx, "[PRATICHE_SUCCESSIONE] Errore Oracle. Tempo: {Ms}ms, Numero: {ErrNum}", stopwatch.ElapsedMilliseconds, oracleEx.Number);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "[PRATICHE_SUCCESSIONE] Errore generico. Tempo: {Ms}ms", stopwatch.ElapsedMilliseconds);
                throw;
            }

            return result;
        }
    }
}