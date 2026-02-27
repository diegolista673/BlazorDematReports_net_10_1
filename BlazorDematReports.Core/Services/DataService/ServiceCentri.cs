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
    /// Servizio per la gestione dei centri di lavorazione.
    /// </summary>
    public class ServiceCentri : ServiceBase<CentriLavorazione>, IServiceCentri
    {
        private readonly CentriMapper _mapper;

        /// <summary>
        /// Inizializza una nuova istanza di <see cref="ServiceCentri"/>.
        /// </summary>
        /// <param name="mapper">Mapper Mapperly per CentriLavorazione ? DTO.</param>
        /// <param name="configUser">Configurazione utente corrente.</param>
        /// <param name="contextFactory">Factory per la creazione del contesto dati.</param>
        /// <param name="logger">Logger per la registrazione delle operazioni.</param>
        public ServiceCentri(CentriMapper mapper, ConfigUser configUser, IDbContextFactory<DematReportsContext> contextFactory, ILogger<ServiceCentri> logger)
            : base(contextFactory, logger, configUser)
        {
            _mapper = mapper;
        }



        /// <summary>
        /// Restituisce i centri di lavorazione accessibili all'utente corrente in base al suo ruolo.
        /// </summary>
        /// <returns>Lista dei centri di lavorazione filtrati per l'utente corrente.</returns>
        public async Task<List<CentriLavorazione>> GetCentriByUserAsync()
        {

            await using var context = await contextFactory.CreateDbContextAsync();
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

            await using var context = await contextFactory.CreateDbContextAsync();
            List<CentriLavorazione> lstCentri;
            if (configUser.IsAdminRole)
            {
                lstCentri = await context.CentriLavoraziones.Where(x => x.Centro != "Non Riconosciuto").OrderBy(x => x.Centro).ToListAsync();
            }
            else
            {
                lstCentri = await context.CentriLavoraziones.Where(x => x.Idcentro == configUser.IdCentroOrigine).ToListAsync();
            }
            List<CentriVisibiliDto> lstCentriVisibiliDto = lstCentri.Select(_mapper.CentroToCentroVisibileDto).ToList();

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

            var entity = _mapper.DtoToCentro(CentriLavorazioneDto);
            await using var context = await contextFactory.CreateDbContextAsync();
            context.CentriLavoraziones.Add(entity);
            await context.SaveChangesAsync();
        }
    }
}
