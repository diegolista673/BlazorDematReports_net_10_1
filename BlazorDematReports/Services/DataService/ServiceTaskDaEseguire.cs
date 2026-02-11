using AutoMapper;
using BlazorDematReports.Application;
using BlazorDematReports.Interfaces.IDataService;
using Entities.Helpers;
using Entities.Models.DbApplication;
using Microsoft.EntityFrameworkCore;

namespace BlazorDematReports.Services.DataService
{
    /// <summary>
    /// Servizio per la gestione dei task da eseguire e delle relative operazioni sui dati.
    /// </summary>
    public class ServiceTaskDaEseguire : ServiceBase<TaskDaEseguire>, IServiceTaskDaEseguire
    {
        private readonly IMapper mapper;
        private readonly ConfigUser configUser;
        private readonly IDbContextFactory<DematReportsContext> contextFactory;
        private readonly ILogger<ServiceTaskDaEseguire> logger;

        /// <summary>
        /// Costruttore che inizializza le dipendenze necessarie per la gestione dei task da eseguire.
        /// </summary>
        /// <param name="mapper">Servizio per la mappatura tra entità e DTO.</param>
        /// <param name="configUser">Configurazione dell'utente corrente.</param>
        /// <param name="contextFactory">Factory per la creazione del contesto dati.</param>
        /// <param name="logger">Logger per il tracking delle operazioni.</param>
        public ServiceTaskDaEseguire(IMapper mapper, ConfigUser configUser, IDbContextFactory<DematReportsContext> contextFactory, ILogger<ServiceTaskDaEseguire> logger) : base(contextFactory)
        {
            this.mapper = mapper;
            this.configUser = configUser;
            this.contextFactory = contextFactory;
            this.logger = logger;
        }

        /// <summary>
        /// Restituisce l'elenco completo dei task da eseguire con tutte le relazioni caricate.
        /// </summary>
        /// <returns>Lista di task da eseguire con navigazione verso task, fasi e procedure lavorazione.</returns>
        public async Task<List<TaskDaEseguire>> GetTabellaTaskDaEseguireAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            return await FindAll().Include(x => x.IdLavorazioneFaseDateReadingNavigation)!.ThenInclude(f => f.IdFaseLavorazioneNavigation)
                                  .Include(x => x.IdLavorazioneFaseDateReadingNavigation)!.ThenInclude(f => f.IdProceduraLavorazioneNavigation)
                                  .AsNoTracking().ToListAsync();
        }

