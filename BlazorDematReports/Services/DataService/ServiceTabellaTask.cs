using AutoMapper;
using BlazorDematReports.Application;
using BlazorDematReports.Dto;
using BlazorDematReports.Interfaces.IDataService;
using Entities.Helpers;
using Entities.Models.DbApplication;
using Microsoft.EntityFrameworkCore;

namespace BlazorDematReports.Services.DataService
{
    /// <summary>
    /// Servizio per la gestione della tabella dei task e delle relative operazioni sui dati.
    /// </summary>
    public class ServiceTabellaTask : ServiceBase<TabellaTask>, IServiceTabellaTask
    {
        private readonly IMapper mapper;
        private readonly ConfigUser configUser;
        private readonly IDbContextFactory<DematReportsContext> contextFactory;
        private readonly ILogger<ServiceTabellaTask> logger;

        /// <summary>
        /// Costruttore che inizializza le dipendenze necessarie per la gestione della tabella dei task.
        /// </summary>
        /// <param name="mapper">Servizio per la mappatura tra entità e DTO.</param>
        /// <param name="configUser">Configurazione dell'utente corrente.</param>
        /// <param name="contextFactory">Factory per la creazione del contesto dati.</param>
        /// <param name="logger">Logger per il tracking delle operazioni.</param>
        public ServiceTabellaTask(IMapper mapper, ConfigUser configUser, IDbContextFactory<DematReportsContext> contextFactory, ILogger<ServiceTabellaTask> logger) : base(contextFactory)
        {
            this.mapper = mapper;
            this.configUser = configUser;
            this.contextFactory = contextFactory;
            this.logger = logger;
        }

        /// <inheritdoc/>
        public async Task<List<TabellaTask>> GetTabellaTaskAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            return await FindAll().ToListAsync();
        }

        /// <inheritdoc/>
        public async Task AddTaskAsync(TabellaTaskDto tabellaTaskDto)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            TabellaTask TabellaTask = mapper.Map<TabellaTask>(tabellaTaskDto);
            await CreateAsync(TabellaTask);
        }

        /// <inheritdoc/>
        public async Task DeleteTask(int idTask)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            await DeleteAsync(idTask);
        }
    }
}
