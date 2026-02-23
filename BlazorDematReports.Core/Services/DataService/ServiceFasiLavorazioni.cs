using AutoMapper;
using BlazorDematReports.Core.Application;
using BlazorDematReports.Core.Application.Dto;
using BlazorDematReports.Core.Services.Interfaces.IDataService;
using Entities.Helpers; // AGGIORNATO: Usando il QueryLoggingHelper unificato
using Entities.Models.DbApplication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlazorDematReports.Core.Services.DataService
{
    /// <summary>
    /// Servizio per la gestione delle fasi di lavorazione e delle relative operazioni sui dati.
    /// </summary>
    public class ServiceFasiLavorazioni : ServiceBase<FasiLavorazione>, IServiceFasiLavorazioni
    {

        /// <summary>
        /// Inizializza una nuova istanza della classe <see cref="ServiceFasiLavorazioni"/>.
        /// </summary>
        /// <param name="mapper">Mapper per la conversione tra entit� e DTO.</param>
        /// <param name="configUser">Configurazione utente corrente.</param>
        /// <param name="contextFactory">Factory per la creazione del contesto dati.</param>
        /// <param name="logger">Logger per la registrazione delle operazioni.</param>
        public ServiceFasiLavorazioni(IMapper mapper, ConfigUser configUser, IDbContextFactory<DematReportsContext> contextFactory, ILogger<ServiceFasiLavorazioni> logger)
            : base(contextFactory, logger, mapper, configUser)
        {
        }

        /// <summary>
        /// Restituisce tutte le fasi di lavorazione disponibili nel sistema.
        /// </summary>
        /// <returns>Lista di tutte le fasi di lavorazione.</returns>
        public async Task<List<FasiLavorazione>> GetFasiLavorazioneAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger: logger);

            using var context = contextFactory.CreateDbContext();
            return await context.FasiLavoraziones.Where(x => x.UtilizzataDaSistema == false).ToListAsync();
        }

        /// <summary>
        /// Restituisce tutte le fasi di lavorazione come oggetti DTO ordinati per nome fase.
        /// </summary>
        /// <returns>Lista di DTO delle fasi di lavorazione ordinata alfabeticamente.</returns>
        public async Task<List<FasiLavorazioneDto>> GetFasiLavorazioneDtoAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger: logger);

            using var context = contextFactory.CreateDbContext();
            var lstFasi = await context.FasiLavoraziones.ToListAsync();
            var LstFasiLavorazione = mapper.Map<List<FasiLavorazione>, List<FasiLavorazioneDto>>(lstFasi);
            LstFasiLavorazione = LstFasiLavorazione.OrderBy(x => x.FaseLavorazione).ToList();

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

            using var context = contextFactory.CreateDbContext();
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

            var entity = mapper.Map<FasiLavorazione>(arg);
            using var context = contextFactory.CreateDbContext();
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

            using var context = contextFactory.CreateDbContext();
            var faseLavorazione = await context.FasiLavoraziones.Where(x => x.IdFaseLavorazione == arg.IdFaseLavorazione).FirstOrDefaultAsync();
            if (faseLavorazione != null)
            {
                faseLavorazione.FaseLavorazione = arg.FaseLavorazione!.ToUpper();
                await context.SaveChangesAsync();
            }
        }
    }
}
