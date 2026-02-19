using AutoMapper;
using AutoMapper.QueryableExtensions;
using BlazorDematReports.Application;
using BlazorDematReports.Dto;
using BlazorDematReports.Interfaces.IDataService;
using BlazorDematReports.Core.DataReading.Dto;
using Entities.Helpers; // Aggiornato: usare helper unificato
using Entities.Models.DbApplication;
using Microsoft.EntityFrameworkCore;

namespace BlazorDematReports.Services.DataService
{
    /// <summary>
    /// Servizio per la gestione delle query associate alle procedure di lavorazione.
    /// Fornisce operazioni CRUD per le query personalizzate utilizzate nei task di elaborazione dati.
    /// </summary>
    public class ServiceQueryProcedureLavorazioni : ServiceBase<QueryProcedureLavorazioni>, IServiceQueryProcedureLavorazioni
    {
        private readonly IMapper mapper;
        private readonly ConfigUser configUser;
        private readonly IDbContextFactory<DematReportsContext> contextFactory;
        private readonly ILogger<ServiceQueryProcedureLavorazioni> logger;

        /// <summary>
        /// Inizializza una nuova istanza del servizio per la gestione delle query delle procedure di lavorazione.
        /// </summary>
        /// <param name="mapper">Mapper per conversioni tra entità e DTO.</param>
        /// <param name="configUser">Configurazione utente per controllo autorizzazioni.</param>
        /// <param name="contextFactory">Factory per la creazione di contesti database.</param>
        /// <param name="lettoreDati">Servizio per l'elaborazione dati e scheduling.</param>
        /// <param name="logger">Logger per registrare operazioni e errori.</param>
        public ServiceQueryProcedureLavorazioni(IMapper mapper, ConfigUser configUser, IDbContextFactory<DematReportsContext> contextFactory, ILogger<ServiceQueryProcedureLavorazioni> logger)
            : base(contextFactory)
        {
            this.mapper = mapper;
            this.configUser = configUser;
            this.contextFactory = contextFactory;
            this.logger = logger;
        }

        /// <inheritdoc/>
        public async Task<List<QueryProcedureLavorazioni>> GetAllQueryProcedureLavorazioniByIdProceduraLavorazioneAsync(int idProceduraLavorazione)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            return await context.QueryProcedureLavorazionis.Where(x => x.IdproceduraLavorazione == idProceduraLavorazione).ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<List<QueryProcedureLavorazioni>> GetAllQueryProcedureLavorazioniAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            return await context.QueryProcedureLavorazionis.Include(x => x.IdproceduraLavorazioneNavigation).ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<List<QueryProcedureLavorazioniDto>> GetAllQueryProcedureLavorazioniDtoAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            if (configUser.IsAdminRole)
            {
                return await context.QueryProcedureLavorazionis
                    .Include(x => x.IdproceduraLavorazioneNavigation)
                    .OrderBy(x => x.IdproceduraLavorazioneNavigation.NomeProcedura)
                    .ProjectTo<QueryProcedureLavorazioniDto>(mapper.ConfigurationProvider)
                    .ToListAsync();
            }
            else
            {
                return await context.QueryProcedureLavorazionis
                    .Include(x => x.IdproceduraLavorazioneNavigation)
                    .ThenInclude(x => x!.IdproceduraClienteNavigation)
                    .ThenInclude(p => p!.IdclienteNavigation)
                    .ThenInclude(p => p!.IdCentroLavorazioneNavigation)
                    .Where(x => x.IdproceduraLavorazioneNavigation!.Idcentro == configUser.IdCentroOrigine)
                    .OrderBy(x => x.IdproceduraLavorazioneNavigation.NomeProcedura)
                    .ProjectTo<QueryProcedureLavorazioniDto>(mapper.ConfigurationProvider)
                    .ToListAsync();
            }
        }

        /// <inheritdoc/>
        public async Task AddQueryProcedureLavorazioniAsync(QueryProcedureLavorazioniDto arg)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            var entity = mapper.Map<QueryProcedureLavorazioni>(arg);
            entity.Titolo = entity.Titolo != null ? entity.Titolo.ToUpper() : entity.Titolo;
            using var context = contextFactory.CreateDbContext();
            context.QueryProcedureLavorazionis.Add(entity);
            await context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task UpdateQueryProcedureLavorazioniAsync(QueryProcedureLavorazioniDto queryProcedureLavorazioniDto)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            var query = await context.QueryProcedureLavorazionis.FirstOrDefaultAsync(c => c.IdQuery.Equals(queryProcedureLavorazioniDto.IdQuery));
            if (query != null)
            {
                query.Titolo = queryProcedureLavorazioniDto.Titolo != null ? queryProcedureLavorazioniDto.Titolo.ToUpper() : queryProcedureLavorazioniDto.Titolo;
                query.Descrizione = queryProcedureLavorazioniDto.Descrizione!;
                query.IdproceduraLavorazione = queryProcedureLavorazioniDto.IdproceduraLavorazione;
                query.Note = queryProcedureLavorazioniDto.Note;
                query.DataCreazioneQuery = DateTime.Now;

                await context.SaveChangesAsync();
            }
        }

        /// <inheritdoc/>
        public async Task DeleteQueryProcedureLavorazioniAsync(int id)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            var entity = await context.QueryProcedureLavorazionis.FindAsync(id);
            if (entity != null)
            {
                context.QueryProcedureLavorazionis.Remove(entity);
                await context.SaveChangesAsync();
            }
        }
    }
}
