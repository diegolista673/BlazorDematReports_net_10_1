using DataReading.Interfaces;
using DataReading.Models;
using Entities.Helpers;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;


namespace DataReading.Services
{
    /// <summary>
    /// Servizio per la gestione dei job Hangfire tramite accesso diretto al database.
    /// </summary>
    public class HangfireJobService : IHangfireJobService
    {
        private readonly ILogger<HangfireJobService> _logger;
        private readonly int _commandTimeoutSeconds = 30;
        private readonly int _connectionTimeoutSeconds = 15;

        /// <summary>
        /// Costruttore che inizializza il logger per HangfireJobService.
        /// </summary>
        /// <param name="logger">Logger per il tracking delle operazioni.</param>
        public HangfireJobService(ILogger<HangfireJobService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Ottiene il job Hangfire in formato JSON dalla tabella del database.
        /// </summary>
        /// <param name="keyJob">Chiave del job Hangfire.</param>
        /// <param name="connectionString">Stringa di connessione al database.</param>
        /// <returns>JSON del job Hangfire.</returns>
        /// <exception cref="NullReferenceException">Job Hangfire non trovato</exception>
        /// <exception cref="SqlException">Errore di connessione al database</exception>
        /// <exception cref="TimeoutException">Timeout durante l'esecuzione della query</exception>
        public async Task<string> GetJobJsonAsync(string keyJob, string connectionString)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(keyJob, nameof(keyJob));
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionString, nameof(connectionString));

            QueryLoggingHelper.LogQueryExecution(logger: _logger, additionalInfo: $"Retrieving job: {keyJob}");

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var connectionStringBuilder = new SqlConnectionStringBuilder(connectionString)
                {
                    ConnectTimeout = _connectionTimeoutSeconds,
                    CommandTimeout = _commandTimeoutSeconds
                };

                using var connection = new SqlConnection(connectionStringBuilder.ConnectionString);

                _logger.LogDebug("[HangfireJobService] Apertura connessione database per job: {JobKey}", keyJob);
                await connection.OpenAsync();

                using var command = new SqlCommand(
                    @"SELECT [Value] FROM [HangFire].[Hash] 
                      WHERE [Key] = @jobKey AND [Field] = @field", connection);

                command.CommandTimeout = _commandTimeoutSeconds;
                command.Parameters.Add("@jobKey", SqlDbType.NVarChar, 100).Value = "recurring-job:" + keyJob;
                command.Parameters.Add("@field", SqlDbType.NVarChar, 100).Value = "Job";

                _logger.LogDebug("[HangfireJobService] Esecuzione query per job: {JobKey}", keyJob);
                var result = (string?)await command.ExecuteScalarAsync();

                stopwatch.Stop();
                _logger.LogInformation("[HangfireJobService] Job {JobKey} recuperato con successo in {ElapsedMs}ms",
                    keyJob, stopwatch.ElapsedMilliseconds);

                return result ?? throw new NullReferenceException($"Hangfire job with key '{keyJob}' not found in database.");
            }
            catch (SqlException sqlEx)
            {
                stopwatch.Stop();
                _logger.LogError(sqlEx, "[HangfireJobService] Errore SQL durante il recupero del job {JobKey}. " +
                                      "Tempo trascorso: {ElapsedMs}ms, " +
                                      "Numero errore: {ErrorNumber}, " +
                                      "Severità: {Class}, " +
                                      "Stato: {State}",
                    keyJob, stopwatch.ElapsedMilliseconds, sqlEx.Number, sqlEx.Class, sqlEx.State);
                throw;
            }
            catch (InvalidOperationException ioEx) when (ioEx.Message.Contains("timeout"))
            {
                stopwatch.Stop();
                _logger.LogError(ioEx, "[HangfireJobService] Timeout durante il recupero del job {JobKey}. " +
                                      "Tempo trascorso: {ElapsedMs}ms", keyJob, stopwatch.ElapsedMilliseconds);
                throw new TimeoutException($"Timeout durante il recupero del job Hangfire '{keyJob}'", ioEx);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "[HangfireJobService] Errore imprevisto durante il recupero del job {JobKey}. " +
                                    "Tempo trascorso: {ElapsedMs}ms", keyJob, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        /// <summary>
        /// Deserializza il JSON di un job Hangfire in un oggetto JsonJobHangfire.
        /// </summary>
        /// <param name="jsonString">Stringa JSON del job Hangfire.</param>
        /// <returns>Oggetto <see cref="JsonJobHangfire"/> deserializzato.</returns>
        /// <exception cref="ArgumentException">JSON string null o vuoto</exception>
        /// <exception cref="System.Text.Json.JsonException">Errore durante la deserializzazione JSON</exception>
        public JsonJobHangfire DeserializeJob(string jsonString)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(jsonString, nameof(jsonString));

            QueryLoggingHelper.LogQueryExecution(logger: _logger, additionalInfo: "Deserializing Hangfire job JSON");

            try
            {
                _logger.LogDebug("[HangfireJobService] Deserializzazione JSON job, lunghezza: {JsonLength}", jsonString.Length);

                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true
                };

                var result = System.Text.Json.JsonSerializer.Deserialize<JsonJobHangfire>(jsonString, options);

                if (result == null)
                    throw new System.Text.Json.JsonException("La deserializzazione ha prodotto un oggetto null");

                _logger.LogDebug("[HangfireJobService] Deserializzazione JSON completata con successo");
                return result;
            }
            catch (System.Text.Json.JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "[HangfireJobService] Errore durante la deserializzazione JSON. " +
                                        "JSON Length: {JsonLength}, JSON Preview: {JsonPreview}",
                    jsonString.Length, jsonString.Length > 100 ? jsonString.Substring(0, 100) + "..." : jsonString);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[HangfireJobService] Errore imprevisto durante la deserializzazione JSON");
                throw;
            }
        }
    }
}
