using Hangfire;
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
    /// Implementazione del servizio di monitoraggio Hangfire basata su IMonitoringApi.
    /// Usa l'API ufficiale Hangfire invece di query SQL dirette sullo schema interno,
    /// garantendo compatibilità con future versioni di Hangfire.
    /// </summary>
    public class HangfireHealthService : IHangfireHealthService
    {
        private static readonly TimeSpan ServerActiveThreshold = TimeSpan.FromMinutes(5);
        private readonly ILogger<HangfireHealthService> _logger;

        /// <summary>
        /// Inizializza una nuova istanza di <see cref="HangfireHealthService"/>.
        /// </summary>
        /// <param name="logger">Logger per il tracking delle operazioni.</param>
        public HangfireHealthService(ILogger<HangfireHealthService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Verifica lo stato di salute di Hangfire tramite IMonitoringApi.
        /// </summary>
        public Task<bool> CheckHangfireHealthAsync()
        {
            try
            {
                _logger.LogDebug("[HangfireHealth] Controllo stato salute Hangfire");

                var monitoring = JobStorage.Current.GetMonitoringApi();
                var servers = monitoring.Servers();
                var threshold = DateTime.UtcNow.Subtract(ServerActiveThreshold);

                var activeServers = servers.Count(s =>
                    s.Heartbeat.HasValue && s.Heartbeat.Value >= threshold);

                if (activeServers == 0)
                {
                    _logger.LogWarning("[HangfireHealth] Nessun server Hangfire attivo trovato");
                    return Task.FromResult(false);
                }

                _logger.LogInformation("[HangfireHealth] Hangfire operativo. Server attivi: {ActiveServers}", activeServers);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[HangfireHealth] Errore durante il controllo stato Hangfire");
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Ottiene statistiche dettagliate sullo stato di Hangfire tramite IMonitoringApi.
        /// </summary>
        public Task<HangfireStats> GetHangfireStatsAsync()
        {
            var result = new HangfireStats();

            try
            {
                var monitoring = JobStorage.Current.GetMonitoringApi();
                var statistics = monitoring.GetStatistics();
                var servers = monitoring.Servers();
                var threshold = DateTime.UtcNow.Subtract(ServerActiveThreshold);

                result.EnqueuedJobs = (int)statistics.Enqueued;
                result.ProcessingJobs = (int)statistics.Processing;
                result.FailedJobs = (int)statistics.Failed;
                result.RecurringJobs = (int)statistics.Recurring;
                result.ActiveServers = servers.Count(s =>
                    s.Heartbeat.HasValue && s.Heartbeat.Value >= threshold);

                _logger.LogInformation(
                    "[HangfireHealth] Statistiche: Enqueued={Enqueued}, Processing={Processing}, Failed={Failed}, Servers={Servers}, Recurring={Recurring}",
                    result.EnqueuedJobs, result.ProcessingJobs, result.FailedJobs, result.ActiveServers, result.RecurringJobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[HangfireHealth] Errore durante il recupero delle statistiche Hangfire");
            }

            return Task.FromResult(result);
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
