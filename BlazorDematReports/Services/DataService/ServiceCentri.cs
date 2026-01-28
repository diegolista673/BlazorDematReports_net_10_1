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
    /// Servizio per la gestione dei centri di lavorazione e delle relative operazioni sui dati.
    /// </summary>
    public class ServiceCentri : ServiceBase<CentriLavorazione>, IServiceCentri
    {
        private readonly IMapper mapper;

        /// <summary>
        /// Inizializza una nuova istanza della classe <see cref="ServiceCentri"/>.
        /// </summary>
        /// <param name="mapper">Mapper per la conversione tra entità e DTO.</param>
        /// <param name="configUser">Configurazione utente corrente.</param>
        /// <param name="contextFactory">Factory per la creazione del contesto dati.</param>
        /// <param name="logger">Logger per la registrazione delle operazioni.</param>
        public ServiceCentri(IMapper mapper, ConfigUser configUser, IDbContextFactory<DematReportsContext> contextFactory, ILogger<ServiceCentri> logger)
            : base(contextFactory)
        {
            this.mapper = mapper;
            this.configUser = configUser;
            this.contextFactory = contextFactory;
            this.logger = logger;
        }
        private readonly ConfigUser configUser;
        private readonly IDbContextFactory<DematReportsContext> contextFactory;
        private readonly ILogger<ServiceCentri> logger;


        /// <summary>
        /// Restituisce i centri di lavorazione accessibili all'utente corrente in base al suo ruolo.
        /// </summary>
        /// <returns>Lista dei centri di lavorazione filtrati per l'utente corrente.</returns>
        public async Task<List<CentriLavorazione>> GetCentriByUserAsync()
        {

            using var context = contextFactory.CreateDbContext();
            List<CentriLavorazione> result;

            if (configUser.IsAdminRole)
            {
                result = await context.CentriLavoraziones.Where(x => x.Centro != "Non Riconosciuto").ToListAsync();
            }
            else
            {
                result = await context.CentriLavoraziones.Where(x => x.Idcentro == configUser.IdCentroOrigine).ToListAsync();
            }


            return result;
        }

        /// <summary>
        /// Restituisce i centri visibili come DTO filtrati per l'utente corrente con ordinamento per nome centro.
        /// </summary>
        /// <returns>Lista di DTO dei centri visibili all'utente corrente.</returns>
        public async Task<List<CentriVisibiliDto>> GetCentriVisibiliDtoByUserAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            List<CentriLavorazione> lstCentri;
            if (configUser.IsAdminRole)
            {
                lstCentri = await context.CentriLavoraziones.Where(x => x.Centro != "Non Riconosciuto").OrderBy(x => x.Centro).ToListAsync();
            }
            else
            {
                lstCentri = await context.CentriLavoraziones.Where(x => x.Idcentro == configUser.IdCentroOrigine).ToListAsync();
            }
            List<CentriVisibiliDto> lstCentriVisibiliDto = mapper.Map<List<CentriVisibiliDto>>(lstCentri);

            return lstCentriVisibiliDto;
        }

        /// <summary>
        /// Aggiunge un nuovo centro di lavorazione al sistema.
        /// </summary>
        /// <param name="CentriLavorazioneDto">DTO del centro di lavorazione da aggiungere.</param>
        /// <returns>Task asincrono per l'operazione di inserimento.</returns>
        public async Task AddCentro(CentriLavorazioneDto CentriLavorazioneDto)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            var entity = mapper.Map<CentriLavorazione>(CentriLavorazioneDto);
            using var context = contextFactory.CreateDbContext();
            context.CentriLavoraziones.Add(entity);
            await context.SaveChangesAsync();
        }
    }
}