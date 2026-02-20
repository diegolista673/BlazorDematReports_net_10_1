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
    /// Scheduler unificato per task di produzione e task di import mail basati sulla tabella TaskDaEseguire.
    /// Costruisce la chiave Hangfire in base al tipo di task.
    /// Formato: {tipoAbbreviato}:{IdTaskDaEseguire}-{nomeprocedura}:{dettaglio}
    ///   - sql:{id}-{proc}:{fase}         per TipoFonte.SQL
    ///   - hdl:{id}-{proc}:{handlercode}  per TipoFonte.HandlerIntegrato
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
        /// Sincronizza tutti i task abilitati: ricalcola chiavi e cron, rimuove le chiavi
        /// Hangfire obsolete e registra quelle nuove. Utile dopo refactor del naming.
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

    /// <summary>Esegue i task schedulati da Hangfire leggendo dati dalla sorgente e salvandoli in ProduzioneSistema.</summary>
    public static class ProductionJobRunner
    {
        /// <summary>Factory DI per la creazione di scope per ogni job Hangfire.</summary>
        public static IServiceScopeFactory ScopeFactory { get; set; } = default!;

        /// <summary>Entry point Hangfire: carica il task, verifica lo stato e delega l'esecuzione.</summary>
        public static async Task RunAsync(int idTaskDaEseguire, CancellationToken cancellationToken = default)
        {
            // Crea un nuovo scope DI per ogni esecuzione del job
            using var scope = ScopeFactory.CreateScope();
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
                await DispatchAsync(scope, entity, cancellationToken);
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


        #region Dispatch e Execution

        /// <summary>
        /// Smista l'esecuzione in base a <see cref="TipoFonteData"/> eliminando magic strings.
        /// </summary>
        private static Task DispatchAsync(IServiceScope scope, TaskDaEseguire entity, CancellationToken ct)
        {
            var config = entity.IdConfigurazioneDatabaseNavigation
                ?? throw new InvalidOperationException(
                    $"Task {entity.IdTaskDaEseguire}: IdConfigurazioneDatabase mancante. " +
                    "Configurare tramite /admin/fonti-dati.");

            return config.TipoFonte switch
            {
                TipoFonteData.SQL              => ExecuteUnifiedDataSourceAsync(scope, entity, ct),
                TipoFonteData.HandlerIntegrato => ExecuteUnifiedHandlerAsync(scope, entity, config.HandlerClassName ?? "", ct),
                _ => throw new InvalidOperationException(
                    $"TipoFonte '{config.TipoFonte}' non supportato per task {entity.IdTaskDaEseguire}.")
            };
        }



        /// <summary>Esegue un handler tramite <see cref="IUnifiedHandlerService"/>.</summary>
        private static async Task ExecuteUnifiedHandlerAsync(
            IServiceScope scope, TaskDaEseguire entity, string handlerCode, CancellationToken ct)
        {
            var unifiedService = scope.ServiceProvider.GetRequiredService<IUnifiedHandlerService>();
            var fase           = entity.IdLavorazioneFaseDateReadingNavigation;

            var context = new UnifiedExecutionContext
            {
                IDProceduraLavorazione = fase.IdProceduraLavorazione,
                HandlerCode            = handlerCode,
                Parameters = new Dictionary<string, object>
                {
                    { "IDFaseLavorazione",    fase.IdFaseLavorazione },
                    { "NomeProcedura",        fase.IdProceduraLavorazioneNavigation?.NomeProceduraProgramma ?? "UNKNOWN" },
                    { "StartDataLavorazione", DateTime.Now.AddDays(-(entity.GiorniPrecedenti ?? TaskConfigurationDefaults.DefaultGiorniPrecedenti)) },
                    { "EndDataLavorazione",   DateTime.Now },
                    { "TaskId",               entity.IdTaskDaEseguire },
                    { "IdConfigurazioneDatabase", entity.IdConfigurazioneDatabase ?? 0 }
                }
            };

            await unifiedService.ExecuteHandlerAsync(handlerCode, context, ct);
        }

        /// <summary>Esegue una query SQL, elabora i dati e li persiste in ProduzioneSistema.</summary>
        private static async Task ExecuteUnifiedDataSourceAsync(
            IServiceScope scope, TaskDaEseguire entity, CancellationToken cancellationToken = default)
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
            var elaboratore = scope.ServiceProvider.GetRequiredService<IElaboratoreDatiLavorazione>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger("DataReading.Infrastructure.ProductionJobRunner");

            // Carica configurazione con mappings
            var config = await db.ConfigurazioneFontiDatis
                .Include(c => c.ConfigurazioneFaseCentros)
                .FirstOrDefaultAsync(c => c.IdConfigurazione == entity.IdConfigurazioneDatabase.Value, cancellationToken);

            if (config == null)
            {
                throw new InvalidOperationException(
                    $"Configurazione {entity.IdConfigurazioneDatabase.Value} non trovata per task {entity.IdTaskDaEseguire}");
            }

            if (config.TipoFonte != TipoFonteData.SQL)
            {
                throw new InvalidOperationException(
                    $"Configurazione {config.IdConfigurazione} ha TipoFonte='{config.TipoFonte}' invece di 'SQL'");
            }

            var idFaseLavorazione = entity.IdLavorazioneFaseDateReadingNavigation.IdFaseLavorazione;
            var idProceduraLavorazione = entity.IdLavorazioneFaseDateReadingNavigation.IdProceduraLavorazione;

            // Trova il mapping per questa fase (include IdCentro necessario per ElaboraDatiLavorazione)
            var mapping = config.ConfigurazioneFaseCentros.FirstOrDefault(fc =>
                fc.IdFaseLavorazione == idFaseLavorazione &&
                fc.FlagAttiva == true);

            if (mapping == null)
            {
                throw new InvalidOperationException(
                    $"Nessun mapping attivo per fase {idFaseLavorazione} nella configurazione {config.IdConfigurazione}. " +
                    "Verificare i mapping Fase/Centro in /admin/fonti-dati.");
            }

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

            var startDate = DateTime.Now.AddDays(-(entity.GiorniPrecedenti ?? TaskConfigurationDefaults.DefaultGiorniPrecedenti));
            var endDate = DateTime.Now;

            // Esegue la query SQL
            var dataTable = await queryService.ExecuteQueryAsync(connectionString, query, startDate, endDate, cancellationToken);

            if (dataTable.Rows.Count == 0)
            {
                logger.LogInformation(
                    "Task {TaskId}: nessun dato restituito dalla query SQL per il periodo {Start:d}-{End:d}",
                    entity.IdTaskDaEseguire, startDate, endDate);
                return;
            }

            // Converte DataTable → List<DatiLavorazione> verificando le colonne obbligatorie
            var datiLavorazione = DataTableToDatiLavorazione(dataTable);

            // Normalizza operatori e raggruppa via ElaboratoreDatiLavorazione
            var datiElaborati = await elaboratore.ElaboraDatiLavorazioneAsync(
                datiLavorazione,
                mapping.IdCentro,
                idProceduraLavorazione,
                idFaseLavorazione);

            // Persiste i risultati in ProduzioneSistema
            int saved = await PersistProduzioneSistemaAsync(db, datiElaborati, cancellationToken);

            logger.LogInformation(
                "Task {TaskId}: {Saved} record salvati in ProduzioneSistema su {Total} elaborati",
                entity.IdTaskDaEseguire, saved, datiElaborati.Count);
        }



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
            var now = DateTime.UtcNow;
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

