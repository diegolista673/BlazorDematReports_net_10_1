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
    /// Servizio per la gestione dei tipi di turno.
    /// Fornisce operazioni per gestire i diversi tipi di turno lavorativo.
    /// </summary>
    public class ServiceTipoTurni : ServiceBase<TipoTurni>, IServiceTipoTurni
    {
        private readonly IMapper mapper;
        private readonly ConfigUser configUser;
        private readonly IDbContextFactory<DematReportsContext> contextFactory;
        private readonly ILogger<ServiceTipoTurni> logger;

        /// <summary>
        /// Inizializza una nuova istanza del servizio per la gestione dei tipi di turno.
        /// </summary>
        /// <param name="mapper">Mapper per conversioni tra entità e DTO.</param>
        /// <param name="configUser">Configurazione utente per controllo autorizzazioni.</param>
        /// <param name="contextFactory">Factory per la creazione di contesti database.</param>
        /// <param name="logger">Logger per registrare operazioni e errori.</param>
        public ServiceTipoTurni(IMapper mapper, ConfigUser configUser, IDbContextFactory<DematReportsContext> contextFactory, ILogger<ServiceTipoTurni> logger) : base(contextFactory)
        {
            this.mapper = mapper;
            this.configUser = configUser;
            this.contextFactory = contextFactory;
            this.logger = logger;
        }

        /// <inheritdoc/>
        public async Task<List<TipoTurni>> GetTipoTurniAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            return await FindAll().ToListAsync();
        }
    }

}
