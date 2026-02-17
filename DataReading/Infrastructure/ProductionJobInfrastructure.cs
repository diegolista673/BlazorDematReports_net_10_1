using DataReading.Dto;
using DataReading.Interfaces;
using Entities.Enums;
using Entities.Models.DbApplication;
using LibraryLavorazioni.Shared.Interfaces;
using LibraryLavorazioni.Shared.Models;
using LibraryLavorazioni.Utility.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace DataReading.Infrastructure
{
    /// <summary>
    /// Scheduler unificato per task di produzione e task di import mail basati sulla tabella TaskDaEseguire.
    /// Genera chiavi Hangfire con prefissi distinti:
    ///  - Produzione: prod:{IdTaskDaEseguire}-{IdProceduraLavorazione}:{nomeprocedura-normal}-{fase-normal}
    ///  - Mail:       mail:{IdTaskDaEseguire}-{IdProceduraLavorazione}:{nomeprocedura-normal}-{mail-service-normalizzato}
    /// </summary>
    public sealed class ProductionJobScheduler : IProductionJobScheduler
    {
        private readonly DematReportsContext _db;
        private readonly IRecurringJobManagerAdapter _adapter;
        private readonly ILogger<ProductionJobScheduler>? _logger;

        public ProductionJobScheduler(DematReportsContext db, IRecurringJobManagerAdapter adapter, ILogger<ProductionJobScheduler>? logger = null)
        { 
            _db = db; 
            _adapter = adapter; 
            _logger = logger;
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
        /// Normalizza codice servizio mail sostituendo anche i punti con '-'
        /// </summary>
        private static string NormalizeService(string svc) => NormalizeToken(svc.Replace('.', '-'));

        /// <summary>
        /// Costruisce la chiave Hangfire in base al tipo di task.
        /// Formato unificato: prod:{IdTaskDaEseguire}-{IdProceduraLavorazione}:{nomeprocedura-normal}-{detail}
        /// </summary>
        private static string BuildHangfireKey(TaskDaEseguire t)
        {
            var idProc = t.IdLavorazioneFaseDateReadingNavigation?.IdProceduraLavorazione;
            var procName = NormalizeToken(t.IdLavorazioneFaseDateReadingNavigation?.IdProceduraLavorazioneNavigation?.NomeProcedura);
            var faseName = NormalizeToken(t.IdLavorazioneFaseDateReadingNavigation?.IdFaseLavorazioneNavigation?.FaseLavorazione);

            // usa ConfigurazioneFontiDati per determinare il dettaglio
            if (t.IdConfigurazioneDatabase.HasValue && t.IdConfigurazioneDatabaseNavigation != null)
            {
                var tipoFonte = t.IdConfigurazioneDatabaseNavigation.TipoFonte;
                var detail = tipoFonte == nameof(TipoFonteData.HandlerIntegrato)
                    ? NormalizeToken(t.IdConfigurazioneDatabaseNavigation.HandlerClassName ?? "handler")
                    : faseName;

                return $"prod:{t.IdTaskDaEseguire}-{idProc}-{procName}:{detail}";
            }

            // FALLBACK (non dovrebbe mai succedere con nuovo sistema)
            return $"prod:{t.IdTaskDaEseguire}-{idProc}-{procName}-{faseName}";
        }



        /// <summary>
        /// Risolve l'espressione cron per un task.
        /// Priorità: CronExpression del task > default
        /// </summary>
        private static string ResolveCron(TaskDaEseguire t)
        {
            // Se il task ha già una CronExpression valorizzata, usala
            if (!string.IsNullOrWhiteSpace(t.CronExpression))
                return t.CronExpression;

            // Fallback di sicurezza
            return "0 5 * * *";
        }
        #endregion

        /// <summary>
        /// Aggiunge o aggiorna un recurring job per l'ID specificato.
        /// Versione semplificata con logging strutturato e gestione migliorata degli errori.
        /// </summary>
        public async Task<string> AddOrUpdateAsync(int idTaskDaEseguire)
        {

            using var activity = new Activity("ProductionJobScheduler.AddOrUpdate")
                .AddTag("TaskId", idTaskDaEseguire.ToString())
                .Start();
        
            _logger?.LogDebug("Inizio sincronizzazione task {TaskId}", idTaskDaEseguire);
        
            try
            {
                // Recupera il task con tutte le relazioni necessarie
                var task = await GetTaskWithRelationsAsync(idTaskDaEseguire);
                
                // Genera chiave e configurazione
                var hangfireKey = BuildHangfireKey(task);
                var cronExpression = ResolveCron(task);
                
                // Aggiorna il task nel database
                await UpdateTaskInDatabaseAsync(task, hangfireKey, cronExpression);
                
                // Registra il job in Hangfire
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
        /// <param name="idTaskDaEseguire">ID del task da recuperare</param>
        /// <returns>Task con tutte le relazioni caricate</returns>
        /// <exception cref="InvalidOperationException">Se il task non viene trovato</exception>
        private async Task<TaskDaEseguire> GetTaskWithRelationsAsync(int idTaskDaEseguire)
        {
            var task = await _db.TaskDaEseguires
                .Include(x => x.IdLavorazioneFaseDateReadingNavigation)!
                    .ThenInclude(f => f.IdProceduraLavorazioneNavigation)
                .Include(x => x.IdLavorazioneFaseDateReadingNavigation)!
                    .ThenInclude(f => f.IdFaseLavorazioneNavigation)
                .Include(x => x.IdConfigurazioneDatabaseNavigation)
                .FirstOrDefaultAsync(x => x.IdTaskDaEseguire == idTaskDaEseguire);

            if (task == null)
            {
                var errorMessage = $"Task {idTaskDaEseguire} non trovato nel database";
                _logger?.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            _logger?.LogDebug("Task {TaskId} recuperato con relazioni: Procedura={ProcedureName}, Fase={PhaseName}", 
                idTaskDaEseguire, 
                task.IdLavorazioneFaseDateReadingNavigation?.IdProceduraLavorazioneNavigation?.NomeProcedura ?? "N/A",
                task.IdLavorazioneFaseDateReadingNavigation?.IdFaseLavorazioneNavigation?.FaseLavorazione ?? "N/A");

            return task;
        }

        /// <summary>
        /// Aggiorna il task nel database con i nuovi valori se necessario.
        /// </summary>
        /// <param name="task">Task da aggiornare</param>
        /// <param name="hangfireKey">Nuova chiave Hangfire</param>
        /// <param name="cronExpression">Espressione cron calcolata</param>
        private async Task UpdateTaskInDatabaseAsync(TaskDaEseguire task, string hangfireKey, string cronExpression)
        {
            var changes = new List<string>();

            // Controlla e aggiorna IdTaskHangFire - FORZA l'aggiornamento se è un ID temporaneo
            if (task.IdTaskHangFire != hangfireKey || 
                (!string.IsNullOrWhiteSpace(task.IdTaskHangFire) && task.IdTaskHangFire.StartsWith("temp-")))
            {
                var oldKey = task.IdTaskHangFire ?? "NULL";
                task.IdTaskHangFire = hangfireKey;
                changes.Add($"HangfireKey: {oldKey} ? {hangfireKey}");
            }

            // Controlla e aggiorna CronExpression se vuota
            if (string.IsNullOrWhiteSpace(task.CronExpression) && !string.IsNullOrWhiteSpace(cronExpression))
            {
                task.CronExpression = cronExpression;
                changes.Add($"Cron: NULL ? {cronExpression}");
            }

            // Controlla e aggiorna Enabled
            if (!task.Enabled)
            {
                task.Enabled = true;
                changes.Add("Enabled: false ? true");
            }

            // Salva solo se ci sono modifiche
            if (changes.Any())
            {
                await _db.SaveChangesAsync();
                _logger?.LogDebug("Task {TaskId} aggiornato: {Changes}", 
                    task.IdTaskDaEseguire, string.Join(", ", changes));
            }
            else
            {
                _logger?.LogDebug("Task {TaskId} già aggiornato, nessuna modifica necessaria", task.IdTaskDaEseguire);
            }
        }

        /// <summary>Disabilita un task e rimuove il relativo recurring job (se presente).</summary>
        public async Task DisableAsync(int idTaskDaEseguire)
        {
            var task = await _db.TaskDaEseguires.FirstOrDefaultAsync(x => x.IdTaskDaEseguire == idTaskDaEseguire);
            if (task == null)
                return;

            task.Enabled = false;
            await _db.SaveChangesAsync();

            if (!string.IsNullOrEmpty(task.IdTaskHangFire))
                _adapter.RemoveIfExists(task.IdTaskHangFire);
        }

        /// <summary>Rimuove un recurring job direttamente dalla chiave.</summary>
        public async Task RemoveByKeyAsync(string hangfireKey)
        {
            if (string.IsNullOrWhiteSpace(hangfireKey))
                return;
            _adapter.RemoveIfExists(hangfireKey);
            await Task.CompletedTask;
        }

        /// <summary>
        /// Riesegue la sincronizzazione di tutti i task Enabled generando chiavi e cron coerenti.
        /// Utile dopo refactor naming o introduzione nuovi handler.
        /// </summary>
        public async Task SyncAllAsync()
        {
            var enabled = await _db.TaskDaEseguires
                .Include(t => t.IdLavorazioneFaseDateReadingNavigation)!.ThenInclude(f => f.IdProceduraLavorazioneNavigation)
                .Include(t => t.IdLavorazioneFaseDateReadingNavigation)!.ThenInclude(f => f.IdFaseLavorazioneNavigation)
                .Include(t => t.IdConfigurazioneDatabaseNavigation)
                .Where(t => t.Enabled)
                .ToListAsync();

            foreach (var t in enabled)
            {
                var key = BuildHangfireKey(t);
                var cron = ResolveCron(t);

                // Aggiorna se la chiave è diversa OPPURE se è un ID temporaneo
                if (t.IdTaskHangFire != key || 
                    (!string.IsNullOrWhiteSpace(t.IdTaskHangFire) && t.IdTaskHangFire.StartsWith("temp-")))
                {
                    t.IdTaskHangFire = key;
                }
                    
                if (string.IsNullOrWhiteSpace(t.CronExpression))
                    t.CronExpression = cron;

                _adapter.AddOrUpdate(t.IdTaskHangFire, t.IdTaskDaEseguire, cron);
            }
            
            // Ora riattacca le entità modificate al contesto per il salvataggio
            foreach (var t in enabled)
            {
                _db.TaskDaEseguires.Attach(t);
                _db.Entry(t).State = EntityState.Modified;
            }
            
            await _db.SaveChangesAsync();
        }

        /// <summary>
        /// Rimuove i recurring job orfani (presenti in Hangfire ma non più associati a task abilitati) e aggiunge quelli mancanti.
        /// Sincronizza le chiavi e le espressioni cron dei task abilitati.
        /// Restituisce il numero totale di job rimossi o aggiunti.
        /// </summary>
        public async Task<int> CleanupOrphansAsync()
        {
            var existingKeys = _adapter.GetRecurringJobKeys().ToHashSet();

            var enabledTasks = await _db.TaskDaEseguires
                .AsSplitQuery()
                .Include(t => t.IdLavorazioneFaseDateReadingNavigation)!.ThenInclude(f => f.IdProceduraLavorazioneNavigation)
                .Include(t => t.IdLavorazioneFaseDateReadingNavigation)!.ThenInclude(f => f.IdFaseLavorazioneNavigation)
                .Include(t => t.IdConfigurazioneDatabaseNavigation)
                .Where(t => t.Enabled)
                .ToListAsync();

            var validKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var t in enabledTasks)
            {
                var expected = BuildHangfireKey(t);
                validKeys.Add(expected);
                
                // Aggiunge anche la chiave corrente se non è temporanea, per evitare rimozioni errate durante la transizione
                if (!string.IsNullOrWhiteSpace(t.IdTaskHangFire) && !t.IdTaskHangFire.StartsWith("temp-"))
                {
                    validKeys.Add(t.IdTaskHangFire!);
                }
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
                var cron = ResolveCron(task);

                // Aggiorna se la chiave è diversa OPPURE se è un ID temporaneo
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
                await _db.SaveChangesAsync();

            return removed + added;
        }
    }

    /// <summary>
    /// Aggiorna sempre audit (LastRunUtc, errori, contatore failure).
    /// </summary>
    /// 

    public static class ProductionJobRunner
    {
        public static IServiceProvider ServiceProvider { get; set; } = default!;

        /// <summary>
        /// Esecuzione runtime (richiamata da Hangfire) di un task schedulato.
        /// Versione unificata che usa il sistema unificato per tutti i tipi di job.
        /// </summary>
        public static async Task RunAsync(int idTaskDaEseguire)
        {
            using var scope = ServiceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DematReportsContext>();
            var logger = scope.ServiceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("DataReading.Infrastructure.ProductionJobRunner");

        var entity = await db.TaskDaEseguires
                .AsSplitQuery()
                .Include(x => x.IdLavorazioneFaseDateReadingNavigation)!.ThenInclude(f => f.IdProceduraLavorazioneNavigation)
                .Include(x => x.IdLavorazioneFaseDateReadingNavigation)!.ThenInclude(f => f.IdFaseLavorazioneNavigation)
                .Include(x => x.IdConfigurazioneDatabaseNavigation)
                .FirstOrDefaultAsync(x => x.IdTaskDaEseguire == idTaskDaEseguire);

            if (entity == null)
            {
                logger?.LogWarning("Task {TaskId} non trovato", idTaskDaEseguire);
                return;
            }

            // Guard Clause: Verifica se il task è abilitato prima di eseguire
            if (!entity.Enabled)
            {
                logger?.LogInformation("Task {TaskId} disabilitato - esecuzione saltata", idTaskDaEseguire);
                return;
            }

            try
            {
                logger?.LogInformation("Avvio esecuzione task unificato {TaskId}", idTaskDaEseguire);

                // Determina il tipo di job e il codice per il registry
                var (jobType, handlerCode) = DetermineJobTypeAndCode(entity);
                logger?.LogDebug("Tipo job rilevato: {JobType}, Handler: {HandlerCode} per task {TaskId}", 
                    jobType, handlerCode, idTaskDaEseguire);

                // Esegui in base al tipo usando il sistema unificato
                switch (jobType)
                {
                    case "UnifiedHandler":
                        await ExecuteUnifiedHandlerAsync(scope, entity, handlerCode);
                        break;

                    case "DatabaseQuery":
                        // NUOVO: Tutti i task SQL usano il sistema unificato tramite UnifiedDataSourceHandler
                        await ExecuteUnifiedDataSourceAsync(scope, entity);
                        break;

                    default:
                        throw new InvalidOperationException($"Tipo job non supportato: {jobType}");
                }

                MarkSuccess(entity);
                logger?.LogInformation("Task {TaskId} completato con successo: {JobType}", 
                    idTaskDaEseguire, jobType);
            }
            catch (Exception ex)
            {
                MarkFailure(entity, ex);
                logger?.LogError(ex, "Errore durante l'esecuzione del task {TaskId}", idTaskDaEseguire);
                throw;
            }
            finally
            {
                await db.SaveChangesAsync();
            }
        }

        #region Job Type Detection and Execution
        
        /// <summary>
        /// Determina il tipo di job basandosi su IdConfigurazioneDatabase.
        /// Tutti i task DEVONO avere IdConfigurazioneDatabase configurato.
        /// </summary>
        /// 
        private static (string jobType, string handlerCode) DetermineJobTypeAndCode(TaskDaEseguire entity)
        {
            if (!entity.IdConfigurazioneDatabase.HasValue || entity.IdConfigurazioneDatabaseNavigation == null)
            {
                throw new InvalidOperationException(
                    $"Task {entity.IdTaskDaEseguire} non ha IdConfigurazioneDatabase. " +
                    "Tutti i task devono essere configurati tramite /admin/fonti-dati");
            }

            var config = entity.IdConfigurazioneDatabaseNavigation;

            // Parse TipoFonte da string
            if (!Enum.TryParse<TipoFonteData>(config.TipoFonte, out var tipoFonte))
            {
                throw new InvalidOperationException(
                    $"TipoFonte '{config.TipoFonte}' non è valido per task {entity.IdTaskDaEseguire}");
            }

            return tipoFonte switch
            {
                TipoFonteData.SQL => ("DatabaseQuery", config.ConnectionStringName ?? ""),
                TipoFonteData.HandlerIntegrato => ("UnifiedHandler", config.HandlerClassName ?? ""),
                _ => throw new InvalidOperationException(
                    $"TipoFonte '{tipoFonte}' non supportato per task {entity.IdTaskDaEseguire}")
            };
        }

        /// <summary>
        /// Esegue un job usando il sistema unificato degli handler.
        /// </summary>
        private static async Task ExecuteUnifiedHandlerAsync(IServiceScope scope, TaskDaEseguire entity, string handlerCode)
        {
            var unifiedService = scope.ServiceProvider.GetRequiredService<IUnifiedHandlerService>();
            
            var context = new UnifiedExecutionContext
            {
                IDProceduraLavorazione = entity.IdLavorazioneFaseDateReadingNavigation.IdProceduraLavorazione,
                ServiceProvider = scope.ServiceProvider,
                HandlerCode = handlerCode,
                Parameters = new Dictionary<string, object>
                {
                    { "IDFaseLavorazione", entity.IdLavorazioneFaseDateReadingNavigation.IdFaseLavorazione },
                    { "NomeProcedura", entity.IdLavorazioneFaseDateReadingNavigation?.IdProceduraLavorazioneNavigation?.NomeProceduraProgramma ?? "UNKNOWN" },
                    { "StartDataLavorazione", DateTime.Now.AddDays(-(entity.GiorniPrecedenti ?? 1)) },
                    { "EndDataLavorazione", DateTime.Now },
                    { "TaskId", entity.IdTaskDaEseguire },
                    { "IdConfigurazioneDatabase", entity.IdConfigurazioneDatabase ?? 0 }
                }
            };

            await unifiedService.ExecuteHandlerAsync(handlerCode, context, CancellationToken.None);
        }

        /// <summary>
        /// Esegue un job di tipo produzione/SQL usando il sistema unificato.
        /// Tutti i task SQL devono avere IdConfigurazioneDatabase configurato.
        /// </summary>
        private static async Task ExecuteUnifiedDataSourceAsync(IServiceScope scope, TaskDaEseguire entity)
        {
            if (!entity.IdConfigurazioneDatabase.HasValue)
            {
                throw new InvalidOperationException(
                    $"Task {entity.IdTaskDaEseguire} non ha IdConfigurazioneDatabase. " +
                    "Tutti i task devono essere configurati tramite /admin/fonti-dati");
            }

            var db = scope.ServiceProvider.GetRequiredService<DematReportsContext>();
            var queryService = scope.ServiceProvider.GetRequiredService<IQueryService>();
            var lavorazioniConfig = scope.ServiceProvider.GetRequiredService<ILavorazioniConfigManager>();
            
            // Carica configurazione con mappings
            var config = await db.ConfigurazioneFontiDatis
                .Include(c => c.ConfigurazioneFaseCentros)
                .FirstOrDefaultAsync(c => c.IdConfigurazione == entity.IdConfigurazioneDatabase.Value);

            if (config == null)
            {
                throw new InvalidOperationException(
                    $"Configurazione {entity.IdConfigurazioneDatabase.Value} non trovata per task {entity.IdTaskDaEseguire}");
            }

            if (config.TipoFonte != nameof(TipoFonteData.SQL))
            {
                throw new InvalidOperationException(
                    $"Configurazione {config.IdConfigurazione} ha TipoFonte='{config.TipoFonte}' invece di 'SQL'");
            }

            // Trova il mapping corretto per questa fase
            // Nota: ConfigurazioneFaseCentro contiene IdCentro, LavorazioniFasiDataReading no
            var idFaseLavorazione = entity.IdLavorazioneFaseDateReadingNavigation.IdFaseLavorazione;

            var mapping = config.ConfigurazioneFaseCentros.FirstOrDefault(fc =>
                fc.IdFaseLavorazione == idFaseLavorazione &&
                fc.FlagAttiva == true);

            // Usa query specifica del mapping se presente
            var query = mapping?.TestoQueryTask;
            
            if (string.IsNullOrWhiteSpace(query))
            {
                throw new InvalidOperationException(
                    $"Nessuna query configurata per task {entity.IdTaskDaEseguire}. " +
                    "Configurare TestoQueryTask nel mapping Fase/Centro.");
            }

            // Esegui query SQL
            var connectionStringProperty = lavorazioniConfig.GetType().GetProperty(config.ConnectionStringName);
            if (connectionStringProperty?.GetValue(lavorazioniConfig)?.ToString() is not string connectionString)
            {
                throw new InvalidOperationException(
                    $"Connection string '{config.ConnectionStringName}' non trovata");
            }

            var startDate = DateTime.Now.AddDays(-(entity.GiorniPrecedenti ?? 1));
            var endDate = DateTime.Now;

            // Esegui query - i risultati vengono salvati automaticamente da ExecuteQueryAsync
            await queryService.ExecuteQueryAsync(
                connectionString,
                query,
                startDate,
                endDate);
        }

        /// <summary>
        /// Marca un task come completato con successo.
        /// </summary>
        private static void MarkSuccess(TaskDaEseguire entity)
        {
            entity.LastRunUtc = DateTime.UtcNow;
            entity.LastError = null;
            entity.Stato = "COMPLETED";
            entity.DataStato = DateTime.Now;
        }

        /// <summary>
        /// Marca un task come fallito con informazioni sull'errore.
        /// </summary>
        private static void MarkFailure(TaskDaEseguire entity, Exception ex)
        {
            entity.LastRunUtc = DateTime.UtcNow;
            entity.LastError = ex.Message;
            entity.Stato = "ERROR";
            entity.DataStato = DateTime.Now;
        }
        #endregion
    }
}

