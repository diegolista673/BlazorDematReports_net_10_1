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
    /// Handler per la lavorazione RDMKT_RSP seguendo il pattern registry.
    /// Gestisce l'elaborazione dei dati di produzione da tabelle dinamiche SQL Server
    /// relative alla procedura RDMKT_RSP.
    /// </summary>
    public sealed class Rdmkt_RSPHandler : IProductionDataHandler
    {
        private readonly ILavorazioniConfigManager _configManager;

        public Rdmkt_RSPHandler(ILavorazioniConfigManager configManager)
        {
            _configManager = configManager;
        }

        /// <summary>Codice identificativo univoco della lavorazione.</summary>
        public string HandlerCode => LavorazioniCodes.RDMKT_RSP;

        /// <summary>Esegue la lavorazione RDMKT_RSP.</summary>
        public async Task<List<DatiLavorazione>> ExecuteAsync(ProductionExecutionContext context, CancellationToken ct = default)
        {
            var lavorazione = new RDMKT_RSPProcessor(_configManager);
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
    /// Classe di lavorazione per la procedura RDMKT_RSP.
    /// Implementa la logica di lettura e aggregazione dei dati di produzione da tabelle dinamiche SQL Server
    /// relative alla procedura RDMKT_RSP, gestendo sia la fase di scansione che di indicizzazione.
    /// </summary>
    internal sealed class RDMKT_RSPProcessor : BaseLavorazione
    {
        private readonly Logger _logger;
        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;

        /// <summary>
        /// Inizializza una nuova istanza di RDMKT_RSPProcessor.
        /// </summary>
        /// <param name="normalizzatoreOperatori">Servizio di normalizzazione operatori.</param>
        /// <param name="gestoreOperatoriDati">Servizio di gestione operatori dati lavorazione.</param>
        /// <param name="elaboratoreDati">Servizio di elaborazione dati lavorazione.</param>
        /// <param name="lavorazioniConfigManager">Servizio di configurazione lavorazioni.</param>
        public RDMKT_RSPProcessor(
            ILavorazioniConfigManager lavorazioniConfigManager
        )
        {
            _lavorazioniConfigManager = lavorazioniConfigManager;
            _logger = LogManager.Setup().LoadConfigurationFromFile("nlog.config").GetCurrentClassLogger();
        }

        /// <summary>
        /// Recupera e aggrega i dati di lavorazione per la procedura RDMKT_RSP.
        /// Gestisce tabelle dinamiche con pattern '_RSP_' e '_UDA_DETTAGLIO'.
        /// </summary>
        /// <param name="ct">Token di cancellazione.</param>
        /// <returns>Lista di DatiLavorazione contenente i dati acquisiti dalle fonti dati.</returns>
        public override async Task<List<DatiLavorazione>> SetDatiDematAsync(CancellationToken ct = default)
        {
            QueryLoggingHelper.LogQueryExecution(nlogLogger: _logger, queryType: "SELECT", entityName: "RDMKT_RSP_Dynamic_Tables");

            var result = new List<DatiLavorazione>();
            var startData = StartDataLavorazione.ToString("yyyyMMdd");
            var endData = EndDataLavorazione?.ToString("yyyyMMdd") ?? startData;

            _logger.Info($"[RDMKT_RSP] Elaborazione dati per IDFaseLavorazione: {IDFaseLavorazione}, Periodo: {startData} - {endData}");

            try
            {
                var tableNames = await GetNonEmptyTableNamesAsync(ct);

                _logger.Info($"[RDMKT_RSP] Trovate {tableNames.Count} tabelle RSP con dati");

                if (IDFaseLavorazione == 5)
                {
                    // Fase di indicizzazione
                    foreach (var tabName in tableNames)
                    {
                        string query = $@"
                            select 
                                OP_INDEX as operatore,
                                CONVERT(date, DATA_INDEX) as DataLavorazione,
                                Count(*) as Documenti,
                                SUM(convert(int,isnull(NUM_PAG,0))/2) AS Fogli,
                                SUM(convert(int,isnull(NUM_PAG,0))) AS Pagine
                            from {tabName}
                            where CONVERT(date, DATA_INDEX) >= @startData and CONVERT(date, DATA_INDEX) <= @endData
                            group by OP_INDEX, CONVERT(date, DATA_INDEX), OP_SCAN";

                        result.AddRange(await EseguiQueryAsync(query, startData, endData, tabName, true, ct));
                    }
                }
                else if (IDFaseLavorazione == 4)
                {
                    // Fase di scansione
                    foreach (var tabName in tableNames)
                    {
                        string query = $@"
                            select 
                                OP_SCAN as operatore,
                                CONVERT(date, DATA_SCAN) as DataLavorazione,
                                Count(*) as Documenti,
                                SUM(convert(int,isnull(NUM_PAG,0))/2) AS Fogli,
                                SUM(convert(int,isnull(NUM_PAG,0))) AS Pagine
                            from {tabName}
                            where CONVERT(date, DATA_SCAN) >= @startData and CONVERT(date, DATA_SCAN) <= @endData
                            group by OP_SCAN, CONVERT(date, DATA_SCAN)
                            order by CONVERT(date, DATA_SCAN)";

                        result.AddRange(await EseguiQueryAsync(query, startData, endData, tabName, false, ct));
                    }
                }
                else
                {
                    _logger.Warn($"[RDMKT_RSP] IDFaseLavorazione {IDFaseLavorazione} non gestito");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"[RDMKT_RSP] Errore durante l'elaborazione delle tabelle dinamiche");
                throw;
            }

            _logger.Info($"[RDMKT_RSP] Elaborazione completata. Record totali ottenuti: {result.Count}");

            return result;
        }

        /// <summary>
        /// Trova tutte le tabelle con nome che contiene '_RSP_' e '_UDA_DETTAGLIO' e con almeno una riga.
        /// </summary>
        /// <param name="ct">Token di cancellazione.</param>
        /// <returns>Lista dei nomi delle tabelle che soddisfano i criteri.</returns>
        private async Task<List<string>> GetNonEmptyTableNamesAsync(CancellationToken ct = default)
        {
            QueryLoggingHelper.LogQueryExecution(nlogLogger: _logger, queryType: "METADATA", additionalInfo: "Ricerca tabelle dinamiche RSP");

            var tableNames = new List<string>();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                string queryTabelle = @"SELECT distinct(T.name) TableName 
                                        FROM sys.tables T 
                                        JOIN sys.sysindexes I ON T.OBJECT_ID = I.ID 
                                        where t.name LIKE '%_RSP_%_UDA_DETTAGLIO' and i.Rows > 0";

                await using var connection = new SqlConnection(_lavorazioniConfigManager.CnxnCaptiva206);
                await connection.OpenAsync(ct);

                await using var command = new SqlCommand(queryTabelle, connection);
                command.CommandTimeout = 30;

                _logger.Debug("[RDMKT_RSP] Esecuzione ricerca tabelle dinamiche");

                using var reader = await command.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    tableNames.Add(reader.GetString(0));
                }

                stopwatch.Stop();

                _logger.Info($"[RDMKT_RSP] Ricerca tabelle completata. " +
                            $"Tabelle trovate: {tableNames.Count}, " +
                            $"Tempo esecuzione: {stopwatch.ElapsedMilliseconds}ms");

                if (tableNames.Count > 0)
                {
                    _logger.Debug($"[RDMKT_RSP] Tabelle trovate: {string.Join(", ", tableNames)}");
                }
            }
            catch (SqlException sqlEx)
            {
                stopwatch.Stop();
                _logger.Error(sqlEx, $"[RDMKT_RSP] Errore SQL durante la ricerca delle tabelle. " +
                                     $"Tempo trascorso: {stopwatch.ElapsedMilliseconds}ms, " +
                                     $"Numero errore: {sqlEx.Number}, " +
                                     $"Severità: {sqlEx.Class}, " +
                                     $"Stato: {sqlEx.State}");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.Error(ex, $"[RDMKT_RSP] Errore generico durante la ricerca delle tabelle. " +
                                 $"Tempo trascorso: {stopwatch.ElapsedMilliseconds}ms");
                throw;
            }

            return tableNames;
        }

        /// <summary>
        /// Esegue la query per ottenere i dati di lavorazione da una tabella specifica.
        /// </summary>
        /// <param name="query">Query SQL da eseguire.</param>
        /// <param name="startData">Data di inizio periodo in formato stringa (yyyyMMdd).</param>
        /// <param name="endData">Data di fine periodo in formato stringa (yyyyMMdd).</param>
        /// <param name="tableName">Nome della tabella su cui viene eseguita la query.</param>
        /// <param name="includeOperatoreScan">Indica se includere il campo OperatoreScan nei risultati.</param>
        /// <param name="ct">Token di cancellazione.</param>
        /// <returns>Lista di DatiLavorazione ottenuta dalla query.</returns>
        private async Task<List<DatiLavorazione>> EseguiQueryAsync(string query, string startData, string endData, string tableName, bool includeOperatoreScan, CancellationToken ct = default)
        {
            QueryLoggingHelper.LogQueryExecution(nlogLogger: _logger, queryType: "SELECT", additionalInfo: $"Tabella: {tableName}, Parametri: startData={startData}, endData={endData}");

            var result = new List<DatiLavorazione>();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                await using var connection = new SqlConnection(_lavorazioniConfigManager.CnxnCaptiva206);
                await connection.OpenAsync(ct);

                await using var cmd = new SqlCommand(query, connection);
                cmd.CommandTimeout = 60; // Timeout maggiore per tabelle dinamiche
                cmd.Parameters.AddWithValue("@startData", startData);
                cmd.Parameters.AddWithValue("@endData", endData);

                _logger.Debug($"[RDMKT_RSP] Esecuzione query su tabella {tableName} con timeout: {cmd.CommandTimeout}s");

                using var reader = await cmd.ExecuteReaderAsync(ct);
                int recordCount = 0;
                while (await reader.ReadAsync(ct))
                {
                    var dati = new DatiLavorazione
                    {
                        Operatore = reader["operatore"] as string,
                        DataLavorazione = reader.GetDateTime(reader.GetOrdinal("DataLavorazione")),
                        Documenti = reader["Documenti"] != DBNull.Value ? Convert.ToInt32(reader["Documenti"]) : null,
                        Fogli = reader["Fogli"] != DBNull.Value ? Convert.ToInt32(reader["Fogli"]) : null,
                        Pagine = reader["Pagine"] != DBNull.Value ? Convert.ToInt32(reader["Pagine"]) : null,
                        AppartieneAlCentroSelezionato = true // Le tabelle RSP sono specifiche per centro
                    };

                    // Gestione opzionale dell'operatore di scansione per la fase di indicizzazione
                    if (includeOperatoreScan)
                    {
                        try
                        {
                            var operatoreScanIndex = reader.GetOrdinal("OP_SCAN");
                            if (reader["OP_SCAN"] != DBNull.Value)
                            {
                                dati.OperatoreScan = reader["OP_SCAN"] as string;
                            }
                        }
                        catch (IndexOutOfRangeException)
                        {
                            // Campo OP_SCAN non presente nella query, ignoriamo
                            _logger.Debug($"[RDMKT_RSP] Campo OP_SCAN non presente per tabella {tableName}");
                        }
                    }

                    result.Add(dati);
                    recordCount++;
                }

                stopwatch.Stop();

                _logger.Info($"[RDMKT_RSP] Query su tabella {tableName} completata. " +
                            $"Record letti: {recordCount}, " +
                            $"Tempo esecuzione: {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (SqlException sqlEx)
            {
                stopwatch.Stop();
                _logger.Error(sqlEx, $"[RDMKT_RSP] Errore SQL su tabella {tableName}. " +
                                     $"Tempo trascorso: {stopwatch.ElapsedMilliseconds}ms, " +
                                     $"Numero errore: {sqlEx.Number}, " +
                                     $"Severità: {sqlEx.Class}, " +
                                     $"Stato: {sqlEx.State}");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.Error(ex, $"[RDMKT_RSP] Errore generico su tabella {tableName}. " +
                                 $"Tempo trascorso: {stopwatch.ElapsedMilliseconds}ms");
                throw;
            }

            return result;
        }
    }
}



