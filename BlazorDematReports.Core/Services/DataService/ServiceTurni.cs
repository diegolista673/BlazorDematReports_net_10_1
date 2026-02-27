using BlazorDematReports.Core.Application;
using BlazorDematReports.Core.Application.Dto;
using BlazorDematReports.Core.Application.Mapping;
using BlazorDematReports.Core.Services.Interfaces.IDataService;
using Entities.Helpers;
using Entities.Models.DbApplication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlazorDematReports.Core.Services.DataService
{
    /// <summary>
    /// Servizio per la gestione dei turni e delle relative operazioni sui dati.
    /// </summary>
    public class ServiceTurni : ServiceBase<Turni>, IServiceTurni
    {
        private readonly TurniMapper _mapper;

        /// <summary>
        /// Inizializza una nuova istanza di <see cref="ServiceTurni"/>.
        /// </summary>
        /// <param name="mapper">Mapper Mapperly per conversioni Turni ↔ DTO.</param>
        /// <param name="configUser">Configurazione dell'utente corrente.</param>
        /// <param name="contextFactory">Factory per la creazione del contesto dati.</param>
        /// <param name="logger">Logger per il tracking delle operazioni.</param>
        public ServiceTurni(TurniMapper mapper, ConfigUser configUser, IDbContextFactory<DematReportsContext> contextFactory, ILogger<ServiceTurni> logger)
            : base(contextFactory, logger, configUser)
        {
            _mapper = mapper;
        }

        /// <summary>
        /// Restituisce la lista completa di tutti i turni disponibili nel sistema.
        /// </summary>
        public async Task<List<Turni>> GetTurniAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            return (await FindAllAsync()).ToList();
        }

        /// <summary>
        /// Aggiunge un nuovo turno al sistema tramite DTO.
        /// </summary>
        public async Task AddTurnoAsync(TurniDto turniDto)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            Turni turni = _mapper.DtoToTurno(turniDto);
            await CreateAsync(turni);
        }

        /// <summary>
        /// Elimina un turno tramite il suo identificativo.
        /// </summary>
        public async Task DeleteTurnoAsync(int idTurno)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            await DeleteAsync(idTurno);
        }
    }
}
