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
    /// Handler per la lavorazione Z0082041_SOFTLINE seguendo il pattern registry.
    /// Gestisce l'elaborazione dei dati di produzione da più tabelle Oracle
    /// relative alla procedura Z0082041_SOFTLINE.
    /// </summary>
    public sealed class Z0082041_SOFTLINEHandler : ILavorazioneHandler
    {
        /// <summary>
        /// Codice identificativo univoco della lavorazione.
        /// </summary>
        public string LavorazioneCode => LavorazioniCodes.Z0082041_SOFTLINE;

        /// <summary>
        /// Esegue la lavorazione Z0082041_SOFTLINE utilizzando il pattern registry e dependency injection.
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
            var lavorazione = new Z0082041_SOFTLINEProcessor(normalizzatore, gestoreOperatori, elaboratore, configManager);
            
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
    /// Classe di lavorazione per la procedura Z0082041_SOFTLINE.
    /// Implementa la logica di lettura e aggregazione dei dati di produzione da più tabelle Oracle
    /// relative alla procedura Z0082041_SOFTLINE, gestendo sia la fase di scansione che di indicizzazione.
    /// </summary>
    internal sealed class Z0082041_SOFTLINEProcessor : BaseLavorazione
    {
        private readonly Logger _logger;
        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;

        /// <summary>
        /// Inizializza una nuova istanza di Z0082041_SOFTLINEProcessor.
        /// </summary>
        /// <param name="normalizzatoreOperatori">Servizio di normalizzazione operatori.</param>
        /// <param name="gestoreOperatoriDati">Servizio di gestione operatori dati lavorazione.</param>
        /// <param name="elaboratoreDati">Servizio di elaborazione dati lavorazione.</param>
        /// <param name="lavorazioniConfigManager">Servizio di configurazione lavorazioni.</param>
        public Z0082041_SOFTLINEProcessor(
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
        /// Recupera e aggrega i dati di lavorazione per la procedura Z0082041_SOFTLINE.
        /// Esegue la query di produzione sulle tabelle Oracle in base alla fase di lavorazione.
        /// </summary>
        /// <returns>Lista di DatiLavorazione contenente i dati acquisiti dalla fonte dati.</returns>
        public override async Task<List<DatiLavorazione>> SetDatiDematAsync()
        {
            QueryLoggingHelper.LogQueryExecution(nlogLogger: _logger, queryType: "SELECT", entityName: "Oracle_Z0082041_SOFTLINE_Tables");

            var result = new List<DatiLavorazione>();
            var startData = StartDataLavorazione.ToString("yyyyMMdd");
            var endData = EndDataLavorazione?.ToString("yyyyMMdd") ?? startData;

            _logger.Info($"[Z0082041_SOFTLINE] Elaborazione dati per IDFaseLavorazione: {IDFaseLavorazione}, Periodo: {startData} - {endData}");

            if (IDFaseLavorazione == 4)
            {
                // Query per fase di scansione
                string query = @"
                    SELECT 
                        OP_SCAN as operatore,
                        TRUNC(DATA_SCAN) as DataLavorazione,
                        COUNT(*) as Documenti,
                        SUM(NVL(NUM_PAG, 0))/2 AS Fogli,
                        SUM(NVL(NUM_PAG, 0)) as Pagine
                    FROM Z0082041_SOFTLINE_DETTAGLIO
                    WHERE TRUNC(DATA_SCAN) >= TO_DATE(:startData, 'YYYYMMDD')
                    AND TRUNC(DATA_SCAN) <= TO_DATE(:endData, 'YYYYMMDD')
                    GROUP BY OP_SCAN, TRUNC(DATA_SCAN)";

                result.AddRange(await EseguiQueryOracleAsync(query, startData, endData));
            }
            else if (IDFaseLavorazione == 5)
            {
                // Query per fase di indicizzazione
                string query = @"
                    SELECT 
                        OP_INDEX as operatore,
                        TRUNC(DATA_INDEX) as DataLavorazione,
                        COUNT(*) as Documenti,
                        SUM(NVL(NUM_PAG, 0))/2 AS Fogli,
                        SUM(NVL(NUM_PAG, 0)) as Pagine
                    FROM Z0082041_SOFTLINE_DETTAGLIO
                    WHERE TRUNC(DATA_INDEX) >= TO_DATE(:startData, 'YYYYMMDD')
                    AND TRUNC(DATA_INDEX) <= TO_DATE(:endData, 'YYYYMMDD')
                    GROUP BY OP_INDEX, TRUNC(DATA_INDEX)";

                result.AddRange(await EseguiQueryOracleAsync(query, startData, endData));
            }
            else
            {
                _logger.Warn($"[Z0082041_SOFTLINE] IDFaseLavorazione {IDFaseLavorazione} non gestito");
            }

            _logger.Info($"[Z0082041_SOFTLINE] Elaborazione completata. Record ottenuti: {result.Count}");

            return result;
        }

        /// <summary>
        /// Esegue una query Oracle per recuperare i dati di lavorazione dalla tabella specificata.
        /// </summary>
        /// <param name="query">Query Oracle da eseguire.</param>
        /// <param name="startData">Data di inizio periodo in formato stringa (yyyyMMdd).</param>
        /// <param name="endData">Data di fine periodo in formato stringa (yyyyMMdd).</param>
        /// <returns>Lista di DatiLavorazione ottenuta dalla query.</returns>
        private async Task<List<DatiLavorazione>> EseguiQueryOracleAsync(string query, string startData, string endData)
        {
            QueryLoggingHelper.LogQueryExecution(nlogLogger: _logger, queryType: "SELECT", additionalInfo: $"Parametri: startData={startData}, endData={endData}");

            var result = new List<DatiLavorazione>();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Usa la connessione Oracle disponibile tramite GetConnectionString
                var oracleConnection = _lavorazioniConfigManager.GetConnectionString("OracleConnectionString") 
                                     ?? _lavorazioniConfigManager.CnxnCaptiva206; // Fallback
                
                await using var connection = new OracleConnection(oracleConnection);
                await connection.OpenAsync();

                await using var cmd = new OracleCommand(query, connection);
                cmd.CommandTimeout = 30;
                cmd.Parameters.Add(new OracleParameter("startData", startData));
                cmd.Parameters.Add(new OracleParameter("endData", endData));

                _logger.Debug($"[Z0082041_SOFTLINE] Esecuzione query Oracle con timeout: {cmd.CommandTimeout}s");

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
                        AppartieneAlCentroSelezionato = true
                    };

                    result.Add(dati);
                }

                stopwatch.Stop();

                _logger.Info($"[Z0082041_SOFTLINE] Query Oracle eseguita con successo. " +
                            $"Record letti: {result.Count}, " +
                            $"Tempo esecuzione: {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (OracleException oracleEx)
            {
                stopwatch.Stop();
                _logger.Error(oracleEx, $"[Z0082041_SOFTLINE] Errore Oracle durante l'esecuzione della query. " +
                                        $"Tempo trascorso: {stopwatch.ElapsedMilliseconds}ms, " +
                                        $"Numero errore: {oracleEx.Number}, " +
                                        $"Messaggio: {oracleEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.Error(ex, $"[Z0082041_SOFTLINE] Errore generico durante l'esecuzione della query. " +
                                 $"Tempo trascorso: {stopwatch.ElapsedMilliseconds}ms");
                throw;
            }

            return result;
        }
    }
}
