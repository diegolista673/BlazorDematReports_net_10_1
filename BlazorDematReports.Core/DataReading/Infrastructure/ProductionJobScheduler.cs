using BlazorDematReports.Core.DataReading.Interfaces;
using Entities.Enums;
using Entities.Models.DbApplication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace BlazorDematReports.Core.DataReading.Infrastructure
{
    /// <summary>
    /// Scheduler per task di produzione in tabella TaskDaEseguire.
    /// Responsabile di creare, aggiornare, sincronizzare e rimuovere i recurring job Hangfire
    /// associati ai task configurati nel sistema.
    /// Costruisce la chiave Hangfire in base al tipo di task.
    /// Formato: {tipoAbbreviato}:{IdTaskDaEseguire}-{idproc}-{nomeprocedura}:{dettaglio}
    ///   - sql:{id}-{idproc}-{proc}:{fase}         per TipoFonte.SQL
    ///   - hdl:{id}-{idproc}-{proc}:{handlercode}  per TipoFonte.HandlerIntegrato
    /// </summary>
    public sealed class ProductionJobScheduler : IProductionJobScheduler
    {
        private readonly IDbContextFactory<DematReportsContext> _contextFactory;
        private readonly IRecurringJobManagerAdapter _adapter;
        private readonly ILogger<ProductionJobScheduler>? _logger;

        public ProductionJobScheduler(
            IDbContextFactory<DematReportsContext> contextFactory,
            IRecurringJobManagerAdapter adapter,
            ILogger<ProductionJobScheduler>? logger = null)
        {
            _contextFactory = contextFactory;
            _adapter        = adapter;
            _logger         = logger;
        }

        #region Key & Cron Helpers

        /// <summary>
        /// Normalizza un token testuale per uso nella chiave Hangfire:
        /// - trim, lower
        /// - spazi -> '-'
        /// - rimozione caratteri non alfanumerici (eccetto - _)
        /// - fallback "na" se vuoto
        /// </summary>
        private static string NormalizeToken(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "na";
            var v = value.Trim().ToLowerInvariant().Replace(' ', '-');
            v = Regex.Replace(v, "[^a-z0-9-_]", "");
            return v;
        }

        /// <summary>
        /// Costruisce la chiave Hangfire in base al tipo di task.
        /// Formato: {tipoAbbreviato}:{IdTaskDaEseguire}-{idproc}-{nomeprocedura}:{dettaglio}
        ///   - sql:{id}-{idproc}-{proc}:{fase}         per TipoFonte.SQL
        ///   - hdl:{id}-{idproc}-{proc}:{handlercode}  per TipoFonte.HandlerIntegrato
        /// </summary>
        private static string BuildHangfireKey(TaskDaEseguire t)
        {
            var idProc   = t.IdLavorazioneFaseDateReadingNavigation?.IdProceduraLavorazione;
            var procName = NormalizeToken(t.IdLavorazioneFaseDateReadingNavigation?.IdProceduraLavorazioneNavigation?.NomeProcedura);
            var faseName = NormalizeToken(t.IdLavorazioneFaseDateReadingNavigation?.IdFaseLavorazioneNavigation?.FaseLavorazione);

            if (t.IdConfigurazioneDatabase.HasValue && t.IdConfigurazioneDatabaseNavigation != null)
            {
                var config = t.IdConfigurazioneDatabaseNavigation;
                var tipoFonte = config.TipoFonte;

                var (prefix, detail) = tipoFonte switch
                {
                    TipoFonteData.SQL              => ("sql", faseName),
                    TipoFonteData.HandlerIntegrato => ("hdl", NormalizeToken(config.HandlerClassName ?? "handler")),
                    _                              => ("job", faseName)   // fallback future-proof
                };

                return $"{prefix}:{t.IdTaskDaEseguire}-{idProc}-{procName}:{detail}";
            }

            // FALLBACK (non dovrebbe mai succedere con nuovo sistema)
            return $"job:{t.IdTaskDaEseguire}-{idProc}-{procName}-{faseName}";
        }

        /// <summary>
        /// Risolve l'espressione cron per un task.
        /// Priorità: CronExpression del task > default "0 5 * * *"
        /// </summary>
        private static string ResolveCron(TaskDaEseguire t)
        {
            if (!string.IsNullOrWhiteSpace(t.CronExpression))
                return t.CronExpression;

            return "0 5 * * *";
        }

        #endregion

        /// <summary>
        /// Aggiunge o aggiorna un recurring job Hangfire per il task specificato.
        /// Ricalcola la chiave, aggiorna il record in DB se necessario e registra il job.
        /// </summary>
        /// <param name="idTaskDaEseguire">ID del task da sincronizzare.</param>
        /// <returns>Chiave Hangfire registrata.</returns>
        public async Task<string> AddOrUpdateAsync(int idTaskDaEseguire)
        {
            using var activity = new Activity("ProductionJobScheduler.AddOrUpdate")
                .AddTag("TaskId", idTaskDaEseguire.ToString())
                .Start();

            _logger?.LogDebug("Inizio sincronizzazione task {TaskId}", idTaskDaEseguire);

            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                var task = await GetTaskWithRelationsAsync(context, idTaskDaEseguire);

                var hangfireKey    = BuildHangfireKey(task);
                var cronExpression = ResolveCron(task);

                await UpdateTaskInDatabaseAsync(context, task, hangfireKey, cronExpression);

                _adapter.AddOrUpdate(hangfireKey, task.IdTaskDaEseguire, cronExpression);

                _logger?.LogInformation("Task {TaskId} sincronizzato con chiave: {HangfireKey}",
                    idTaskDaEseguire, hangfireKey);

                activity?.AddTag("HangfireKey", hangfireKey);
                activity?.AddTag("Success", "true");

                return hangfireKey;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Errore durante sincronizzazione task {TaskId}", idTaskDaEseguire);
                activity?.AddTag("Success", "false");
                activity?.AddTag("Error", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Recupera un task con tutte le relazioni necessarie per la generazione della chiave Hangfire.
        /// </summary>
        /// <exception cref="InvalidOperationException">Se il task non viene trovato.</exception>
        private async Task<TaskDaEseguire> GetTaskWithRelationsAsync(
            DematReportsContext context,
            int idTaskDaEseguire)
        {
            var task = await context.TaskDaEseguires
                .Include(x => x.IdLavorazioneFaseDateReadingNavigation)!
                    .ThenInclude(f => f.IdProceduraLavorazioneNavigation)
                .Include(x => x.IdLavorazioneFaseDateReadingNavigation)!
                    .ThenInclude(f => f.IdFaseLavorazioneNavigation)
                .Include(x => x.IdConfigurazioneDatabaseNavigation)
                .FirstOrDefaultAsync(x => x.IdTaskDaEseguire == idTaskDaEseguire);

            if (task is null)
            {
                var msg = $"Task {idTaskDaEseguire} non trovato nel database";
                _logger?.LogError(msg);
                throw new InvalidOperationException(msg);
            }

            _logger?.LogDebug(
                "Task {TaskId} recuperato: Procedura={Proc}, Fase={Fase}",
                idTaskDaEseguire,
                task.IdLavorazioneFaseDateReadingNavigation?.IdProceduraLavorazioneNavigation?.NomeProcedura ?? "N/A",
                task.IdLavorazioneFaseDateReadingNavigation?.IdFaseLavorazioneNavigation?.FaseLavorazione   ?? "N/A");

            return task;
        }

        /// <summary>
        /// Aggiorna il task nel database con i nuovi valori se necessario (chiave, cron, enabled).
        /// Salva solo se ci sono modifiche effettive.
        /// </summary>
        private async Task UpdateTaskInDatabaseAsync(
            DematReportsContext context,
            TaskDaEseguire task,
            string hangfireKey,
            string cronExpression)
        {
            var changes = new List<string>();

            // Forza aggiornamento se la chiave è cambiata o è ancora temporanea
            if (task.IdTaskHangFire != hangfireKey ||
                (!string.IsNullOrWhiteSpace(task.IdTaskHangFire) && task.IdTaskHangFire.StartsWith("temp-")))
            {
                changes.Add($"HangfireKey: {task.IdTaskHangFire ?? "NULL"} -> {hangfireKey}");
                task.IdTaskHangFire = hangfireKey;
            }

            if (string.IsNullOrWhiteSpace(task.CronExpression) && !string.IsNullOrWhiteSpace(cronExpression))
            {
                changes.Add($"Cron: NULL -> {cronExpression}");
                task.CronExpression = cronExpression;
            }

            if (!task.Enabled)
            {
                changes.Add("Enabled: false -> true");
                task.Enabled = true;
            }

            if (changes.Count > 0)
            {
                await context.SaveChangesAsync();
                _logger?.LogDebug("Task {TaskId} aggiornato: {Changes}",
                    task.IdTaskDaEseguire, string.Join(", ", changes));
            }
            else
            {
                _logger?.LogDebug("Task {TaskId} gia aggiornato, nessuna modifica", task.IdTaskDaEseguire);
            }
        }

        /// <summary>Disabilita un task e rimuove il relativo recurring job Hangfire (se presente).</summary>
        public async Task DisableAsync(int idTaskDaEseguire)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                var task = await context.TaskDaEseguires
                    .FirstOrDefaultAsync(x => x.IdTaskDaEseguire == idTaskDaEseguire);

                if (task is null)
                    return;

                task.Enabled = false;
                await context.SaveChangesAsync();

                if (!string.IsNullOrEmpty(task.IdTaskHangFire))
                    _adapter.RemoveIfExists(task.IdTaskHangFire);
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
            {
                _logger?.LogError(ex, "DisableAsync: impossibile connettersi a SQL Server per il task {TaskId}", idTaskDaEseguire);
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "DisableAsync: errore inatteso per il task {TaskId}", idTaskDaEseguire);
                throw;
            }
        }

        /// <summary>Rimuove un recurring job Hangfire direttamente dalla chiave.</summary>
        public async Task RemoveByKeyAsync(string hangfireKey)
        {
            if (string.IsNullOrWhiteSpace(hangfireKey))
                return;
            _adapter.RemoveIfExists(hangfireKey);
            await Task.CompletedTask;
        }

        /// <summary>
        /// Sincronizza tutti i task abilitati: ricalcola chiavi e cron, rimuove le chiavi
        /// Hangfire obsolete e registra quelle nuove. Utile dopo refactor del naming.
        /// </summary>
        public async Task SyncAllAsync()
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                var enabled = await context.TaskDaEseguires
                    .Include(t => t.IdLavorazioneFaseDateReadingNavigation)!.ThenInclude(f => f.IdProceduraLavorazioneNavigation)
                    .Include(t => t.IdLavorazioneFaseDateReadingNavigation)!.ThenInclude(f => f.IdFaseLavorazioneNavigation)
                    .Include(t => t.IdConfigurazioneDatabaseNavigation)
                    .Where(t => t.Enabled)
                    .ToListAsync();

                foreach (var t in enabled)
                {
                    var expectedKey = BuildHangfireKey(t);
                    var cron        = ResolveCron(t);
                    var oldKey      = t.IdTaskHangFire;

                    bool keyChanged = oldKey != expectedKey
                        || (!string.IsNullOrWhiteSpace(oldKey) && oldKey.StartsWith("temp-"));

                    if (keyChanged)
                    {
                        if (!string.IsNullOrWhiteSpace(oldKey))
                            _adapter.RemoveIfExists(oldKey);

                        t.IdTaskHangFire = expectedKey;
                    }

                    if (string.IsNullOrWhiteSpace(t.CronExpression))
                        t.CronExpression = cron;

                    _adapter.AddOrUpdate(t.IdTaskHangFire, t.IdTaskDaEseguire, cron);
                }

                await context.SaveChangesAsync();
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
            {
                _logger?.LogError(ex, "SyncAllAsync: impossibile connettersi a SQL Server. Verificare che il server sia avviato e raggiungibile");
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "SyncAllAsync: errore inatteso durante la sincronizzazione dei job");
                throw;
            }
        }

        /// <summary>
        /// Rimuove i recurring job orfani (presenti in Hangfire ma non più associati a task abilitati)
        /// e aggiunge quelli mancanti. Sincronizza chiavi e cron.
        /// </summary>
        /// <returns>Numero totale di job rimossi o aggiunti.</returns>
        public async Task<int> CleanupOrphansAsync()
        {
            try
            {
                await using var context  = await _contextFactory.CreateDbContextAsync();
                var existingKeys = _adapter.GetRecurringJobKeys().ToHashSet();

                var enabledTasks = await context.TaskDaEseguires
                    .AsSplitQuery()
                    .Include(t => t.IdLavorazioneFaseDateReadingNavigation)!.ThenInclude(f => f.IdProceduraLavorazioneNavigation)
                    .Include(t => t.IdLavorazioneFaseDateReadingNavigation)!.ThenInclude(f => f.IdFaseLavorazioneNavigation)
                    .Include(t => t.IdConfigurazioneDatabaseNavigation)
                    .Where(t => t.Enabled)
                    .ToListAsync();

                var validKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var t in enabledTasks)
                {
                    validKeys.Add(BuildHangfireKey(t));

                    if (!string.IsNullOrWhiteSpace(t.IdTaskHangFire) && !t.IdTaskHangFire.StartsWith("temp-"))
                        validKeys.Add(t.IdTaskHangFire!);
                }

                int removed = 0;
                foreach (var key in existingKeys)
                {
                    if (key.StartsWith("system:", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!validKeys.Contains(key))
                    {
                        _adapter.RemoveIfExists(key);
                        removed++;
                    }
                }

                int added = 0;
                foreach (var task in enabledTasks)
                {
                    var expected = BuildHangfireKey(task);
                    var cron     = ResolveCron(task);

                    if (task.IdTaskHangFire != expected ||
                        (!string.IsNullOrWhiteSpace(task.IdTaskHangFire) && task.IdTaskHangFire.StartsWith("temp-")))
                    {
                        task.IdTaskHangFire = expected;
                    }

                    if (string.IsNullOrWhiteSpace(task.CronExpression))
                        task.CronExpression = cron;

                    if (!existingKeys.Contains(expected))
                    {
                        _adapter.AddOrUpdate(expected, task.IdTaskDaEseguire, task.CronExpression!);
                        added++;
                    }
                }

                if (removed > 0 || added > 0)
                    await context.SaveChangesAsync();

                return removed + added;
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
            {
                _logger?.LogError(ex, "CleanupOrphansAsync: impossibile connettersi a SQL Server. Verificare che il server sia avviato e raggiungibile");
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "CleanupOrphansAsync: errore inatteso durante il cleanup degli orphan job");
                throw;
            }
        }
    }
}
