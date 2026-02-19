using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BlazorDematReports.Core.DataReading.Services
{
    /// <summary>
    /// Servizio per il monitoraggio e diagnostica dello stato di Hangfire.
    /// </summary>
    public interface IHangfireHealthService
    {
        /// <summary>
        /// Verifica lo stato di salute di Hangfire.
        /// </summary>
        /// <returns>True se Hangfire è operativo, false altrimenti.</returns>
        Task<bool> CheckHangfireHealthAsync();

        /// <summary>
        /// Ottiene statistiche dettagliate sullo stato di Hangfire.
        /// </summary>
        /// <returns>Oggetto contenente le statistiche di Hangfire.</returns>
        Task<HangfireStats> GetHangfireStatsAsync();
    }

    /// <summary>
    /// Implementazione del servizio di monitoraggio Hangfire.
    /// </summary>
    public class HangfireHealthService : IHangfireHealthService
    {
        private readonly ILogger<HangfireHealthService> _logger;
        private readonly string _connectionString;

        public HangfireHealthService(ILogger<HangfireHealthService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("HangfireConnection")
                ?? throw new InvalidOperationException("HangfireConnection not found in configuration");
        }

        /// <summary>
        /// Verifica lo stato di salute di Hangfire.
        /// </summary>
        public async Task<bool> CheckHangfireHealthAsync()
        {
            try
            {
                _logger.LogDebug("[HangfireHealth] Controllo stato salute Hangfire");

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Verifica che le tabelle Hangfire esistano
                using var command = new SqlCommand(@"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_SCHEMA = 'HangFire' 
                    AND TABLE_NAME IN ('Job', 'JobQueue', 'Server', 'State')", connection);

                var tableCount = (int)await command.ExecuteScalarAsync();

                if (tableCount < 4)
                {
                    _logger.LogWarning("[HangfireHealth] Tabelle Hangfire mancanti. Trovate: {TableCount}/4", tableCount);
                    return false;
                }

                // Verifica che ci siano server attivi
                using var serverCommand = new SqlCommand(@"
                    SELECT COUNT(*) 
                    FROM [HangFire].[Server] 
                    WHERE [LastHeartbeat] >= DATEADD(minute, -5, GETUTCDATE())", connection);

                var activeServers = (int)await serverCommand.ExecuteScalarAsync();

                if (activeServers == 0)
                {
                    _logger.LogWarning("[HangfireHealth] Nessun server Hangfire attivo trovato");
                    return false;
                }

                _logger.LogInformation("[HangfireHealth] Hangfire operativo. Server attivi: {ActiveServers}", activeServers);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[HangfireHealth] Errore durante il controllo stato Hangfire");
                return false;
            }
        }

        /// <summary>
        /// Ottiene statistiche dettagliate sullo stato di Hangfire.
        /// </summary>
        public async Task<HangfireStats> GetHangfireStatsAsync()
        {
            var stats = new HangfireStats();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Job in coda
                using var queueCommand = new SqlCommand(@"
                    SELECT COUNT(*) FROM [HangFire].[JobQueue] WHERE [FetchedAt] IS NULL", connection);
                stats.EnqueuedJobs = (int)await queueCommand.ExecuteScalarAsync();

                // Job processing
                using var processingCommand = new SqlCommand(@"
                    SELECT COUNT(*) FROM [HangFire].[JobQueue] WHERE [FetchedAt] IS NOT NULL", connection);
                stats.ProcessingJobs = (int)await processingCommand.ExecuteScalarAsync();

                // Job failed
                using var failedCommand = new SqlCommand(@"
                    SELECT COUNT(*) FROM [HangFire].[Job] j
                    INNER JOIN [HangFire].[State] s ON j.[Id] = s.[JobId]
                    WHERE s.[Name] = 'Failed' AND s.[Id] = (
                        SELECT TOP 1 [Id] FROM [HangFire].[State] 
                        WHERE [JobId] = j.[Id] ORDER BY [CreatedAt] DESC
                    )", connection);
                stats.FailedJobs = (int)await failedCommand.ExecuteScalarAsync();

                // Server attivi
                using var serverCommand = new SqlCommand(@"
                    SELECT COUNT(*) FROM [HangFire].[Server] 
                    WHERE [LastHeartbeat] >= DATEADD(minute, -5, GETUTCDATE())", connection);
                stats.ActiveServers = (int)await serverCommand.ExecuteScalarAsync();

                // Recurring jobs
                using var recurringCommand = new SqlCommand(@"
                    SELECT COUNT(*) FROM [HangFire].[Hash] 
                    WHERE [Key] LIKE 'recurring-job:%' AND [Field] = 'Job'", connection);
                stats.RecurringJobs = (int)await recurringCommand.ExecuteScalarAsync();

                _logger.LogInformation("[HangfireHealth] Statistiche: Enqueued={EnqueuedJobs}, Processing={ProcessingJobs}, Failed={FailedJobs}, Servers={ActiveServers}, Recurring={RecurringJobs}",
                    stats.EnqueuedJobs, stats.ProcessingJobs, stats.FailedJobs, stats.ActiveServers, stats.RecurringJobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[HangfireHealth] Errore durante il recupero delle statistiche Hangfire");
            }

            return stats;
        }
    }

    /// <summary>
    /// Statistiche dello stato di Hangfire.
    /// </summary>
    public class HangfireStats
    {
        public int EnqueuedJobs { get; set; }
        public int ProcessingJobs { get; set; }
        public int FailedJobs { get; set; }
        public int ActiveServers { get; set; }
        public int RecurringJobs { get; set; }
        public DateTime CheckTime { get; set; } = DateTime.UtcNow;
    }
}