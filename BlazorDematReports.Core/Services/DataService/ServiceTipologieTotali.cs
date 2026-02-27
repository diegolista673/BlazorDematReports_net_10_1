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
    /// Servizio per la gestione delle tipologie totali e delle relative operazioni sui dati.
    /// </summary>
    public class ServiceTipologieTotali : ServiceBase<TipologieTotali>, IServiceTipologieTotali
    {
        private readonly TipologieTotaliMapper _mapper;

        /// <summary>
        /// Costruttore che inizializza le dipendenze necessarie per la gestione delle tipologie totali.
        /// </summary>
        /// <param name="mapper">Mapper Mapperly per conversioni TipologieTotali ↔ DTO.</param>
        /// <param name="configUser">Configurazione dell'utente corrente.</param>
        /// <param name="contextFactory">Factory per la creazione del contesto dati.</param>
        /// <param name="logger">Logger per il tracking delle operazioni.</param>
        public ServiceTipologieTotali(TipologieTotaliMapper mapper, ConfigUser configUser, IDbContextFactory<DematReportsContext> contextFactory, ILogger<ServiceTipologieTotali> logger) : base(contextFactory, logger, configUser)
        {
            _mapper = mapper;
        }

        /// <summary>
        /// Restituisce tutte le tipologie totali disponibili nel sistema.
        /// </summary>
        /// <returns>Lista di tutte le tipologie totali.</returns>
        public async Task<List<TipologieTotali>> GetTipologieTotaliAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            return (await FindAllAsync()).ToList();
        }

        /// <summary>
        /// Elimina una tipologia totale specificata dall'ID.
        /// </summary>
        /// <param name="idTotaliTipologie">ID della tipologia totale da eliminare.</param>
        /// <returns>Task asincrono per l'operazione di eliminazione.</returns>
        public async Task DeleteTipologieTotaliAsync(int idTotaliTipologie)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            await DeleteAsync(idTotaliTipologie);
        }

        /// <summary>
        /// Aggiunge una nuova tipologia totale al sistema.
        /// </summary>
        /// <param name="arg">DTO della tipologia totale da aggiungere.</param>
        /// <returns>Task asincrono per l'operazione di inserimento.</returns>
        public async Task AddTipologieTotaliAsync(TipologieTotaliDto arg)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            TipologieTotali TipologieTotali = _mapper.DtoToTotale(arg);
            await CreateAsync(TipologieTotali);
        }

        /// <summary>
        /// Restituisce le tipologie totali attive per una specifica procedura di lavorazione e fase.
        /// </summary>
        /// <param name="IDProceduraLavorazione">ID della procedura di lavorazione.</param>
        /// <param name="IDFase">ID della fase di lavorazione.</param>
        /// <returns>Lista delle tipologie totali attive per la procedura e fase specificate.</returns>
        public async Task<List<TipologieTotali>> GetTipologieAttiveByIdLavorazioneAsync(int IDProceduraLavorazione, int IDFase)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            await using var context = await contextFactory.CreateDbContextAsync();
            return await context.TipologieTotalis
                .Where(tipo => tipo.LavorazioniFasiTipoTotales.Any(c => c.IdProceduraLavorazione == IDProceduraLavorazione &&
                                                                        c.IdFase == IDFase))
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Restituisce tutte le tipologie totali come oggetti DTO per la presentazione.
        /// </summary>
        /// <returns>Lista di DTO delle tipologie totali.</returns>
        public async Task<List<TipologieTotaliDto>> GetTipologieTotaliDtoAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            var lstTipologie = (await FindAllAsync()).ToList();
            var lstTipologieDto = lstTipologie.Select(_mapper.TotaleToDto).ToList();
            return lstTipologieDto;
        }

        /// <summary>
        /// Aggiorna una tipologia totale esistente con i nuovi dati forniti.
        /// </summary>
        /// <param name="arg">DTO della tipologia totale con i dati aggiornati.</param>
        /// <returns>Task asincrono per l'operazione di aggiornamento.</returns>
        public async Task UpdateTipologieTotaliAsync(TipologieTotaliDto arg)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            await using var context = await contextFactory.CreateDbContextAsync();
            var tipologia = await context.TipologieTotalis.Where(x => x.IdTipoTotale == arg.IdTipoTotale).FirstOrDefaultAsync();
            tipologia!.TipoTotale = arg.TipoTotale!.ToUpper();

            await context.SaveChangesAsync();
        }
    }
}
