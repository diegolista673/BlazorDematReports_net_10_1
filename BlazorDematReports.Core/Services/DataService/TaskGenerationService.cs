using BlazorDematReports.Core.Constants;
using BlazorDematReports.Core.DataReading.Infrastructure;
using Entities.Models.DbApplication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlazorDematReports.Core.Services.DataService
{
    /// <summary>
    /// Servizio per la gestione automatica della creazione di TaskDaEseguire da ConfigurazioneFaseCentro
    /// </summary>
    public interface ITaskGenerationService
    {
        /// <summary>
        /// Genera task per una configurazione appena creata/aggiornata
        /// </summary>
        Task<TaskGenerationResult> GenerateTasksForConfigurationAsync(int idConfigurazione);

        /// <summary>
        /// Genera un singolo task per un mapping specifico
        /// </summary>
        Task<bool> GenerateTaskForMappingAsync(int idFaseCentro);
    }

    public class TaskGenerationService : ITaskGenerationService
    {
        private readonly IDbContextFactory<DematReportsContext> _contextFactory;
        private readonly IProductionJobScheduler _scheduler;
        private readonly ILogger<TaskGenerationService> _logger;

        public TaskGenerationService(
            IDbContextFactory<DematReportsContext> contextFactory,
            IProductionJobScheduler scheduler,
            ILogger<TaskGenerationService> logger)
        {
            _contextFactory = contextFactory;
            _scheduler = scheduler;
            _logger = logger;
        }

        public async Task<TaskGenerationResult> GenerateTasksForConfigurationAsync(int idConfigurazione)
        {
            var result = new TaskGenerationResult();

            await using var context = await _contextFactory.CreateDbContextAsync();

            // Carica configurazione con mappings
            var config = await context.ConfigurazioneFontiDatis
                .Include(c => c.ConfigurazioneFaseCentros)
                .FirstOrDefaultAsync(c => c.IdConfigurazione == idConfigurazione);

            if (config == null)
            {
                result.Errors.Add($"Configurazione {idConfigurazione} non trovata");
                _logger.LogError("Configurazione {ConfigId} non trovata", idConfigurazione);
                return result;
            }

            _logger.LogInformation(
                "Generazione task per Configurazione {ConfigId}. Mappings attivi: {Count}",
                idConfigurazione, config.ConfigurazioneFaseCentros.Count(m => m.FlagAttiva));

            // Per ogni mapping attivo, crea un task
            foreach (var mapping in config.ConfigurazioneFaseCentros.Where(m => m.FlagAttiva))
            {
                try
                {
                    // Trova o crea LavorazioneFasiDataReading corrispondente
                    var lavorazioneFase = await context.LavorazioniFasiDataReadings
                        .FirstOrDefaultAsync(lf =>
                            lf.IdProceduraLavorazione == mapping.IdProceduraLavorazione &&
                            lf.IdFaseLavorazione == mapping.IdFaseLavorazione);

                    if (lavorazioneFase == null)
                    {
                        // CREA AUTOMATICAMENTE il record mancante
                        _logger.LogInformation(
                            "Creazione LavorazioneFasiDataReading per Procedura {ProcId}, Fase {FaseId}",
                            mapping.IdProceduraLavorazione, mapping.IdFaseLavorazione);

                        lavorazioneFase = new LavorazioniFasiDataReading
                        {
                            IdProceduraLavorazione = mapping.IdProceduraLavorazione,
                            IdFaseLavorazione = mapping.IdFaseLavorazione,
                            FlagDataReading = true,
                            FlagGraficoDocumenti = false
                        };

                        context.LavorazioniFasiDataReadings.Add(lavorazioneFase);
                        await context.SaveChangesAsync(); // Salva per ottenere l'ID

                        _logger.LogInformation(
                            "LavorazioneFasiDataReading creata con ID {Id}",
                            lavorazioneFase.IdlavorazioneFaseDateReading);
                    }

                    // Verifica se esiste già un task per questo mapping
                    var taskEsistente = await context.TaskDaEseguires
                        .AnyAsync(t =>
                            t.IdConfigurazioneDatabase == idConfigurazione &&
                            t.IdLavorazioneFaseDateReading == lavorazioneFase.IdlavorazioneFaseDateReading);

                    if (taskEsistente)
                    {
                        result.ExistingTasks++;
                        _logger.LogDebug(
                            "Task già esistente per Config {ConfigId}, Mapping {MappingId}",
                            idConfigurazione, mapping.IdFaseCentro);
                        continue;
                    }

                    // Crea nuovo task
                    var nuovoTask = new TaskDaEseguire
                    {
                        IdLavorazioneFaseDateReading = lavorazioneFase.IdlavorazioneFaseDateReading,
                        IdConfigurazioneDatabase = idConfigurazione,
                        Stato = "CONFIGURED",
                        DataStato = DateTime.Now,
                        GiorniPrecedenti = mapping.GiorniPrecedenti > 0
                            ? mapping.GiorniPrecedenti
                            : TaskConfigurationDefaults.DefaultGiorniPrecedenti,
                        CronExpression = mapping.CronExpression ?? TaskConfigurationDefaults.DefaultCronExpression,
                        Enabled = true,
                        IdTaskHangFire = $"temp-{Guid.NewGuid()}"
                    };

                    context.TaskDaEseguires.Add(nuovoTask);
                    await context.SaveChangesAsync();

                    // Registra in Hangfire e genera chiave definitiva
                    try
                    {
                        await _scheduler.AddOrUpdateAsync(nuovoTask.IdTaskDaEseguire);
                        result.CreatedTasks++;

                        _logger.LogInformation(
                            "Task {TaskId} creato per Config {ConfigId}, Mapping {MappingId}",
                            nuovoTask.IdTaskDaEseguire, idConfigurazione, mapping.IdFaseCentro);
                    }
                    catch (Exception schedEx)
                    {
                        result.Errors.Add(
                            $"Errore scheduling task {nuovoTask.IdTaskDaEseguire}: {schedEx.Message}");
                        _logger.LogError(schedEx,
                            "Errore scheduling task {TaskId}", nuovoTask.IdTaskDaEseguire);
                    }
                }
                catch (Exception mappingEx)
                {
                    result.Errors.Add(
                        $"Errore creazione task per mapping {mapping.IdFaseCentro}: {mappingEx.Message}");
                    _logger.LogError(mappingEx,
                        "Errore creazione task per mapping {MappingId}", mapping.IdFaseCentro);
                }
            }

            return result;
        }

        public async Task<bool> GenerateTaskForMappingAsync(int idFaseCentro)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var mapping = await context.ConfigurazioneFaseCentros
                .Include(m => m.IdConfigurazioneNavigation)
                .FirstOrDefaultAsync(m => m.IdFaseCentro == idFaseCentro);

            if (mapping == null)
            {
                _logger.LogWarning("Mapping {MappingId} non trovato", idFaseCentro);
                return false;
            }

            // Trova o crea LavorazioneFasiDataReading
            var lavorazioneFase = await context.LavorazioniFasiDataReadings
                .FirstOrDefaultAsync(lf =>
                    lf.IdProceduraLavorazione == mapping.IdProceduraLavorazione &&
                    lf.IdFaseLavorazione == mapping.IdFaseLavorazione);

            if (lavorazioneFase == null)
            {
                _logger.LogInformation(
                    "Creazione LavorazioneFasiDataReading per Mapping {MappingId}", idFaseCentro);

                lavorazioneFase = new LavorazioniFasiDataReading
                {
                    IdProceduraLavorazione = mapping.IdProceduraLavorazione,
                    IdFaseLavorazione = mapping.IdFaseLavorazione,
                    FlagDataReading = true,
                    FlagGraficoDocumenti = false
                };

                context.LavorazioniFasiDataReadings.Add(lavorazioneFase);
                await context.SaveChangesAsync();

                _logger.LogInformation(
                    "LavorazioneFasiDataReading creata con ID {Id}",
                    lavorazioneFase.IdlavorazioneFaseDateReading);
            }

            // Verifica duplicato
            var exists = await context.TaskDaEseguires
                .AnyAsync(t =>
                    t.IdConfigurazioneDatabase == mapping.IdConfigurazione &&
                    t.IdLavorazioneFaseDateReading == lavorazioneFase.IdlavorazioneFaseDateReading);

            if (exists)
            {
                _logger.LogDebug("Task già esistente per Mapping {MappingId}", idFaseCentro);
                return true; // Non è un errore, il task esiste già
            }

            // Crea task
            var nuovoTask = new TaskDaEseguire
            {
                IdLavorazioneFaseDateReading = lavorazioneFase.IdlavorazioneFaseDateReading,
                IdConfigurazioneDatabase = mapping.IdConfigurazione,
                Stato = "CONFIGURED",
                DataStato = DateTime.Now,
                GiorniPrecedenti = mapping.GiorniPrecedenti > 0 ? mapping.GiorniPrecedenti : 10,
                CronExpression = mapping.CronExpression ?? "0 5 * * *",
                Enabled = true, //  SEMPRE TRUE per nuovi task generati
                IdTaskHangFire = $"temp-{Guid.NewGuid()}"
            };

            context.TaskDaEseguires.Add(nuovoTask);
            await context.SaveChangesAsync();

            // Registra in Hangfire
            await _scheduler.AddOrUpdateAsync(nuovoTask.IdTaskDaEseguire);

            _logger.LogInformation(
                "Task {TaskId} creato per Mapping {MappingId}",
                nuovoTask.IdTaskDaEseguire, idFaseCentro);

            return true;
        }
    }

    /// <summary>
    /// Risultato dell'operazione di generazione task
    /// </summary>
    public class TaskGenerationResult
    {
        public int CreatedTasks { get; set; }
        public int ExistingTasks { get; set; }
        public List<string> Errors { get; set; } = new();

        public bool HasErrors => Errors.Any();
        public bool Success => CreatedTasks > 0 && !HasErrors;

        public string GetSummary()
        {
            var parts = new List<string>();

            if (CreatedTasks > 0)
                parts.Add($"{CreatedTasks} task creati");

            if (ExistingTasks > 0)
                parts.Add($"{ExistingTasks} già esistenti");

            if (HasErrors)
                parts.Add($"{Errors.Count} errori");

            return string.Join(", ", parts);
        }
    }
}
