using AutoMapper;
using BlazorDematReports.Core.Application.Dto;
using BlazorDematReports.Core.Interfaces.IDataService;
using Entities.Helpers;
using Entities.Models.DbApplication;
using Microsoft.EntityFrameworkCore;


namespace BlazorDematReports.Core.Services.DataService
{
    /// <summary>
    /// Servizio per la gestione dei tipi di turno.
    /// Fornisce operazioni per gestire i diversi tipi di turno lavorativo.
    /// </summary>
    public class ServiceTipoTurni : ServiceBase<TipoTurni>, IServiceTipoTurni
    {

        /// <summary>
        /// Inizializza una nuova istanza del servizio per la gestione dei tipi di turno.
        /// </summary>
        /// <param name="mapper">Mapper per conversioni tra entit� e DTO.</param>
        /// <param name="configUser">Configurazione utente per controllo autorizzazioni.</param>
        /// <param name="contextFactory">Factory per la creazione di contesti database.</param>
        /// <param name="logger">Logger per registrare operazioni e errori.</param>
        public ServiceTipoTurni(IMapper mapper, ConfigUser configUser, IDbContextFactory<DematReportsContext> contextFactory, ILogger<ServiceTipoTurni> logger) : base(contextFactory, logger, mapper, configUser)
        {
        }

        /// <inheritdoc/>
        public async Task<List<TipoTurni>> GetTipoTurniAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            return await FindAll().ToListAsync();
        }
    }

}
