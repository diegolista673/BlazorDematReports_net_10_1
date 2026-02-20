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
    /// Handler per la lavorazione di default seguendo il pattern registry.
    /// Gestisce le lavorazioni standard tramite query SQL configurabili.
    /// </summary>
    public sealed class DefaultLavorazioneHandler : ILavorazioneHandler
    {
        private readonly INormalizzatoreOperatori _normalizzatore;
        private readonly IGestoreOperatoriDatiLavorazione _gestoreOperatori;
        private readonly IElaboratoreDatiLavorazione _elaboratore;
        private readonly ILavorazioniConfigManager _configManager;

        public DefaultLavorazioneHandler(
            INormalizzatoreOperatori normalizzatore,
            IGestoreOperatoriDatiLavorazione gestoreOperatori,
            IElaboratoreDatiLavorazione elaboratore,
            ILavorazioniConfigManager configManager)
        {
            _normalizzatore   = normalizzatore;
            _gestoreOperatori = gestoreOperatori;
            _elaboratore      = elaboratore;
            _configManager    = configManager;
        }

        /// <summary>Codice identificativo univoco della lavorazione di default.</summary>
        public string LavorazioneCode => LavorazioniCodes.DEFAULT;

        /// <summary>Esegue la lavorazione di default tramite query SQL configurabile.</summary>
        public async Task<List<DatiLavorazione>> ExecuteAsync(LavorazioneExecutionContext context, CancellationToken ct = default)
        {
            var lavorazione = new DefaultLavorazioneProcessor(_normalizzatore, _gestoreOperatori, _elaboratore, _configManager);
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
    /// Implementazione di default per la gestione delle lavorazioni.
    /// Fornisce la logica standard per la lettura dei dati di produzione tramite query SQL associate alle fasi di lavorazione.
    /// </summary>
    internal sealed class DefaultLavorazioneProcessor : BaseLavorazione
    {
        private readonly Logger _logger;
        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;

        /// <summary>
        /// Inizializza una nuova istanza di DefaultLavorazioneProcessor.
        /// </summary>
        /// <param name="normalizzatoreOperatori">Servizio di normalizzazione operatori.</param>
        /// <param name="gestoreOperatoriDati">Servizio di gestione operatori dati lavorazione.</param>
        /// <param name="elaboratoreDati">Servizio di elaborazione dati lavorazione.</param>
        /// <param name="lavorazioniConfigManager">Servizio di configurazione lavorazioni.</param>
        public DefaultLavorazioneProcessor(
            INormalizzatoreOperatori normalizzatoreOperatori,
            IGestoreOperatoriDatiLavorazione gestoreOperatoriDati,
            IElaboratoreDatiLavorazione elaboratoreDati,
            ILavorazioniConfigManager lavorazioniConfigManager
        ) : base(normalizzatoreOperatori, gestoreOperatoriDati, elaboratoreDati)
        {
            _lavorazioniConfigManager = lavorazioniConfigManager;
            ElaboraDatiLavorazione = elaboratoreDati;
            _logger = LogManager.Setup().LoadConfigurationFromFile("nlog.config").GetCurrentClassLogger();
        }

        /// <summary>
        /// Implementazione di default per il recupero dei dati di lavorazione.
        /// Utilizza una query SQL generica configurabile.
        /// </summary>
        /// <returns>Lista di DatiLavorazione contenente i dati acquisiti dalla fonte dati.</returns>
        public override async Task<List<DatiLavorazione>> SetDatiDematAsync()
        {
            QueryLoggingHelper.LogQueryExecution(nlogLogger: _logger, queryType: "SELECT", entityName: "DEFAULT_LAVORAZIONE");

            var result = new List<DatiLavorazione>();
            var startData = StartDataLavorazione.ToString("yyyyMMdd");
            var endData = EndDataLavorazione?.ToString("yyyyMMdd") ?? startData;

            _logger.Info($"[DEFAULT_LAVORAZIONE] Elaborazione dati per IDFaseLavorazione: {IDFaseLavorazione}, Periodo: {startData} - {endData}");

            try
            {
                // Query di default per le lavorazioni standard
                string query = @"
                    SELECT 
                        'DefaultOperator' as operatore,
                        CAST(GETDATE() as date) as DataLavorazione,
                        0 as Documenti,
                        0 as Fogli,
                        0 as Pagine";

                result.AddRange(await EseguiQueryDefaultAsync(query, startData, endData));

                _logger.Info($"[DEFAULT_LAVORAZIONE] Elaborazione completata. Record ottenuti: {result.Count}");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"[DEFAULT_LAVORAZIONE] Errore durante l'esecuzione della lavorazione di default");
                throw;
            }

            return result;
        }

        /// <summary>
        /// Legge i dati di produzione tramite una query SQL associata alla fase di lavorazione.
        /// </summary>
        /// <param name="nomeConnessione">Nome della proprietà di configurazione della connessione.</param>
        /// <param name="queryDaEseguire">Query SQL da eseguire.</param>
        /// <param name="nomeProcedura">Nome della procedura di lavorazione.</param>
        /// <param name="startDate">Data di inizio periodo di ricerca.</param>
        /// <param name="endDate">Data di fine periodo di ricerca.</param>
        /// <returns>Lista di DatiLavorazione ottenuta dalla query.</returns>
        public async Task<List<DatiLavorazione>> LeggiDatiProduzioneAsync(
            string nomeConnessione,
            string queryDaEseguire,
            string nomeProcedura,
            DateTime startDate,
            DateTime endDate)
        {
            QueryLoggingHelper.LogQueryExecution(nlogLogger: _logger, queryType: "CUSTOM", additionalInfo: $"Connessione: {nomeConnessione}, Procedura: {nomeProcedura}");

            var result = new List<DatiLavorazione>();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var connectionString = _lavorazioniConfigManager.GetConnectionString(nomeConnessione);
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException($"Connection string '{nomeConnessione}' non trovata nella configurazione");
                }

                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                await using var cmd = new SqlCommand(queryDaEseguire, connection);
                cmd.CommandTimeout = 60;
                cmd.Parameters.AddWithValue("@startDate", startDate);
                cmd.Parameters.AddWithValue("@endDate", endDate);

                _logger.Debug($"[DEFAULT_LAVORAZIONE] Esecuzione query personalizzata con timeout: {cmd.CommandTimeout}s");

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
                        AppartieneAlCentroSelezionato = false // Da verificare in base alla query specifica
                    };

                    result.Add(dati);
                }

                stopwatch.Stop();

                _logger.Info($"[DEFAULT_LAVORAZIONE] Query personalizzata eseguita con successo. " +
                            $"Record letti: {result.Count}, " +
                            $"Tempo esecuzione: {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (SqlException sqlEx)
            {
                stopwatch.Stop();
                _logger.Error(sqlEx, $"[DEFAULT_LAVORAZIONE] Errore SQL durante l'esecuzione della query personalizzata. " +
                                     $"Tempo trascorso: {stopwatch.ElapsedMilliseconds}ms, " +
                                     $"Numero errore: {sqlEx.Number}, " +
                                     $"Severità: {sqlEx.Class}, " +
                                     $"Stato: {sqlEx.State}");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.Error(ex, $"[DEFAULT_LAVORAZIONE] Errore generico durante l'esecuzione della query personalizzata. " +
                                 $"Tempo trascorso: {stopwatch.ElapsedMilliseconds}ms");
                throw;
            }

            return result;
        }

        /// <summary>
        /// Esegue una query SQL di default per recuperare i dati di lavorazione.
        /// </summary>
        /// <param name="query">Query SQL da eseguire.</param>
        /// <param name="startData">Data di inizio periodo in formato stringa (yyyyMMdd).</param>
        /// <param name="endData">Data di fine periodo in formato stringa (yyyyMMdd).</param>
        /// <returns>Lista di DatiLavorazione ottenuta dalla query.</returns>
        private async Task<List<DatiLavorazione>> EseguiQueryDefaultAsync(string query, string startData, string endData)
        {
            QueryLoggingHelper.LogQueryExecution(nlogLogger: _logger, queryType: "SELECT", additionalInfo: $"Parametri: startData={startData}, endData={endData}");

            var result = new List<DatiLavorazione>();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Utilizza una connessione di default se disponibile
                var defaultConnection = _lavorazioniConfigManager.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(defaultConnection))
                {
                    _logger.Warn("[DEFAULT_LAVORAZIONE] Nessuna connessione di default configurata. Restituisco dati mock.");
                    result.Add(new DatiLavorazione
                    {
                        Operatore = "DefaultOperator",
                        DataLavorazione = StartDataLavorazione,
                        Documenti = 0,
                        Fogli = 0,
                        Pagine = 0,
                        AppartieneAlCentroSelezionato = true
                    });
                    return result;
                }

                await using var connection = new SqlConnection(defaultConnection);
                await connection.OpenAsync();

                await using var cmd = new SqlCommand(query, connection);
                cmd.CommandTimeout = 30;

                _logger.Debug($"[DEFAULT_LAVORAZIONE] Esecuzione query di default con timeout: {cmd.CommandTimeout}s");

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

                _logger.Info($"[DEFAULT_LAVORAZIONE] Query di default eseguita con successo. " +
                            $"Record letti: {result.Count}, " +
                            $"Tempo esecuzione: {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.Error(ex, $"[DEFAULT_LAVORAZIONE] Errore durante l'esecuzione della query di default. " +
                                 $"Tempo trascorso: {stopwatch.ElapsedMilliseconds}ms");
                throw;
            }

            return result;
        }
    }
}