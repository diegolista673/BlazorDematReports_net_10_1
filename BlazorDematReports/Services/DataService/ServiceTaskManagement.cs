using BlazorDematReports.Core.Constants;
using BlazorDematReports.Dto;
using BlazorDematReports.Interfaces.IDataService;
using BlazorDematReports.Services.DataService.Queries;
using Entities.Converters;
using Entities.Enums;
using Entities.Models.DbApplication;
using Microsoft.EntityFrameworkCore;
using Hangfire;

namespace BlazorDematReports.Services.DataService
{
    public class ServiceTaskManagement : IServiceTaskManagement
    {
        private readonly IDbContextFactory<DematReportsContext> _contextFactory;
        private readonly ILogger<ServiceTaskManagement> _logger;

        public ServiceTaskManagement(
            IDbContextFactory<DematReportsContext> contextFactory,
            ILogger<ServiceTaskManagement> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        /// <summary>
        /// Recupera configurazione con tutti i mapping e task associati.
        /// Versione ottimizzata con projection diretta a DTO.
        /// </summary>
        public async Task<ConfigurazioneTaskDetailDto?> GetConfigurazioneWithTasksAsync(int idConfigurazione)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Query ottimizzata con projection
            var configDto = await context.ConfigurazioneFontiDatis
                .GetConfigurazioneWithMappings()
                .FirstOrDefaultAsync(c => c.IdConfigurazione == idConfigurazione);

            if (configDto == null) return null;

            var result = new ConfigurazioneTaskDetailDto
            {
                IdConfigurazione = configDto.IdConfigurazione,
                CodiceConfigurazione = configDto.CodiceConfigurazione,
                TipoFonte = configDto.TipoFonte,
                TestoQueryPrincipale = null
            };

            // Carica task per ogni mapping (query separata ottimizzata)
            foreach (var mapping in configDto.Mappings)
            {
                var mappingDto = new MappingConTaskDto
                {
                    IdFaseCentro = mapping.IdFaseCentro,
                    NomeProcedura = mapping.NomeProcedura,
                    NomeFase = mapping.NomeFase,
                    NomeCentro = mapping.NomeCentro,
                    FlagAttiva = mapping.FlagAttiva,
                    TestoQueryOverride = mapping.TestoQueryTask
                };

                // Query task ottimizzata (solo campi necessari)
                var tasks = await context.TaskDaEseguires
                    .Where(t => t.IdLavorazioneFaseDateReading == mapping.IdFaseCentro)
                    .Select(t => new TaskDto
                    {
                        IdTaskDaEseguire = t.IdTaskDaEseguire,
                        IdTaskHangFire = t.IdTaskHangFire,
                        Stato = t.Stato,
                        Enabled = t.Enabled,
                        CronExpression = t.CronExpression,
                        LastRunUtc = t.LastRunUtc,
                        LastError = t.LastError,
                        ConsecutiveFailures = t.ConsecutiveFailures
                    })
                    .ToListAsync();

                mappingDto.Tasks = tasks;
                result.Mappings.Add(mappingDto);
            }

            _logger.LogDebug(
                "Configurazione {Id} caricata con {Mappings} mappings e {Tasks} task totali",
                idConfigurazione,
                result.Mappings.Count,
                result.TotaleTasks
            );

            return result;
        }

        /// <summary>
        /// Attiva/Disattiva un singolo task
        /// </summary>
        public async Task<bool> ToggleTaskAsync(int idTask, bool enabled)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var task = await context.TaskDaEseguires
                .FirstOrDefaultAsync(t => t.IdTaskDaEseguire == idTask);

            if (task == null)
            {
                _logger.LogWarning("Task {IdTask} non trovato", idTask);
                return false;
            }

            task.Enabled = enabled;
            await context.SaveChangesAsync();

            _logger.LogInformation(
                "Task {IdTask} ({JobId}) {Action}",
                idTask,
                task.IdTaskHangFire,
                enabled ? "abilitato" : "disabilitato");

            return true;
        }

