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
    /// Servizio per la gestione dei ruoli e delle relative operazioni sui dati.
    /// </summary>
    public class ServiceRuoli : ServiceBase<Ruoli>, IServiceRuoli
    {
        private readonly IMapper mapper;
        private readonly ConfigUser configUser;
        private readonly IDbContextFactory<DematReportsContext> contextFactory;
        private readonly ILogger<ServiceRuoli> logger;

        /// <summary>
        /// Inizializza una nuova istanza del servizio per la gestione dei ruoli.
        /// </summary>
        /// <param name="mapper">Mapper per conversioni tra entità e DTO.</param>
        /// <param name="configUser">Configurazione utente per controllo autorizzazioni.</param>
        /// <param name="contextFactory">Factory per la creazione di contesti database.</param>
        /// <param name="logger">Logger per registrare operazioni e errori.</param>
        public ServiceRuoli(IMapper mapper, ConfigUser configUser, IDbContextFactory<DematReportsContext> contextFactory, ILogger<ServiceRuoli> logger)
            : base(contextFactory)
        {
            this.mapper = mapper;
            this.configUser = configUser;
            this.contextFactory = contextFactory;
            this.logger = logger;
        }

        /// <inheritdoc/>
        public async Task<List<Ruoli>> GetRuoliAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            return await context.Ruolis.ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<List<Ruoli>> GetRuoliByUserAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);
            using var context = contextFactory.CreateDbContext();
            if (configUser.IsAdminRole)
            {
                return await context.Ruolis.ToListAsync();
            }
            else
            {
                return await context.Ruolis.Where(x => !x.Ruolo.Equals("ADMIN")).ToListAsync();
            }
        }

        /// <inheritdoc/>
        public async Task<Ruoli?> GetRuoliByIdAsync(int IdRuolo)
        {
            QueryLoggingHelper.LogQueryExecution(logger);
            using var context = contextFactory.CreateDbContext();
            return await context.Ruolis.FirstOrDefaultAsync(x => x.IdRuolo == IdRuolo);
        }

        /// <inheritdoc/>
        public async Task<List<RuoliDto>> GetRuoliDtoAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            List<Ruoli> lst = await context.Ruolis.ToListAsync();
            var LstRuoli = mapper.Map<List<Ruoli>, List<RuoliDto>>(lst);
            LstRuoli = LstRuoli.OrderBy(x => x.IdRuolo).ToList();
            return LstRuoli;
        }

        /// <inheritdoc/>
        public async Task AddRuoloAsync(RuoliDto ruoliDto)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            var entity = mapper.Map<Ruoli>(ruoliDto);
            using var context = contextFactory.CreateDbContext();
            context.Ruolis.Add(entity);
            await context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task DeleteRuoloAsync(int idRuolo)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            var entity = await context.Ruolis.FindAsync(idRuolo);
            if (entity != null)
            {
                context.Ruolis.Remove(entity);
                await context.SaveChangesAsync();
            }
        }

        /// <inheritdoc/>
        public async Task UpdateRuoloAsync(RuoliDto arg)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            var ruolo = await context.Ruolis.FirstOrDefaultAsync(x => x.IdRuolo.Equals(arg.IdRuolo));
            if (ruolo != null)
            {
                ruolo.Ruolo = arg.Ruolo!;
                await context.SaveChangesAsync();
            }
        }
    }
}
