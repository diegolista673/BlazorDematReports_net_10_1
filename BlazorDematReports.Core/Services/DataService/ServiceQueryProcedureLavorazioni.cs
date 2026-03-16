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
    /// Servizio per la gestione delle query associate alle procedure di lavorazione.
    /// </summary>
    public class ServiceQueryProcedureLavorazioni : ServiceBase<QueryProcedureLavorazioni>, IServiceQueryProcedureLavorazioni
    {
        private readonly QueryProcedureLavorazioniMapper _mapper;

        /// <summary>
        /// Inizializza una nuova istanza del servizio.
        /// </summary>
        /// <param name="mapper">Mapper Mapperly per QueryProcedureLavorazioni ↔ DTO.</param>
        /// <param name="configUser">Configurazione utente per controllo autorizzazioni.</param>
        /// <param name="contextFactory">Factory per la creazione di contesti database.</param>
        /// <param name="logger">Logger per registrare operazioni e errori.</param>
        public ServiceQueryProcedureLavorazioni(QueryProcedureLavorazioniMapper mapper, ConfigUser configUser, IDbContextFactory<DematReportsContext> contextFactory, ILogger<ServiceQueryProcedureLavorazioni> logger)
            : base(contextFactory, logger, configUser)
        {
            _mapper = mapper;
        }

        /// <inheritdoc/>
        public async Task<List<QueryProcedureLavorazioni>> GetAllQueryProcedureLavorazioniByIdProceduraLavorazioneAsync(int idProceduraLavorazione)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            await using var context = await contextFactory.CreateDbContextAsync();
            return await context.QueryProcedureLavorazionis.Where(x => x.IdproceduraLavorazione == idProceduraLavorazione).ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<List<QueryProcedureLavorazioni>> GetAllQueryProcedureLavorazioniAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            await using var context = await contextFactory.CreateDbContextAsync();
            return await context.QueryProcedureLavorazionis.Include(x => x.IdproceduraLavorazioneNavigation).ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<List<QueryProcedureLavorazioniDto>> GetAllQueryProcedureLavorazioniDtoAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            await using var context = await contextFactory.CreateDbContextAsync();
            if (configUser.IsAdminRole)
            {
                return (await context.QueryProcedureLavorazionis
                    .Include(x => x.IdproceduraLavorazioneNavigation)
                    .OrderBy(x => x.IdproceduraLavorazioneNavigation.NomeProcedura)
                    .ToListAsync())
                    .Select(_mapper.EntityToDto).ToList();
            }
            else
            {
                return (await context.QueryProcedureLavorazionis
                    .Include(x => x.IdproceduraLavorazioneNavigation)
                    .ThenInclude(x => x!.IdproceduraClienteNavigation)
                    .ThenInclude(p => p!.IdclienteNavigation)
                    .ThenInclude(p => p!.IdCentroLavorazioneNavigation)
                    .Where(x => x.IdproceduraLavorazioneNavigation!.Idcentro == configUser.IdCentroOrigine)
                    .OrderBy(x => x.IdproceduraLavorazioneNavigation.NomeProcedura)
                    .ToListAsync())
                    .Select(_mapper.EntityToDto).ToList();
            }
        }

        /// <inheritdoc/>
        public async Task AddQueryProcedureLavorazioniAsync(QueryProcedureLavorazioniDto arg)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            var entity = _mapper.DtoToEntity(arg);
            entity.Titolo = entity.Titolo != null ? entity.Titolo.ToUpper() : entity.Titolo;
            await using var context = await contextFactory.CreateDbContextAsync();
            context.QueryProcedureLavorazionis.Add(entity);
            await context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task UpdateQueryProcedureLavorazioniAsync(QueryProcedureLavorazioniDto queryProcedureLavorazioniDto)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            await using var context = await contextFactory.CreateDbContextAsync();
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

            await using var context = await contextFactory.CreateDbContextAsync();
            var entity = await context.QueryProcedureLavorazionis.FindAsync(id);
            if (entity != null)
            {
                context.QueryProcedureLavorazionis.Remove(entity);
                await context.SaveChangesAsync();
            }
        }
    }
}
