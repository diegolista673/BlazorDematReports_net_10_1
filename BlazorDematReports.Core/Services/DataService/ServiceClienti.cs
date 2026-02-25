using AutoMapper;
using BlazorDematReports.Core.Application;
using BlazorDematReports.Core.Application.Dto;
using BlazorDematReports.Core.Services.Interfaces.IDataService;
using Entities.Helpers;
using Entities.Models.DbApplication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlazorDematReports.Core.Services.DataService
{
    /// <summary>
    /// Servizio per la gestione dei clienti e delle relative operazioni sui dati.
    /// </summary>
    public class ServiceClienti : ServiceBase<Clienti>, IServiceClienti
    {

        /// <summary>
        /// Costruttore che inizializza le dipendenze necessarie per la gestione dei clienti.
        /// </summary>
        /// <param name="mapper">Servizio per la mappatura tra entità e DTO.</param>
        /// <param name="configUser">Configurazione dell'utente corrente.</param>
        /// <param name="contextFactory">Factory per la creazione del contesto dati.</param>
        /// <param name="logger">Logger per il tracking delle operazioni.</param>
        public ServiceClienti(IMapper mapper, ConfigUser configUser, IDbContextFactory<DematReportsContext> contextFactory, ILogger<ServiceClienti> logger)
            : base(contextFactory, logger, mapper, configUser)
        {
        }

        /// <summary>
        /// Aggiunge un nuovo cliente al sistema.
        /// </summary>
        /// <param name="clienteDto">DTO del cliente da aggiungere.</param>
        /// <returns>Task asincrono per l'operazione di inserimento.</returns>
        public async Task AddClienteAsync(ClientiDto clienteDto)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            var entity = mapper.Map<Clienti>(clienteDto);
            await using var context = await contextFactory.CreateDbContextAsync();
            context.Clientis.Add(entity);
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Elimina un cliente specificato dall'ID.
        /// </summary>
        /// <param name="idcliente">ID del cliente da eliminare.</param>
        /// <returns>Task asincrono per l'operazione di eliminazione.</returns>
        public async Task DeleteClienteAsync(int idcliente)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            await using var context = await contextFactory.CreateDbContextAsync();
            var entity = await context.Clientis.FindAsync(idcliente);
            if (entity != null)
            {
                context.Clientis.Remove(entity);
                await context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Restituisce i clienti accessibili all'utente corrente in base al suo ruolo e centro di origine.
        /// </summary>
        /// <returns>Lista dei clienti filtrati per l'utente corrente con informazioni del centro incluse.</returns>
        public async Task<List<Clienti>> GetClientiByUserAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            await using var context = await contextFactory.CreateDbContextAsync();
            if (configUser.IsAdminRole)
            {
                return await context.Clientis.Include(x => x.IdCentroLavorazioneNavigation).ToListAsync();
            }
            else
            {
                return await context.Clientis.Include(x => x.IdCentroLavorazioneNavigation).Where(x => x.IdCentroLavorazione == configUser.IdCentroOrigine).ToListAsync();
            }
        }

        /// <summary>
        /// Restituisce tutti i clienti appartenenti al centro di lavorazione specificato.
        /// </summary>
        /// <param name="idCentro">ID del centro di lavorazione per il filtro.</param>
        /// <returns>Lista dei clienti del centro specificato con informazioni del centro incluse.</returns>
        public async Task<List<Clienti>> GetClientiByIDCentroAsync(int idCentro)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            await using var context = await contextFactory.CreateDbContextAsync();
            return await context.Clientis.Include(x => x.IdCentroLavorazioneNavigation).Where(x => x.IdCentroLavorazione == idCentro).ToListAsync();
        }

        /// <summary>
        /// Restituisce i clienti come DTO ottimizzati con proiezione diretta per migliorare le performance.
        /// </summary>
        /// <returns>Lista di DTO dei clienti ordinati per nome, filtrati per l'utente corrente.</returns>
        public async Task<List<ClientiDto>> GetClientiDtoByUserAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            await using var context = await contextFactory.CreateDbContextAsync();
            var query = context.Clientis.AsQueryable();
            if (!configUser.IsAdminRole)
            {
                query = query.Where(x => x.IdCentroLavorazione == configUser.IdCentroOrigine);
            }
            var result = await query
                .Select(x => new ClientiDto
                {
                    IdCliente = x.IdCliente,
                    NomeCliente = x.NomeCliente,
                    DataCreazioneCliente = x.DataCreazioneCliente,
                    IdCentroLavorazione = x.IdCentroLavorazione,
                    Centro = x.IdCentroLavorazioneNavigation.Centro
                })
                .OrderBy(x => x.NomeCliente)
                .ToListAsync();
            return result;
        }

        /// <summary>
        /// Restituisce un cliente specifico identificato dall'ID.
        /// </summary>
        /// <param name="IdCliente">ID del cliente da cercare.</param>
        /// <returns>Il cliente specificato o null se non trovato.</returns>
        public async Task<Clienti?> GetClienteByIdAsync(int IdCliente)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            await using var context = await contextFactory.CreateDbContextAsync();
            return await context.Clientis.Where(x => x.IdCliente == IdCliente).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Aggiorna un cliente esistente con i nuovi dati forniti.
        /// </summary>
        /// <param name="arg">DTO del cliente con i dati aggiornati.</param>
        /// <returns>Task asincrono per l'operazione di aggiornamento.</returns>
        public async Task UpdateClienteAsync(ClientiDto arg)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            await using var context = await contextFactory.CreateDbContextAsync();
            var cliente = await context.Clientis.Where(x => x.IdCliente.Equals(arg.IdCliente)).Include(x => x.IdCentroLavorazioneNavigation).FirstOrDefaultAsync();
            if (cliente != null)
            {
                cliente.NomeCliente = arg.NomeCliente!;
                cliente.DataCreazioneCliente = arg.DataCreazioneCliente;
                cliente.IdCentroLavorazione = (int)arg.IdCentroLavorazione!;
                await context.SaveChangesAsync();
            }
        }
    }
}
