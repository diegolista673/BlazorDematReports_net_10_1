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
    /// Servizio per la gestione delle fasi di lavorazione.
    /// </summary>
    public class ServiceFasiLavorazioni : ServiceBase<FasiLavorazione>, IServiceFasiLavorazioni
    {
        private readonly LavorazioniFasiMapper _mapper;

        /// <summary>
        /// Inizializza una nuova istanza di <see cref="ServiceFasiLavorazioni"/>.
        /// </summary>
        /// <param name="mapper">Mapper Mapperly per FasiLavorazione ↔ DTO.</param>
        /// <param name="configUser">Configurazione utente corrente.</param>
        /// <param name="contextFactory">Factory per la creazione del contesto dati.</param>
        /// <param name="logger">Logger per la registrazione delle operazioni.</param>
        public ServiceFasiLavorazioni(LavorazioniFasiMapper mapper, ConfigUser configUser, IDbContextFactory<DematReportsContext> contextFactory, ILogger<ServiceFasiLavorazioni> logger)
            : base(contextFactory, logger, configUser)
        {
            _mapper = mapper;
        }

        /// <summary>
        /// Restituisce tutte le fasi di lavorazione disponibili nel sistema.
        /// </summary>
        /// <returns>Lista di tutte le fasi di lavorazione.</returns>
        public async Task<List<FasiLavorazione>> GetFasiLavorazioneAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger: logger);

            await using var context = await contextFactory.CreateDbContextAsync();
            return await context.FasiLavoraziones.Where(x => x.UtilizzataDaSistema == false).ToListAsync();
        }

        /// <summary>
        /// Restituisce tutte le fasi di lavorazione come oggetti DTO ordinati per nome fase.
        /// </summary>
        /// <returns>Lista di DTO delle fasi di lavorazione ordinata alfabeticamente.</returns>
        public async Task<List<FasiLavorazioneDto>> GetFasiLavorazioneDtoAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger: logger);

            await using var context = await contextFactory.CreateDbContextAsync();
            var lstFasi = await context.FasiLavoraziones.ToListAsync();
            var LstFasiLavorazione = lstFasi.Select(_mapper.FaseToDto).OrderBy(x => x.FaseLavorazione).ToList();

            return LstFasiLavorazione;
        }

        /// <summary>
        /// Elimina una fase di lavorazione specificata dall'ID.
        /// </summary>
        /// <param name="idFase">ID della fase di lavorazione da eliminare.</param>
        /// <returns>Task asincrono per l'operazione di eliminazione.</returns>
        public async Task DeleteFasiLavorazioneAsync(int idFase)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            await using var context = await contextFactory.CreateDbContextAsync();
            var entity = await context.FasiLavoraziones.FindAsync(idFase);
            if (entity != null)
            {
                context.FasiLavoraziones.Remove(entity);
                await context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Aggiunge una nuova fase di lavorazione al sistema.
        /// </summary>
        /// <param name="arg">DTO della fase di lavorazione da aggiungere.</param>
        /// <returns>Task asincrono per l'operazione di inserimento.</returns>
        public async Task AddFasiLavorazioneAsync(FasiLavorazioneDto arg)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            var entity = _mapper.DtoToFase(arg);
            await using var context = await contextFactory.CreateDbContextAsync();
            context.FasiLavoraziones.Add(entity);
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Aggiorna una fase di lavorazione esistente con i nuovi dati forniti.
        /// </summary>
        /// <param name="arg">DTO della fase di lavorazione con i dati aggiornati.</param>
        /// <returns>Task asincrono per l'operazione di aggiornamento.</returns>
        public async Task UpdateFasiLavorazioneAsync(FasiLavorazioneDto arg)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            await using var context = await contextFactory.CreateDbContextAsync();
            var faseLavorazione = await context.FasiLavoraziones.Where(x => x.IdFaseLavorazione == arg.IdFaseLavorazione).FirstOrDefaultAsync();
            if (faseLavorazione != null)
            {
                faseLavorazione.FaseLavorazione = arg.FaseLavorazione!.ToUpper();
                await context.SaveChangesAsync();
            }
        }
    }
}