        /// <summary>
        /// Restituisce l'elenco dei task da eseguire filtrati per ID della procedura di lavorazione.
        /// </summary>
        /// <param name="idProceduraLavorazione">ID della procedura di lavorazione per il filtro.</param>
        /// <returns>Lista di task da eseguire filtrati per la procedura specificata con tutte le relazioni caricate.</returns>
        public async Task<List<TaskDaEseguire>> GetTabellaTaskDaEseguireAsync(int idProceduraLavorazione)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            return await FindAll()
                .Include(x => x.IdLavorazioneFaseDateReadingNavigation)!
                .ThenInclude(f => f.IdFaseLavorazioneNavigation)
                .Include(x => x.IdLavorazioneFaseDateReadingNavigation)!
                .ThenInclude(f => f.IdProceduraLavorazioneNavigation)
                .Where(x => x.IdLavorazioneFaseDateReadingNavigation.IdProceduraLavorazione == idProceduraLavorazione)
                .AsNoTracking().ToListAsync();
        }

        /// <summary>
        /// Restituisce l'elenco dei task da eseguire con configurazione EmailCSV.
        /// </summary>
        /// <returns>Lista di task da eseguire per l'importazione tramite mail.</returns>
        public async Task<List<TaskDaEseguire>> GetMailImportTasksAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            return await FindAll()
                .Include(t => t.IdConfigurazioneDatabaseNavigation)
                .Include(t => t.IdLavorazioneFaseDateReadingNavigation)!.ThenInclude(f => f.IdProceduraLavorazioneNavigation)
                .Where(t => t.IdConfigurazioneDatabase.HasValue && 
                           t.IdConfigurazioneDatabaseNavigation!.TipoFonte == "EmailCSV")
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Restituisce l'elenco dei task da eseguire filtrati per ID della procedura di lavorazione e con configurazione EmailCSV.
        /// </summary>
        /// <param name="idProceduraLavorazione">ID della procedura di lavorazione per il filtro.</param>
        /// <returns>Lista di task da eseguire filtrati per la procedura specificata e pronti per l'importazione tramite mail.</returns>
        public async Task<List<TaskDaEseguire>> GetMailImportTasksAsync(int idProceduraLavorazione)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            return await FindAll()
                .Include(t => t.IdConfigurazioneDatabaseNavigation)
                .Include(t => t.IdLavorazioneFaseDateReadingNavigation)!.ThenInclude(f => f.IdProceduraLavorazioneNavigation)
                .Where(t => t.IdConfigurazioneDatabase.HasValue && 
                           t.IdConfigurazioneDatabaseNavigation!.TipoFonte == "EmailCSV" &&
                           t.IdLavorazioneFaseDateReadingNavigation.IdProceduraLavorazione == idProceduraLavorazione)
                .AsNoTracking()
                .ToListAsync();
        }


        /// <summary>
        /// Ottiene tutti i task mail (EmailCSV) configurati nel sistema.
        /// </summary>
        /// <returns>Lista di task mail.</returns>
        public async Task<List<TaskDaEseguire>> GetMailJobsAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            return await FindAll()
                .Include(t => t.IdConfigurazioneDatabaseNavigation)
                .Include(x => x.IdLavorazioneFaseDateReadingNavigation)!.ThenInclude(f => f.IdFaseLavorazioneNavigation)
                .Include(x => x.IdLavorazioneFaseDateReadingNavigation)!.ThenInclude(f => f.IdProceduraLavorazioneNavigation)
                .Where(t => t.IdConfigurazioneDatabase.HasValue && 
                           t.IdConfigurazioneDatabaseNavigation!.TipoFonte == "EmailCSV")
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Salva o aggiorna un task mail.
        /// </summary>
        /// <param name="task">Task da salvare o aggiornare.</param>
        /// <returns>Task salvato/aggiornato.</returns>
        public async Task<TaskDaEseguire> SaveOrUpdateMailJobAsync(TaskDaEseguire task)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var ctx = contextFactory.CreateDbContext();

            if (task.IdTaskDaEseguire == 0)
            {
                // Nuovo task
                ctx.TaskDaEseguires.Add(task);
            }
            else
            {
                // Aggiornamento task esistente
                ctx.TaskDaEseguires.Update(task);
            }

            await ctx.SaveChangesAsync();
            return task;
        }

        /// <summary>
        /// Elimina un task specifico.
        /// </summary>
        /// <param name="taskId">ID del task da eliminare.</param>
        /// <returns>Task asincrono.</returns>
        public async Task DeleteTaskAsync(int taskId)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var ctx = contextFactory.CreateDbContext();
            var task = await ctx.TaskDaEseguires.FindAsync(taskId);
            
            if (task != null)
            {
                ctx.TaskDaEseguires.Remove(task);
                await ctx.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Disabilita tutti i task del sistema.
        /// </summary>
        /// <returns>Numero di task disabilitati.</returns>
        public async Task<int> DisableAllTasksAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            await using var ctx = await contextFactory.CreateDbContextAsync();
            var tasks = await ctx.TaskDaEseguires.Where(t => t.Enabled).ToListAsync();

            foreach (var task in tasks)
            {
                task.Enabled = false;
            }

            await ctx.SaveChangesAsync();
            logger.LogInformation("Disabilitati {Count} task", tasks.Count);
            return tasks.Count;
        }

        /// <summary>
        /// Abilita tutti i task del sistema.
        /// </summary>
        /// <returns>Numero di task abilitati.</returns>
        public async Task<int> EnableAllTasksAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            await using var ctx = await contextFactory.CreateDbContextAsync();
            var tasks = await ctx.TaskDaEseguires.Where(t => !t.Enabled).ToListAsync();

            foreach (var task in tasks)
            {
                task.Enabled = true;
            }

            await ctx.SaveChangesAsync();
            logger.LogInformation("Abilitati {Count} task", tasks.Count);
            return tasks.Count;
        }
    }
}
