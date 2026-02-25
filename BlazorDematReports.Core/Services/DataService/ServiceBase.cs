using AutoMapper;
using BlazorDematReports.Core.Application;
using BlazorDematReports.Core.Services.Interfaces.IDataService;
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
        protected readonly ILogger logger;

        /// <summary>Mapper AutoMapper per la conversione tra entità e DTO.</summary>
        protected readonly IMapper mapper;

        /// <summary>Configurazione utente corrente (ruolo, centro di origine).</summary>
        protected readonly ConfigUser configUser;

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
            ArgumentNullException.ThrowIfNull(contextFactory);
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentNullException.ThrowIfNull(mapper);
            ArgumentNullException.ThrowIfNull(configUser);

            this.contextFactory = contextFactory;
            this.logger = logger;
            this.mapper = mapper;
            this.configUser = configUser;
        }

        /// <summary>
        /// Restituisce tutte le entità materializzate come lista read-only.
        /// Il contesto viene creato e disposto internamente per garantire la corretta gestione delle risorse.
        /// </summary>
        /// <returns>Lista read-only di tutte le entità.</returns>
        public async Task<IReadOnlyList<T>> FindAllAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger: logger);

            await using var context = await contextFactory.CreateDbContextAsync();
            return await context.Set<T>().AsNoTracking().ToListAsync();
        }

        /// <summary>
        /// Restituisce le entità filtrate tramite espressione lambda, materializzate come lista read-only.
        /// Il contesto viene creato e disposto internamente per garantire la corretta gestione delle risorse.
        /// </summary>
        /// <param name="expression">Espressione di filtro.</param>
        /// <returns>Lista read-only delle entità filtrate.</returns>
        public async Task<IReadOnlyList<T>> FindByConditionAsync(Expression<Func<T, bool>> expression)
        {
            QueryLoggingHelper.LogQueryExecution(logger: logger);

            await using var context = await contextFactory.CreateDbContextAsync();
            return await context.Set<T>().Where(expression).AsNoTracking().ToListAsync();
        }

        /// <summary>
        /// Aggiunge una nuova entità e salva le modifiche nel database.
        /// Ogni chiamata crea un nuovo contesto dati per garantire la thread safety.
        /// </summary>
        /// <param name="entity">Entità da aggiungere.</param>
        /// <returns>Task asincrono.</returns>
        public async Task CreateAsync(T entity)
        {
            QueryLoggingHelper.LogQueryExecution(logger: logger);

            await using var context = await contextFactory.CreateDbContextAsync();
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
            QueryLoggingHelper.LogQueryExecution(logger: logger);

            await using var context = await contextFactory.CreateDbContextAsync();
            T? existing = await context.Set<T>().FindAsync(id);
            if (existing != null)
            {
                context.Set<T>().Remove(existing);
                await context.SaveChangesAsync();
            }
        }
    }
}