        /// <summary>
        /// Attiva/Disattiva tutti i task di un mapping
        /// </summary>
        public async Task<bool> ToggleMappingTasksAsync(int idFaseCentro, bool enabled)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var tasks = await context.TaskDaEseguires
                .Where(t => t.IdLavorazioneFaseDateReading == idFaseCentro)
                .ToListAsync();

            if (!tasks.Any())
            {
                _logger.LogWarning("Nessun task trovato per mapping {IdFaseCentro}", idFaseCentro);
                return false;
            }

            foreach (var task in tasks)
            {
                task.Enabled = enabled;
            }

            await context.SaveChangesAsync();

            _logger.LogInformation(
                "{Count} task del mapping {IdFaseCentro} {Action}",
                tasks.Count,
                idFaseCentro,
                enabled ? "abilitati" : "disabilitati");

            return true;
        }

        /// <summary>
        /// Elimina un task specifico sia dal database che da Hangfire
        /// </summary>
        public async Task<bool> DeleteTaskAsync(int idTask)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var task = await context.TaskDaEseguires
                .FirstOrDefaultAsync(t => t.IdTaskDaEseguire == idTask);

            if (task == null)
            {
                _logger.LogWarning("Task {IdTask} non trovato per eliminazione", idTask);
                return false;
            }

            var hangfireJobId = task.IdTaskHangFire;

            // 1. Rimuovi dal database
            context.TaskDaEseguires.Remove(task);
            await context.SaveChangesAsync();

            // 2. Rimuovi il recurring job da Hangfire
            try
            {
                RecurringJob.RemoveIfExists(hangfireJobId);
                _logger.LogInformation(
                    "Task {IdTask} eliminato con successo (DB + Hangfire Job: {JobId})",
                    idTask,
                    hangfireJobId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Task {IdTask} eliminato dal DB, ma errore nella rimozione da Hangfire (Job: {JobId})",
                    idTask,
                    hangfireJobId);
            }

            return true;
        }

        /// <summary>
        /// Restituisce la query attiva per un mapping (override o principale)
        /// </summary>
        public async Task<string> GetActiveQueryForMappingAsync(int idFaseCentro)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var mapping = await context.ConfigurazioneFaseCentros
                .Include(fc => fc.IdConfigurazioneNavigation)
                .FirstOrDefaultAsync(fc => fc.IdFaseCentro == idFaseCentro);

            if (mapping == null)
                return string.Empty;

            // Usa TestoQueryTask dal mapping
            return mapping.TestoQueryTask ?? string.Empty;
        }

        /// <summary>
        /// Recupera un task per la modifica nell'UI
        /// </summary>
        public async Task<ConfigurazioneTaskEditDto?> GetTaskForEditAsync(int idFaseCentro)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var mapping = await context.ConfigurazioneFaseCentros
                .Include(fc => fc.IdFaseLavorazioneNavigation)
                .Include(fc => fc.IdProceduraLavorazioneNavigation)
                .Include(fc => fc.IdCentroNavigation)
                .Include(fc => fc.IdConfigurazioneNavigation)
                .FirstOrDefaultAsync(fc => fc.IdFaseCentro == idFaseCentro);

            if (mapping == null) return null;

            return new ConfigurazioneTaskEditDto
            {
                IdFaseCentro = mapping.IdFaseCentro,
                IdConfigurazione = mapping.IdConfigurazione,
                IdProceduraLavorazione = mapping.IdProceduraLavorazione,
                IdFaseLavorazione = mapping.IdFaseLavorazione,
                IdCentro = mapping.IdCentro,
                TipoTask = mapping.IdConfigurazioneNavigation?.TipoFonte ?? TipoFonteData.SQL,
                CronExpression = mapping.CronExpression ?? TaskConfigurationDefaults.DefaultCronExpression,
                TestoQueryTask = mapping.TestoQueryTask,
                HandlerClassName = mapping.HandlerClassName ?? mapping.IdConfigurazioneNavigation?.HandlerClassName,
                NomeProcedura = mapping.IdProceduraLavorazioneNavigation?.NomeProcedura ?? "N/A",
                NomeFase = mapping.IdFaseLavorazioneNavigation?.FaseLavorazione ?? "N/A",
                NomeCentro = mapping.IdCentroNavigation?.Centro ?? "N/A"
            };
        }

        /// <summary>
        /// Aggiorna configurazione di un task esistente
        /// </summary>
        public async Task<bool> UpdateTaskConfigurationAsync(ConfigurazioneTaskEditDto taskDto)
        {
            if (!taskDto.IsValid())
            {
                _logger.LogWarning("Task configuration non valida per IdFaseCentro {Id}: {Desc}", 
                    taskDto.IdFaseCentro, taskDto.GetDescription());
                return false;
            }

            await using var context = await _contextFactory.CreateDbContextAsync();
            await using var tx = await context.Database.BeginTransactionAsync();

            try
            {
                var mapping = await context.ConfigurazioneFaseCentros
                    .FirstOrDefaultAsync(fc => fc.IdFaseCentro == taskDto.IdFaseCentro);

                if (mapping == null)
                {
                    _logger.LogWarning("Mapping {Id} non trovato", taskDto.IdFaseCentro);
                    return false;
                }

                // Validazione duplicati (Fase + Cron)
                var isUnique = await ValidateUniqueTaskAsync(
                    taskDto.IdConfigurazione, 
                    taskDto.IdFaseLavorazione, 
                    taskDto.CronExpression, 
                    taskDto.IdFaseCentro);

                if (!isUnique)
                {
                    _logger.LogWarning(
                        "Task duplicato rilevato: Config={Config} Fase={Fase} Cron={Cron}", 
                        taskDto.IdConfigurazione, 
                        taskDto.IdFaseLavorazione, 
                        taskDto.CronExpression);
                    return false;
                }

                // Aggiorna mapping con nuova configurazione
                mapping.TipoTask = taskDto.TipoTask.ToString(); // ✅ Conversione semplice a string
                mapping.CronExpression = taskDto.CronExpression;
                mapping.TestoQueryTask = taskDto.TestoQueryTask;
                mapping.HandlerClassName = taskDto.HandlerClassName;

                await context.SaveChangesAsync();

                // Aggiorna il recurring job in Hangfire se esiste
                var hangfireTask = await context.TaskDaEseguires
                    .FirstOrDefaultAsync(t => t.IdLavorazioneFaseDateReading == taskDto.IdFaseCentro);

                if (hangfireTask != null)
                {
                    hangfireTask.CronExpression = taskDto.CronExpression;
                    await context.SaveChangesAsync();

                    // Aggiorna o ricrea il recurring job in Hangfire
                    // Nota: Il metodo ExecuteTask sarà implementato dal sistema di esecuzione task
                    RecurringJob.AddOrUpdate(
                        hangfireTask.IdTaskHangFire,
                        () => Console.WriteLine($"Task {taskDto.IdFaseCentro} execution placeholder"),
                        taskDto.CronExpression);
                    
                    _logger.LogInformation(
                        "Recurring job Hangfire aggiornato: JobId={JobId} CRON={Cron}",
                        hangfireTask.IdTaskHangFire,
                        taskDto.CronExpression);
                }

                await tx.CommitAsync();

                _logger.LogInformation(
                    "Task {Id} aggiornato: {Desc}",
                    taskDto.IdFaseCentro,
                    taskDto.GetDescription());

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore aggiornamento task {Id}", taskDto.IdFaseCentro);
                await tx.RollbackAsync();
                return false;
            }
        }

        /// <summary>
        /// Valida che non esista un task duplicato (stessa Fase + stesso CRON)
        /// </summary>
        public async Task<bool> ValidateUniqueTaskAsync(
            int idConfigurazione, 
            int idFase, 
            string cron, 
            int? excludeIdFaseCentro = null)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var query = context.ConfigurazioneFaseCentros
                .Where(fc => 
                    fc.IdConfigurazione == idConfigurazione &&
                    fc.IdFaseLavorazione == idFase &&
                    fc.CronExpression == cron);

            // Esclude il task corrente se stiamo modificando (non creando)
            if (excludeIdFaseCentro.HasValue)
            {
                query = query.Where(fc => fc.IdFaseCentro != excludeIdFaseCentro.Value);
            }

            var exists = await query.AnyAsync();
            return !exists; // True se NON esiste duplicato (è valido)
        }
    }
}

