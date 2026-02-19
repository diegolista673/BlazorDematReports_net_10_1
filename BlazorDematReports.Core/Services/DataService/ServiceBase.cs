using AutoMapper;
using BlazorDematReports.Core.Application;
using BlazorDematReports.Core.Interfaces.IDataService;
using Entities.Helpers;
using Entities.Models.DbApplication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace BlazorDematReports.Core.Services.DataService
{
    /// <summary>
    /// Servizio base generico per la gestione delle entità e delle operazioni CRUD.
    /// Fornisce campi condivisi (contextFactory, logger, mapper, configUser) a tutti i service figli,
    /// eliminando la necessità di ridichiarazioni ridondanti nelle sottoclassi.
    /// </summary>
    /// <typeparam name="T">Tipo dell'entità gestita.</typeparam>
    public class ServiceBase<T> : IServiceBase<T> where T : class
    {
        /// <summary>Factory per la creazione del contesto dati.</summary>
        protected readonly IDbContextFactory<DematReportsContext> contextFactory;

        /// <summary>Logger per il tracking delle operazioni.</summary>
        protected readonly ILogger? logger;

        /// <summary>Mapper AutoMapper per la conversione tra entità e DTO.</summary>
        protected readonly IMapper? mapper;

        /// <summary>Configurazione utente corrente (ruolo, centro di origine).</summary>
        protected readonly ConfigUser? configUser;

        /// <summary>
        /// Costruttore minimale — solo contextFactory.
        /// </summary>
        public ServiceBase(IDbContextFactory<DematReportsContext> contextFactory)
        {
            this.contextFactory = contextFactory;
        }

        /// <summary>
        /// Costruttore con contextFactory e logger.
        /// </summary>
        public ServiceBase(IDbContextFactory<DematReportsContext> contextFactory, ILogger logger)
        {
            this.contextFactory = contextFactory;
            this.logger = logger;
        }

        /// <summary>
        /// Costruttore completo — tutti i campi condivisi.
        /// </summary>
        /// <param name="contextFactory">Factory per la creazione del contesto dati.</param>
        /// <param name="logger">Logger per il tracking delle operazioni.</param>
        /// <param name="mapper">Mapper AutoMapper per la conversione tra entità e DTO.</param>
        /// <param name="configUser">Configurazione utente corrente.</param>
        public ServiceBase(
            IDbContextFactory<DematReportsContext> contextFactory,
            ILogger logger,
            IMapper mapper,
            ConfigUser configUser)
        {
            this.contextFactory = contextFactory;
            this.logger = logger;
            this.mapper = mapper;
            this.configUser = configUser;
        }

        /// <summary>
        /// Restituisce una query per tutte le entità del tipo specificato.
        /// Ogni chiamata crea un nuovo contesto dati per garantire la thread safety.
        /// </summary>
        /// <returns>IQueryable di T.</returns>
        public IQueryable<T> FindAll()
        {
            if (logger != null)
            {
                QueryLoggingHelper.LogQueryExecution(logger: logger);
            }

            var context = contextFactory.CreateDbContext();
            return context.Set<T>().AsNoTracking();
        }

        /// <summary>
        /// Restituisce una query filtrata tramite espressione lambda.
        /// Ogni chiamata crea un nuovo contesto dati per garantire la thread safety.
        /// </summary>
        /// <param name="expression">Espressione di filtro.</param>
        /// <returns>IQueryable di T filtrato.</returns>
        public IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression)
        {
            if (logger != null)
            {
                QueryLoggingHelper.LogQueryExecution(logger: logger);
            }

            var context = contextFactory.CreateDbContext();
            return context.Set<T>().Where(expression).AsNoTracking();
        }

        /// <summary>
        /// Aggiunge una nuova entità e salva le modifiche nel database.
        /// Ogni chiamata crea un nuovo contesto dati per garantire la thread safety.
        /// </summary>
        /// <param name="entity">Entità da aggiungere.</param>
        /// <returns>Task asincrono.</returns>
        public async Task CreateAsync(T entity)
        {
            if (logger != null)
            {
                QueryLoggingHelper.LogQueryExecution(logger: logger);
            }

            using var context = contextFactory.CreateDbContext();
            context.Set<T>().Add(entity);
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Elimina un'entità tramite identificativo e salva le modifiche nel database.
        /// Ogni chiamata crea un nuovo contesto dati per garantire la thread safety.
        /// </summary>
        /// <param name="id">Identificativo dell'entità da eliminare.</param>
        /// <returns>Task asincrono.</returns>
        public async Task DeleteAsync(int id)
        {
            if (logger != null)
            {
                QueryLoggingHelper.LogQueryExecution(logger: logger);
            }

            using var context = contextFactory.CreateDbContext();
            T? existing = context.Set<T>().Find(id);
            if (existing != null)
            {
                context.Set<T>().Remove(existing);
                await context.SaveChangesAsync();
            }
        }
    }
}
