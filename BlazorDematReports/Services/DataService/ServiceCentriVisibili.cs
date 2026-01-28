using AutoMapper;
using BlazorDematReports.Application;
using BlazorDematReports.Interfaces.IDataService;
using Entities.Helpers; // Aggiornato: usare helper unificato
using Entities.Models.DbApplication;
using Microsoft.EntityFrameworkCore;

namespace BlazorDematReports.Services.DataService
{
    /// <summary>
    /// Servizio per la gestione dei centri visibili e delle relative operazioni sui dati.
    /// </summary>
    public class ServiceCentriVisibili : ServiceBase<CentriVisibili>, IServiceCentriVisibili
    {
        private readonly IMapper mapper;
        private readonly ConfigUser configUser;
        private readonly IDbContextFactory<DematReportsContext> contextFactory;
        private readonly ILogger<ServiceCentriVisibili> logger;

        /// <summary>
        /// Inizializza una nuova istanza del servizio per la gestione dei centri visibili.
        /// </summary>
        /// <param name="mapper">Mapper per conversioni tra entità e DTO.</param>
        /// <param name="configUser">Configurazione utente per controllo autorizzazioni.</param>
        /// <param name="contextFactory">Factory per la creazione di contesti database.</param>
        /// <param name="logger">Logger per registrare operazioni e errori.</param>
        public ServiceCentriVisibili(IMapper mapper, ConfigUser configUser, IDbContextFactory<DematReportsContext> contextFactory, ILogger<ServiceCentriVisibili> logger)
            : base(contextFactory)
        {
            this.mapper = mapper;
            this.configUser = configUser;
            this.contextFactory = contextFactory;
            this.logger = logger;
        }

        /// <inheritdoc/>
        public async Task<List<CentriVisibili>> GetCentriForShowDataAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            return await context.CentriVisibilis
                    .Include(x => x.IdCentroNavigation)
                    .Include(x => x.IdOperatoreNavigation)
                    .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task Fill()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            var ListOperatori = await context.Operatoris.ToListAsync();
            for (var i = 1; i <= 4; i++)
            {
                foreach (var opi in ListOperatori)
                {
                    CentriVisibili centriVisibili = new CentriVisibili();
                    centriVisibili.IdOperatore = opi.Idoperatore;
                    centriVisibili.IdCentro = i;
                    centriVisibili.FlagVisibile = (opi.Idcentro == i);
                    context.CentriVisibilis.Add(centriVisibili);
                }
            }
            await context.SaveChangesAsync();
        }
    }
}
