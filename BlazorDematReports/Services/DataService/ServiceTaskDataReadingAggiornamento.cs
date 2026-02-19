using AutoMapper;
using BlazorDematReports.Application;
using BlazorDematReports.Dto;
using BlazorDematReports.Interfaces.IDataService;
using Entities.Helpers;
using Entities.Models.DbApplication;
using Microsoft.EntityFrameworkCore;

namespace BlazorDematReports.Services.DataService
{
    /// <summary>
    /// Servizio per la gestione degli aggiornamenti task data reading e delle relative operazioni sui dati.
    /// </summary>
    public class ServiceTaskDataReadingAggiornamento : ServiceBase<TaskDataReadingAggiornamento>, IServiceTaskDataReadingAggiornamento
    {

        // Compiled query per ultimo aggiornamento (usa MAX su DataAggiornamento)
        private static readonly Func<DematReportsContext, int, int, DateTime?> _getLastAggiornamentoCompiled =
            EF.CompileQuery((DematReportsContext ctx, int idProc, int idFase) =>
                ctx.TaskDataReadingAggiornamentos
                   .Where(x => x.IdLavorazione == idProc && x.IdFase == idFase)
                   .Select(x => (DateTime?)x.DataAggiornamento)
                   .Max());

        /// <summary>
        /// Costruttore che inizializza le dipendenze necessarie per la gestione degli aggiornamenti task data reading.
        /// </summary>
        /// <param name="mapper">Servizio per la mappatura tra entit� e DTO.</param>
        /// <param name="configUser">Configurazione dell'utente corrente.</param>
        /// <param name="contextFactory">Factory per la creazione del contesto dati.</param>
        /// <param name="logger">Logger per il tracking delle operazioni.</param>
        public ServiceTaskDataReadingAggiornamento(IMapper mapper, ConfigUser configUser, IDbContextFactory<DematReportsContext> contextFactory, ILogger<ServiceTaskDataReadingAggiornamento> logger) : base(contextFactory, logger, mapper, configUser)
        {
        }

        /// <summary>
        /// Restituisce la lista degli aggiornamenti task data reading filtrata per intervallo di date.
        /// </summary>
        /// <param name="startDate">Data di inizio del periodo di ricerca.</param>
        /// <param name="endDate">Data di fine del periodo di ricerca.</param>
        /// <returns>Lista di oggetti <see cref="TaskDataReadingAggiornamento"/> ordinata per data aggiornamento decrescente.</returns>
        public async Task<List<TaskDataReadingAggiornamento>> GetAggiornamentoLavorazioneByDateAsync(DateTime startDate, DateTime endDate)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            return await FindByCondition(x => x.DataAggiornamento >= startDate.Date && x.DataAggiornamento <= endDate.Date).OrderByDescending(x => x.DataAggiornamento).ToListAsync();
        }

        /// <summary>
        /// Restituisce la lista degli aggiornamenti task data reading per procedura e data specifiche e li mappa su DTO.
        /// </summary>
        /// <param name="IdProceduraLavorazione">Identificativo della procedura di lavorazione.</param>
        /// <param name="dataAggiornamento">Data di aggiornamento specifica.</param>
        /// <returns>Lista di oggetti <see cref="TaskDataReadingAggiornamentoDto"/> ordinata per data aggiornamento.</returns>
        public async Task<List<TaskDataReadingAggiornamentoDto>> GetAggiornamentoLavorazioneDtoByDateAsync(int IdProceduraLavorazione, DateTime dataAggiornamento)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            var lstTask = await FindByCondition(x => x.IdLavorazione == IdProceduraLavorazione && x.DataAggiornamento.Date == dataAggiornamento.Date)
                                .OrderBy(x => x.DataAggiornamento)
                                .ToListAsync();

            var listTaskDto = mapper.Map<List<TaskDataReadingAggiornamento>, List<TaskDataReadingAggiornamentoDto>>(lstTask);

            return listTaskDto;
        }

        /// <summary>
        /// Restituisce la lista degli aggiornamenti task data reading per intervallo di date e li mappa su DTO.
        /// </summary>
        /// <param name="startDate">Data di inizio del periodo di ricerca.</param>
        /// <param name="endDate">Data di fine del periodo di ricerca.</param>
        /// <returns>Lista di oggetti <see cref="TaskDataReadingAggiornamentoDto"/> ordinata per data aggiornamento.</returns>
        public async Task<List<TaskDataReadingAggiornamentoDto>> GetAggiornamentoDtoByDateAsync(DateTime startDate, DateTime endDate)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            var lstTask = await FindByCondition(x => x.DataAggiornamento.Date >= startDate.Date && x.DataAggiornamento.Date <= endDate.Date).OrderBy(x => x.DataAggiornamento).ToListAsync();
            var listTaskDto = mapper.Map<List<TaskDataReadingAggiornamento>, List<TaskDataReadingAggiornamentoDto>>(lstTask);

            return listTaskDto;
        }

        /// <summary>
        /// Restituisce l'aggiornamento task data reading pi� recente per procedura e fase specifiche.
        /// </summary>
        /// <param name="IdProceduraLavorazione">Identificativo della procedura di lavorazione.</param>
        /// <param name="IdFase">Identificativo della fase di lavorazione.</param>
        /// <returns>Oggetto <see cref="TaskDataReadingAggiornamento"/> pi� recente o null se non trovato.</returns>
        public async Task<TaskDataReadingAggiornamento?> GetAggiornamentoLavorazioneAsync(int IdProceduraLavorazione, int IdFase)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            return await FindByCondition(x => x.IdLavorazione.Equals(IdProceduraLavorazione) && x.IdFase.Equals(IdFase))
                .OrderByDescending(x => x.DataAggiornamento)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Restituisce la data dell'ultimo aggiornamento per procedura e fase specifiche come stringa formattata.
        /// </summary>
        /// <param name="IdProceduraLavorazione">Identificativo della procedura di lavorazione.</param>
        /// <param name="idFaseLavorazione">Identificativo della fase di lavorazione.</param>
        /// <returns>Data dell'ultimo aggiornamento in formato stringa o null se non trovato.</returns>
        public Task<string?> GetUltimoAggiornamentoAsync(int IdProceduraLavorazione, int idFaseLavorazione)
        {
            QueryLoggingHelper.LogQueryExecution(logger);
            using var context = contextFactory.CreateDbContext();
            var lastDate = _getLastAggiornamentoCompiled(context, IdProceduraLavorazione, idFaseLavorazione);
            return Task.FromResult(lastDate?.ToShortDateString());
        }
    }
}