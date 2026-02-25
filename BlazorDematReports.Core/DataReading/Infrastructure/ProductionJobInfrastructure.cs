using BlazorDematReports.Core.Constants;
using BlazorDematReports.Core.DataReading.Interfaces;
using BlazorDematReports.Core.Lavorazioni.Interfaces;
using BlazorDematReports.Core.Lavorazioni.Models;
using BlazorDematReports.Core.Utility.Interfaces;
using BlazorDematReports.Core.Utility.Models;
using Entities.Enums;
using Entities.Models.DbApplication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace BlazorDematReports.Core.DataReading.Infrastructure
{
    /// <summary>
    /// Scheduler per task di produzione in tabella TaskDaEseguire.
    /// Costruisce la chiave Hangfire in base al tipo di task.
    /// Formato: {tipoAbbreviato}:{IdTaskDaEseguire}-{nomeprocedura}:{dettaglio}
    ///   - sql:{id}-{proc}:{fase}         per TipoFonte.SQL
    ///   - hdl:{id}-{proc}:{handlercode}  per TipoFonte.HandlerIntegrato
    /// </summary>
    public sealed class ProductionJobScheduler : IProductionJobScheduler
    {
        private readonly IDbContextFactory<DematReportsContext> _contextFactory;
        private readonly IRecurringJobManagerAdapter _adapter;
        private readonly ILogger<ProductionJobScheduler>? _logger;

        public ProductionJobScheduler(IDbContextFactory<DematReportsContext> contextFactory, IRecurringJobManagerAdapter adapter, ILogger<ProductionJobScheduler>? logger = null)
        {
            _contextFactory = contextFactory;
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
        /// Formato: {tipoAbbreviato}:{IdTaskDaEseguire}-{nomeprocedura}:{dettaglio}
        ///   - sql:{id}-{proc}:{fase}         per TipoFonte.SQL
        ///   - hdl:{id}-{proc}:{handlercode}  per TipoFonte.HandlerIntegrato
        /// </summary>
        private static string BuildHangfireKey(TaskDaEseguire t)
        {
            var idProc   = t.IdLavorazioneFaseDateReadingNavigation?.IdProceduraLavorazione;
            var procName = NormalizeToken(t.IdLavorazioneFaseDateReadingNavigation?.IdProceduraLavorazioneNavigation?.NomeProcedura);
            var faseName = NormalizeToken(t.IdLavorazioneFaseDateReadingNavigation?.IdFaseLavorazioneNavigation?.FaseLavorazione);

            if (t.IdConfigurazioneDatabase.HasValue && t.IdConfigurazioneDatabaseNavigation != null)
            {
                var config = t.IdConfigurazioneDatabaseNavigation;

                var (prefix, detail) = config.TipoFonte switch
                {
                    TipoFonteData.SQL             => ("sql", faseName),
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
                await using var context = await _contextFactory.CreateDbContextAsync();

                // Recupera il task con tutte le relazioni necessarie
                var task = await GetTaskWithRelationsAsync(context, idTaskDaEseguire);

                // Genera chiave e configurazione
                var hangfireKey = BuildHangfireKey(task);
                var cronExpression = ResolveCron(task);

                // Aggiorna il task nel database
                await UpdateTaskInDatabaseAsync(context, task, hangfireKey, cronExpression);

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
        private async Task<TaskDaEseguire> GetTaskWithRelationsAsync(DematReportsContext context, int idTaskDaEseguire)
        {
            var task = await context.TaskDaEseguires
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
        private async Task UpdateTaskInDatabaseAsync(DematReportsContext context, TaskDaEseguire task, string hangfireKey, string cronExpression)
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
                await context.SaveChangesAsync();
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
            await using var context = await _contextFactory.CreateDbContextAsync();
            var task = await context.TaskDaEseguires.FirstOrDefaultAsync(x => x.IdTaskDaEseguire == idTaskDaEseguire);
            if (task == null)
                return;

            task.Enabled = false;
            await context.SaveChangesAsync();

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
        /// Sincronizza tutti i task abilitati: ricalcola chiavi e cron, rimuove le chiavi
        /// Hangfire obsolete e registra quelle nuove. Utile dopo refactor del naming.
        /// </summary>
        public async Task SyncAllAsync()
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
                    // Rimuove la vecchia chiave PRIMA di registrarne una nuova
                    if (!string.IsNullOrWhiteSpace(oldKey))
                        _adapter.RemoveIfExists(oldKey);

                    t.IdTaskHangFire = expectedKey;
                }

                if (string.IsNullOrWhiteSpace(t.CronExpression))
                    t.CronExpression = cron;

                _adapter.AddOrUpdate(t.IdTaskHangFire, t.IdTaskDaEseguire, cron);
            }

            // Le entità sono già tracciate dalla query: SaveChanges persiste tutte le modifiche
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Rimuove i recurring job orfani (presenti in Hangfire ma non più associati a task abilitati) e aggiunge quelli mancanti.
        /// Sincronizza le chiavi e le espressioni cron dei task abilitati.
        /// Restituisce il numero totale di job rimossi o aggiunti.
        /// </summary>
        public async Task<int> CleanupOrphansAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
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
                await context.SaveChangesAsync();

            return removed + added;
        }
    }


    /// <summary>
    /// Provides methods to execute production jobs using Hangfire, managing task execution, dependency injection
    /// scopes, and task state updates.
    /// </summary>
    /// <remarks>This static class serves as the entry point for running production tasks in a background job
    /// context. Each job is executed within its own dependency injection scope to ensure proper resource management and
    /// isolation. The class handles task retrieval, execution, and logging, and updates the task's state to reflect
    /// success or failure. It supports both SQL-based and custom handler data acquisition strategies, and ensures that
    /// only enabled tasks are executed. Use this class to coordinate the full lifecycle of production job execution,
    /// including data acquisition, processing, and persistence.</remarks>
    public static class ProductionJobRunner
    {
        private static IServiceScopeFactory? _scopeFactory;

        /// <summary>
        /// Inizializza il runner con la factory DI. Deve essere chiamato una sola volta all'avvio dell'applicazione.
        /// Lancia <see cref="InvalidOperationException"/> se chiamato più di una volta.
        /// </summary>
        /// <param name="factory">Factory per la creazione di scope DI per ogni job Hangfire.</param>
        /// <exception cref="ArgumentNullException">Se <paramref name="factory"/> è null.</exception>
        /// <exception cref="InvalidOperationException">Se già inizializzato.</exception>
        public static void Initialize(IServiceScopeFactory factory)
        {
            ArgumentNullException.ThrowIfNull(factory);

            if (_scopeFactory is not null)
                throw new InvalidOperationException("ProductionJobRunner è già stato inizializzato.");

            _scopeFactory = factory;
        }

        /// <summary>Entry point Hangfire: carica il task, verifica lo stato e delega l'esecuzione.</summary>
        public static async Task RunAsync(int idTaskDaEseguire, CancellationToken cancellationToken = default)
        {
            await RunAsync(idTaskDaEseguire, startDate: null, endDate: null, cancellationToken);
        }

        /// <summary>
        /// Entry point per esecuzione manuale con range date custom.
        /// Se startDate/endDate sono null, usa GiorniPrecedenti del task.
        /// </summary>
        /// <param name="idTaskDaEseguire">ID del task da eseguire.</param>
        /// <param name="startDate">Data inizio custom (null = usa GiorniPrecedenti).</param>
        /// <param name="endDate">Data fine custom (null = usa DateTime.Now).</param>
        /// <param name="cancellationToken">Token per cancellazione.</param>
        public static async Task RunAsync(
            int idTaskDaEseguire, 
            DateTime? startDate, 
            DateTime? endDate, 
            CancellationToken cancellationToken = default)
        {
            if (_scopeFactory is null)
                throw new InvalidOperationException("ProductionJobRunner non è stato inizializzato. Chiamare Initialize() all'avvio dell'applicazione.");

            // Crea un nuovo scope DI per ogni esecuzione del job
            using var scope = _scopeFactory.CreateScope();
            var db     = scope.ServiceProvider.GetRequiredService<DematReportsContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
                             .CreateLogger("ProductionJobRunner");

            var entity = await db.TaskDaEseguires
                .AsSplitQuery()
                .Include(x => x.IdLavorazioneFaseDateReadingNavigation)!.ThenInclude(f => f.IdProceduraLavorazioneNavigation)
                .Include(x => x.IdLavorazioneFaseDateReadingNavigation)!.ThenInclude(f => f.IdFaseLavorazioneNavigation)
                .Include(x => x.IdConfigurazioneDatabaseNavigation)
                .FirstOrDefaultAsync(x => x.IdTaskDaEseguire == idTaskDaEseguire, cancellationToken);

            if (entity == null)
            {
                logger.LogWarning("Task {TaskId} non trovato", idTaskDaEseguire);
                return;
            }

            // Guard Clause: Verifica se il task è abilitato prima di eseguire
            if (!entity.Enabled)
            {
                logger.LogInformation("Task {TaskId} disabilitato, esecuzione saltata", idTaskDaEseguire);
                return;
            }

            try
            {
                await ExecuteProductionTaskAsync(scope, entity, startDate, endDate, cancellationToken);
                MarkSuccess(entity);
                logger.LogInformation("Task {TaskId} completato", idTaskDaEseguire);
            }
            catch (Exception ex)
            {
                MarkFailure(entity, ex);
                logger.LogError(ex, "Errore task {TaskId}", idTaskDaEseguire);
                throw;
            }
            finally
            {
                await db.SaveChangesAsync(cancellationToken);
            }
        }


        #region Unified Execution Pipeline

        /// <summary>
        /// Punto di ingresso unificato per l'esecuzione di tutti i task di produzione.
        /// Pipeline: Acquire → Elaborate → Persist (comune per SQL e HandlerIntegrato).
        /// </summary>
        private static async Task ExecuteProductionTaskAsync(
            IServiceScope scope, 
            TaskDaEseguire entity, 
            DateTime? startDate, 
            DateTime? endDate, 
            CancellationToken ct)
        {
            var db          = scope.ServiceProvider.GetRequiredService<DematReportsContext>();
            var elaboratore = scope.ServiceProvider.GetRequiredService<IElaboratoreDatiLavorazione>();
            var logger      = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
                                  .CreateLogger("ProductionJobRunner");
            var fase = entity.IdLavorazioneFaseDateReadingNavigation;

            // Carica configurazione + mapping (comune per entrambi i path)
            var config = await db.ConfigurazioneFontiDatis
                .Include(c => c.ConfigurazioneFaseCentros)
                .FirstOrDefaultAsync(c => c.IdConfigurazione == entity.IdConfigurazioneDatabase, ct)
                ?? throw new InvalidOperationException(
                    $"Configurazione {entity.IdConfigurazioneDatabase} non trovata per task {entity.IdTaskDaEseguire}");

            var mapping = config.ConfigurazioneFaseCentros
                .FirstOrDefault(fc => fc.IdFaseLavorazione == fase.IdFaseLavorazione && fc.FlagAttiva == true)
                ?? throw new InvalidOperationException(
                    $"Nessun mapping attivo per fase {fase.IdFaseLavorazione} nella configurazione {config.IdConfigurazione}. " +
                    "Verificare i mapping Fase/Centro in /admin/fonti-dati.");

            // STRATEGY: Acquisisce dati in base al TipoFonte
            var datiLavorazione = await AcquireDatiLavorazioneAsync(
                scope, entity, config, mapping, startDate, endDate, ct);

            if (datiLavorazione.Count == 0)
            {
                logger.LogInformation(
                    "Task {TaskId}: nessun dato acquisito per il periodo configurato",
                    entity.IdTaskDaEseguire);
                return;
            }

            // Pipeline condivisa: elabora operatori e normalizza
            var datiElaborati = await elaboratore.ElaboraDatiLavorazioneAsync(
                datiLavorazione,
                mapping.IdCentro,
                fase.IdProceduraLavorazione,
                fase.IdFaseLavorazione,
                ct);

            // Persiste con strategia delete-then-reinsert
            int saved = await PersistProduzioneSistemaAsync(db, datiElaborati, ct);

            // Log audit: registra esecuzione in TaskDataReadingAggiornamento
            await LogTaskExecutionAsync(db, entity, fase, datiElaborati, startDate, endDate, saved, ct);

            logger.LogInformation(
                "Task {TaskId}: {Saved} record salvati in ProduzioneSistema su {Total} elaborati",
                entity.IdTaskDaEseguire, saved, datiElaborati.Count);
        }

        /// <summary>
        /// Strategy method: acquisisce dati dalla sorgente in base al TipoFonte.
        /// Restituisce sempre List&lt;DatiLavorazione&gt; indipendentemente dalla sorgente.
        /// </summary>
        private static Task<List<DatiLavorazione>> AcquireDatiLavorazioneAsync(
            IServiceScope scope,
            TaskDaEseguire entity,
            ConfigurazioneFontiDati config,
            ConfigurazioneFaseCentro mapping,
            DateTime? startDate,
            DateTime? endDate,
            CancellationToken ct)
        {
            return config.TipoFonte switch
            {
                TipoFonteData.SQL              => AcquireFromSqlAsync(scope, entity, config, mapping, startDate, endDate, ct),
                TipoFonteData.HandlerIntegrato => AcquireFromHandlerAsync(scope, entity, config, mapping, startDate, endDate, ct),
                _ => throw new InvalidOperationException(
                    $"TipoFonte '{config.TipoFonte}' non supportato per task {entity.IdTaskDaEseguire}")
            };
        }

        /// <summary>Strategy SQL: esegue query configurata e converte DataTable a DatiLavorazione.</summary>
        private static async Task<List<DatiLavorazione>> AcquireFromSqlAsync(
            IServiceScope scope,
            TaskDaEseguire entity,
            ConfigurazioneFontiDati config,
            ConfigurazioneFaseCentro mapping,
            DateTime? startDate,
            DateTime? endDate,
            CancellationToken ct)
        {
            var queryService      = scope.ServiceProvider.GetRequiredService<IQueryService>();
            var lavorazioniConfig = scope.ServiceProvider.GetRequiredService<ILavorazioniConfigManager>();
            var logger            = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
                                        .CreateLogger("ProductionJobRunner");

            var query = mapping.TestoQueryTask;
            if (string.IsNullOrWhiteSpace(query))
            {
                throw new InvalidOperationException(
                    $"Nessuna query configurata per task {entity.IdTaskDaEseguire}. " +
                    "Configurare TestoQueryTask nel mapping Fase/Centro.");
            }

            var connectionString = lavorazioniConfig.GetConnectionString(config.ConnectionStringName);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    $"Connection string '{config.ConnectionStringName}' non trovata in appsettings.");
            }

            var effectiveStartDate = startDate ?? DateTime.Now.AddDays(-(entity.GiorniPrecedenti ?? TaskConfigurationDefaults.DefaultGiorniPrecedenti));
            var effectiveEndDate = endDate ?? DateTime.Now;
            var dataTable = await queryService.ExecuteQueryAsync(connectionString, query, effectiveStartDate, effectiveEndDate, ct);

            if (dataTable.Rows.Count == 0)
            {
                logger.LogInformation(
                    "Task {TaskId}: nessun dato dalla query SQL per periodo {Start:d}-{End:d}",
                    entity.IdTaskDaEseguire, effectiveStartDate, effectiveEndDate);
                return new List<DatiLavorazione>();
            }

            return DataTableToDatiLavorazione(dataTable);
        }

        /// <summary>Strategy Handler: esegue handler integrato custom e cattura DatiLavorazione.</summary>
        private static async Task<List<DatiLavorazione>> AcquireFromHandlerAsync(
            IServiceScope scope,
            TaskDaEseguire entity,
            ConfigurazioneFontiDati config,
            ConfigurazioneFaseCentro mapping,
            DateTime? startDate,
            DateTime? endDate,
            CancellationToken ct)
        {
            var unifiedService = scope.ServiceProvider.GetRequiredService<IUnifiedHandlerService>();
            var logger         = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
                                     .CreateLogger("ProductionJobRunner");
            var fase = entity.IdLavorazioneFaseDateReadingNavigation;

            var handlerCode = config.HandlerClassName;
            if (string.IsNullOrWhiteSpace(handlerCode))
            {
                throw new InvalidOperationException(
                    $"HandlerClassName mancante nella configurazione {config.IdConfigurazione}");
            }

            var effectiveStartDate = startDate ?? DateTime.Now.AddDays(-(entity.GiorniPrecedenti ?? TaskConfigurationDefaults.DefaultGiorniPrecedenti));
            var effectiveEndDate = endDate ?? DateTime.Now;

            var context = new UnifiedExecutionContext
            {
                IDProceduraLavorazione = fase.IdProceduraLavorazione,
                HandlerCode            = handlerCode,
                Parameters = new Dictionary<string, object>
                {
                    { "IDFaseLavorazione",        fase.IdFaseLavorazione },
                    { "IDCentro",                 mapping.IdCentro },
                    { "NomeProcedura",            fase.IdProceduraLavorazioneNavigation?.NomeProceduraProgramma ?? "UNKNOWN" },
                    { "StartDataLavorazione",     effectiveStartDate },
                    { "EndDataLavorazione",       effectiveEndDate },
                    { "TaskId",                   entity.IdTaskDaEseguire },
                    { "IdConfigurazioneDatabase", entity.IdConfigurazioneDatabase ?? 0 }
                }
            };

            var result = await unifiedService.ExecuteHandlerAsync(handlerCode, context, ct);

            if (result is not List<DatiLavorazione> datiLavorazione)
            {
                logger.LogWarning(
                    "Handler {Code} ha restituito tipo inatteso: {Type}",
                    handlerCode, result?.GetType().Name ?? "null");
                return new List<DatiLavorazione>();
            }

            if (datiLavorazione.Count == 0)
            {
                logger.LogInformation("Handler {Code}: nessun dato restituito", handlerCode);
            }

            return datiLavorazione;
        }

        #endregion

        #region Data Transformation

        /// <summary>
        /// Converte un DataTable con le colonne standard in una lista di DatiLavorazione.
        /// Le colonne sono cercate in modo case-insensitive.
        /// Colonne obbligatorie: Operatore, DataLavorazione, Documenti, Fogli, Pagine.
        /// </summary>
        private static List<DatiLavorazione> DataTableToDatiLavorazione(DataTable table)
        {
            string ColName(string name) =>
                table.Columns.Cast<DataColumn>()
                     .FirstOrDefault(c => c.ColumnName.Equals(name, StringComparison.OrdinalIgnoreCase))
                     ?.ColumnName
                ?? throw new InvalidOperationException(
                    $"Colonna '{name}' mancante nel risultato della query. " +
                    "Le query devono restituire: Operatore, DataLavorazione, Documenti, Fogli, Pagine.");

            var colOp   = ColName("Operatore");
            var colData = ColName("DataLavorazione");
            var colDoc  = ColName("Documenti");
            var colFog  = ColName("Fogli");
            var colPag  = ColName("Pagine");

            var result = new List<DatiLavorazione>(table.Rows.Count);
            foreach (DataRow row in table.Rows)
            {
                result.Add(new DatiLavorazione
                {
                    Operatore                  = row[colOp]?.ToString()?.Trim(),
                    DataLavorazione            = Convert.ToDateTime(row[colData]),
                    Documenti                  = row[colDoc] == DBNull.Value ? 0 : Convert.ToInt32(row[colDoc]),
                    Fogli                      = row[colFog] == DBNull.Value ? 0 : Convert.ToInt32(row[colFog]),
                    Pagine                     = row[colPag] == DBNull.Value ? 0 : Convert.ToInt32(row[colPag]),
                    AppartieneAlCentroSelezionato = true
                });
            }
            return result;
        }

        /// <summary>
        /// Persiste i dati elaborati in ProduzioneSistema con strategia delete-then-reinsert:
        /// 1. Elimina i record auto-inseriti nel range di date per proc+fase (dati stale rimossi).
        /// 2. Inserisce tutti i record nuovi restituiti dalla query.
        /// I record con FlagInserimentoManuale=true non vengono mai toccati.
        /// Salta i record con IdOperatore=0 (operatori non riconosciuti — FK non valida).
        /// </summary>
        /// <returns>Numero di record inseriti.</returns>
        private static async Task<int> PersistProduzioneSistemaAsync(
            DematReportsContext db,
            List<DatiElaborati> datiElaborati,
            CancellationToken ct)
        {
            if (datiElaborati.Count == 0) return 0;

            var dates  = datiElaborati.Select(d => d.DataLavorazione.Date).Distinct().ToList();
            var idProc = datiElaborati[0].IdProceduraLavorazione;
            var idFase = datiElaborati[0].IdFaseLavorazione;

            // 1. Elimina i record auto-inseriti nel range — i dati vecchi/scaduti vengono rimossi
            var toDelete = await db.ProduzioneSistemas
                .Where(p => p.IdProceduraLavorazione == idProc &&
                            p.IdFaseLavorazione      == idFase &&
                            dates.Contains(p.DataLavorazione.Date) &&
                            p.FlagInserimentoAuto    == true &&
                            p.FlagInserimentoManuale != true)
                .ToListAsync(ct);

            if (toDelete.Count > 0)
                db.ProduzioneSistemas.RemoveRange(toDelete);

            // 2. Recupera le chiavi con inserimento manuale per evitare duplicati
            var manualKeys = await db.ProduzioneSistemas
                .Where(p => p.IdProceduraLavorazione == idProc &&
                            p.IdFaseLavorazione      == idFase &&
                            dates.Contains(p.DataLavorazione.Date) &&
                            p.FlagInserimentoManuale == true)
                .Select(p => new { p.IdOperatore, Data = p.DataLavorazione.Date })
                .ToListAsync(ct);

            var manualSet = manualKeys
                .Select(k => (k.IdOperatore, k.Data))
                .ToHashSet();

            // 3. Inserisce i record aggiornati dalla query sorgente
            var now = DateTime.Now;
            int inserted = 0;

            foreach (var dato in datiElaborati)
            {
                // FK non valida
                if (dato.IdOperatore == 0)
                    continue;

                // Non sovrascrivere inserimenti manuali dell'utente
                if (manualSet.Contains((dato.IdOperatore, dato.DataLavorazione.Date)))
                    continue;

                db.ProduzioneSistemas.Add(new ProduzioneSistema
                {
                    IdOperatore              = dato.IdOperatore,
                    Operatore                = dato.Operatore,
                    OperatoreNonRiconosciuto = dato.OperatoreNonRiconosciuto,
                    IdProceduraLavorazione   = dato.IdProceduraLavorazione,
                    IdFaseLavorazione        = dato.IdFaseLavorazione,
                    IdCentro                 = dato.IdCentro,
                    DataLavorazione          = dato.DataLavorazione.Date,
                    DataAggiornamento        = now,
                    Documenti                = dato.Documenti,
                    Fogli                    = dato.Fogli,
                    Pagine                   = dato.Pagine,
                    FlagInserimentoAuto      = true,
                    FlagInserimentoManuale   = false
                });
                inserted++;
            }

            // Salva delete + insert in una transazione atomica
            await db.SaveChangesAsync(ct);

            return inserted;
        }

        /// <summary>
        /// Registra l'esecuzione del task nella tabella di audit TaskDataReadingAggiornamento.
        /// </summary>
        private static async Task LogTaskExecutionAsync(
            DematReportsContext db,
            TaskDaEseguire entity,
            LavorazioniFasiDataReading fase,
            List<DatiElaborati> datiElaborati,
            DateTime? startDate,
            DateTime? endDate,
            int recordsSaved,
            CancellationToken ct)
        {
            var effectiveStartDate = startDate ?? DateTime.Now.AddDays(-(entity.GiorniPrecedenti ?? TaskConfigurationDefaults.DefaultGiorniPrecedenti));
            var effectiveEndDate = endDate ?? DateTime.Now;

            var auditLog = new TaskDataReadingAggiornamento
            {
                Lavorazione = fase.IdProceduraLavorazioneNavigation?.NomeProcedura ?? "UNKNOWN",
                IdLavorazione = fase.IdProceduraLavorazione,
                FaseLavorazione = fase.IdFaseLavorazioneNavigation?.FaseLavorazione ?? "UNKNOWN",
                IdFase = fase.IdFaseLavorazione,
                DataInizioLavorazione = effectiveStartDate.Date,
                DataFineLavorazione = effectiveEndDate.Date,
                DataAggiornamento = DateTime.Now,
                Risultati = recordsSaved,
                EsitoLetturaDato = true,
                DescrizioneEsito = $"Task {entity.IdTaskDaEseguire}: {recordsSaved} record salvati su {datiElaborati.Count} elaborati"
            };

            db.TaskDataReadingAggiornamentos.Add(auditLog);
            await db.SaveChangesAsync(ct);
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

