using AutoMapper;
using BlazorDematReports.Core.Application;
using BlazorDematReports.Core.Application.Dto;
using BlazorDematReports.Core.Services.Interfaces.IDataService;
using Entities.Helpers;
using Entities.Models.DbApplication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace BlazorDematReports.Core.Services.DataService
{
    /// <summary>
    /// Servizio per la gestione dei reparti di produzione.
    /// Fornisce operazioni CRUD per i reparti e metodi di ricerca specializzati.
    /// </summary>
    public class ServiceRepartiProduzione : IServiceRepartiProduzione
    {
        private readonly IMapper mapper;
        private readonly ConfigUser configUser;
        private readonly IDbContextFactory<DematReportsContext> contextFactory;
        private readonly ILogger<ServiceRepartiProduzione> logger;

        /// <summary>
        /// Inizializza una nuova istanza del servizio per la gestione dei reparti di produzione.
        /// </summary>
        /// <param name="mapper">Mapper per conversioni tra entità e DTO.</param>
        /// <param name="configUser">Configurazione utente per controllo autorizzazioni.</param>
        /// <param name="contextFactory">Factory per la creazione di contesti database.</param>
        /// <param name="logger">Logger per registrare operazioni e errori.</param>
        public ServiceRepartiProduzione(IMapper mapper, ConfigUser configUser, IDbContextFactory<DematReportsContext> contextFactory, ILogger<ServiceRepartiProduzione> logger)
        {
            this.mapper = mapper;
            this.configUser = configUser;
            this.contextFactory = contextFactory;
            this.logger = logger;
        }

        /// <inheritdoc/>
        public IQueryable<RepartiProduzione> FindAll()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            var context = contextFactory.CreateDbContext();
            return context.RepartiProduziones.AsNoTracking();
        }

        /// <inheritdoc/>
        public IQueryable<RepartiProduzione> FindByCondition(Expression<Func<RepartiProduzione, bool>> expression)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            var context = contextFactory.CreateDbContext();
            return context.RepartiProduziones.Where(expression).AsNoTracking();
        }

        /// <inheritdoc/>
        public async Task CreateAsync(RepartiProduzione entity)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            context.RepartiProduziones.Add(entity);
            await context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(int id)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            var entity = await context.RepartiProduziones.FindAsync(id);
            if (entity != null)
            {
                context.RepartiProduziones.Remove(entity);
                await context.SaveChangesAsync();
            }
        }

        /// <inheritdoc/>
        public async Task AddRepartiProduzione(RepartiProduzioneDto repartiProduzioneDto)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            var entity = mapper.Map<RepartiProduzione>(repartiProduzioneDto);
            using var context = contextFactory.CreateDbContext();
            context.RepartiProduziones.Add(entity);
            await context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task DeleteRepartiProduzione(int IdReparto)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            var entity = await context.RepartiProduziones.FindAsync(IdReparto);
            if (entity != null)
            {
                context.RepartiProduziones.Remove(entity);
                await context.SaveChangesAsync();
            }
        }

        /// <inheritdoc/>
        public async Task<List<RepartiProduzione>> GetRepartiProduzioneAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            return await context.RepartiProduziones.OrderBy(x => x.Reparti).ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<RepartiProduzione?> GetRepartiProduzioneByIdAsync(int IdReparto)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            return await context.RepartiProduziones.FirstOrDefaultAsync(c => c.IdReparti.Equals(IdReparto));
        }

        /// <inheritdoc/>
        public async Task<RepartiProduzione?> GetRepartiProduzioneByTextAsync(string reparto)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            return await context.RepartiProduziones.FirstOrDefaultAsync(x => x.Reparti == reparto);
        }
    }
}
