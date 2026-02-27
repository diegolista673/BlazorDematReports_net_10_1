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
    /// Servizio per la gestione dei ruoli e delle relative operazioni sui dati.
    /// </summary>
    public class ServiceRuoli : ServiceBase<Ruoli>, IServiceRuoli
    {
        private readonly TurniMapper _mapper;

        /// <summary>
        /// Inizializza una nuova istanza del servizio per la gestione dei ruoli.
        /// </summary>
        /// <param name="mapper">Mapper Mapperly per conversioni Ruoli ↔ DTO.</param>
        /// <param name="configUser">Configurazione utente per controllo autorizzazioni.</param>
        /// <param name="contextFactory">Factory per la creazione di contesti database.</param>
        /// <param name="logger">Logger per registrare operazioni e errori.</param>
        public ServiceRuoli(TurniMapper mapper, ConfigUser configUser, IDbContextFactory<DematReportsContext> contextFactory, ILogger<ServiceRuoli> logger)
            : base(contextFactory, logger, configUser)
        {
            _mapper = mapper;
        }

        /// <inheritdoc/>
        public async Task<List<Ruoli>> GetRuoliAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            await using var context = await contextFactory.CreateDbContextAsync();
            return await context.Ruolis.ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<List<Ruoli>> GetRuoliByUserAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);
            await using var context = await contextFactory.CreateDbContextAsync();
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
            await using var context = await contextFactory.CreateDbContextAsync();
            return await context.Ruolis.FirstOrDefaultAsync(x => x.IdRuolo == IdRuolo);
        }

        /// <inheritdoc/>
        public async Task<List<RuoliDto>> GetRuoliDtoAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            await using var context = await contextFactory.CreateDbContextAsync();
            List<Ruoli> lst = await context.Ruolis.ToListAsync();
            var LstRuoli = lst.Select(_mapper.RuoloToDto).OrderBy(x => x.IdRuolo).ToList();
            return LstRuoli;
        }

        /// <inheritdoc/>
        public async Task AddRuoloAsync(RuoliDto ruoliDto)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            var entity = _mapper.DtoToRuolo(ruoliDto);
            await using var context = await contextFactory.CreateDbContextAsync();
            context.Ruolis.Add(entity);
            await context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task DeleteRuoloAsync(int idRuolo)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            await using var context = await contextFactory.CreateDbContextAsync();
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

            await using var context = await contextFactory.CreateDbContextAsync();
            var ruolo = await context.Ruolis.FirstOrDefaultAsync(x => x.IdRuolo.Equals(arg.IdRuolo));
            if (ruolo != null)
            {
                ruolo.Ruolo = arg.Ruolo!;
                await context.SaveChangesAsync();
            }
        }
    }
}
