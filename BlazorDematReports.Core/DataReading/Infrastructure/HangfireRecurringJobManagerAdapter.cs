using Cronos;
using Hangfire;
using Hangfire.Storage;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace BlazorDematReports.Core.DataReading.Infrastructure
{
    /// <summary>
    /// Adapter per la gestione dei recurring job Hangfire (creazione, aggiornamento, rimozione, enumerazione).
    /// Incapsula IRecurringJobManager e fornisce una enumerazione resiliente delle chiavi compatibile con versioni / storage differenti.
    /// Strategia enumerazione GetRecurringJobKeys:
    ///   1. Usa connection.GetRecurringJobs() (API diretta) se disponibile.
    ///   2. Fallback reflection su MonitoringApi.RecurringJobs(offset, limit) oppure senza parametri (versioni diverse).
    ///   3. Fallback finale: lettura diretta del set redis/sql 'recurring-jobs' via reflection (per storage custom che espongono GetAllItemsFromSet).
    /// Tutti i fallback sono best-effort: eventuali eccezioni vengono loggate a livello Warning senza interrompere il flusso.
    /// </summary>
    public sealed class HangfireRecurringJobManagerAdapter : IRecurringJobManagerAdapter
    {
        private static readonly TimeZoneInfo UtcTimeZone = TimeZoneInfo.Utc; // Forza pianificazione in UTC (coerenza ambienti distribuiti)

        private readonly IRecurringJobManager _manager; // Gestore Hangfire iniettato
        private readonly ILogger<HangfireRecurringJobManagerAdapter> _logger; // Logger tipizzato

        public HangfireRecurringJobManagerAdapter(
            IRecurringJobManager manager,
            ILogger<HangfireRecurringJobManagerAdapter> logger)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Crea o aggiorna un recurring job delegando a Hangfire. Esegue validazione cron preventiva.
        /// </summary>
        /// <param name="jobKey">Chiave univoca (Id) del recurring job.</param>
        /// <param name="taskId">Identificativo del task applicativo associato.</param>
        /// <param name="cronExpression">Espressione cron (formato standard a 5 campi).</param>
        public void AddOrUpdate(string jobKey, int taskId, string cronExpression)
        {
            ValidateCron(jobKey, cronExpression);
            try
            {
                _logger.LogInformation("[RecurringAdapter] AddOrUpdate JobKey={JobKey} Cron={Cron}", jobKey, cronExpression);
                var options = new RecurringJobOptions { TimeZone = UtcTimeZone };
                _manager.AddOrUpdate(jobKey, () => ProductionJobRunner.RunAsync(taskId), cronExpression, options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore registrazione recurring job {JobKey}", jobKey);
                throw;
            }
        }

        /// <summary>
        /// Rimuove un recurring job se esiste (safe: ignora chiavi nulle/vuote).
        /// </summary>
        public void RemoveIfExists(string jobKey)
        {
            if (string.IsNullOrWhiteSpace(jobKey))
                return;
            try
            {
                _logger.LogInformation("[RecurringAdapter] RemoveIfExists JobKey={JobKey}", jobKey);
                _manager.RemoveIfExists(jobKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore rimozione recurring job {JobKey}", jobKey);
                throw;
            }
        }

        /// <summary>
        /// Restituisce l'elenco delle chiavi dei recurring job registrati.
        /// L'ordine non è garantito e potrebbero esserci duplicati intermedi (rimossi prima del return).
        /// </summary>
        public IEnumerable<string> GetRecurringJobKeys()
        {
            var keys = new List<string>();
            try
            {
                // 1) Tentativo principale: API diretta di connection (metodo consigliato se supportato dallo storage)
                using (var connection = JobStorage.Current.GetConnection())
                {
                    try
                    {
                        var recurring = connection.GetRecurringJobs();
                        if (recurring != null && recurring.Count > 0)
                        {
                            foreach (var r in recurring)
                                if (!string.IsNullOrWhiteSpace(r.Id))
                                    keys.Add(r.Id);
                            if (keys.Count > 0)
                            {
                                _logger.LogDebug("[RecurringAdapter] Enumerati {Count} recurring job via connection API", keys.Count);
                                return keys;
                            }
                        }
                    }
                    catch (NotImplementedException)
                    {
                        // Storage custom potrebbe non implementare GetRecurringJobs -> fallback
                        _logger.LogDebug("[RecurringAdapter] GetRecurringJobs non implementato, passo a reflection");
                    }
                    catch (Exception exConn)
                    {
                        // Qualunque eccezione non-blocking: si tenta reflection
                        _logger.LogWarning(exConn, "[RecurringAdapter] Errore GetRecurringJobs(), fallback reflection");
                    }
                }

                // 2) Monitor API con reflection (supporta differenze di firma tra versioni Hangfire)
                var monitor = JobStorage.Current.GetMonitoringApi();
                if (!TryCollectKeys(monitor, keys, withPaging: true))
                {
                    TryCollectKeys(monitor, keys, withPaging: false);
                }

                if (keys.Count > 0)
                {
                    _logger.LogDebug("[RecurringAdapter] Enumerazione reflection completata: {Count} job", keys.Count);
                    return keys;
                }

                // 3) Fallback finale: lettura set 'recurring-jobs' (usato ad es. da alcuni storage che persistono le chiavi come set)
                try
                {
                    using var rawConn = JobStorage.Current.GetConnection();
                    var setItems = GetAllItemsFromSetViaReflection(rawConn, "recurring-jobs");
                    if (setItems != null)
                    {
                        foreach (var id in setItems)
                            if (!string.IsNullOrWhiteSpace(id) && !keys.Contains(id))
                                keys.Add(id);
                        if (keys.Count > 0)
                            _logger.LogDebug("[RecurringAdapter] Recuperate {Count} chiavi via Set 'recurring-jobs'", keys.Count);
                    }
                }
                catch (Exception exSet)
                {
                    _logger.LogWarning(exSet, "[RecurringAdapter] Fallback set recurring-jobs fallito");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Impossibile enumerare i recurring job (lista parziale o vuota)");
            }
            return keys;
        }

        #region Helper
        /// <summary>Valida la cron (non vuota e parsabile da Cronos con almeno una occorrenza futura).</summary>
        private static void ValidateCron(string jobKey, string cronExpression)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(jobKey);
            ArgumentException.ThrowIfNullOrWhiteSpace(cronExpression);
            if (!IsCronValid(cronExpression))
                throw new ArgumentException($"Cron non valida: {cronExpression}", nameof(cronExpression));
        }

        /// <summary>
        /// Ritorna true se l'espressione cron è sintatticamente corretta e produce una prossima occorrenza.
        /// Usa DateTime.UtcNow (Kind=Utc) perché Cronos richiede un riferimento UTC per GetNextOccurrence;
        /// DateTime.Now (Kind=Local) causa un'eccezione interna che verrebbe inghiottita dal catch restituendo false.
        /// </summary>
        private static bool IsCronValid(string cron)
        {
            try
            {
                var expr = CronExpression.Parse(cron, CronFormat.Standard);
                return expr.GetNextOccurrence(DateTime.UtcNow) != null;
            }
            catch { return false; }
        }

        /// <summary>
        /// Prova a recuperare le chiavi usando la Monitoring API (reflection per gestire firme diverse).
        /// item può essere:
        ///   - RecurringJobDto (prop Id)
        ///   - KeyValuePair<string, RecurringJobDto> (prop Key)
        /// </summary>
        private static bool TryCollectKeys(object monitor, List<string> target, bool withPaging)
        {
            MethodInfo? method = withPaging
                ? monitor.GetType().GetMethod("RecurringJobs", BindingFlags.Public | BindingFlags.Instance, new[] { typeof(int), typeof(int) })
                : monitor.GetType().GetMethod("RecurringJobs", BindingFlags.Public | BindingFlags.Instance, Type.EmptyTypes);
            if (method == null)
                return false;

            var result = withPaging
                ? method.Invoke(monitor, new object[] { 0, int.MaxValue })
                : method.Invoke(monitor, null);

            if (result is System.Collections.IEnumerable list)
            {
                foreach (var item in list)
                {
                    string? id = null;
                    var idProp = item.GetType().GetProperty("Id");
                    if (idProp?.GetValue(item) is string directId)
                    {
                        id = directId;
                    }
                    else
                    {
                        var keyProp = item.GetType().GetProperty("Key");
                        if (keyProp?.GetValue(item) is string kvKey)
                            id = kvKey;
                    }
                    if (!string.IsNullOrWhiteSpace(id))
                        target.Add(id);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Richiama (se disponibile) GetAllItemsFromSet sullo storage connection per leggere un set (es. 'recurring-jobs').
        /// Usato come fallback finale.
        /// </summary>
        private static IEnumerable<string>? GetAllItemsFromSetViaReflection(IStorageConnection conn, string setName)
        {
            try
            {
                var m = conn.GetType().GetMethod("GetAllItemsFromSet", BindingFlags.Public | BindingFlags.Instance);
                if (m == null)
                    return null;
                var result = m.Invoke(conn, new object[] { setName });
                return result as IEnumerable<string>;
            }
            catch { return null; }
        }
        #endregion
    }
}
