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
    /// Servizio per la gestione degli operatori normalizzati.
    /// </summary>
    public class ServiceOperatoriNormalizzati : ServiceBase<OperatoriNormalizzati>, IServiceOperatoriNormalizzati
    {
        private readonly OperatoriMapper _mapper;

        /// <summary>
        /// Inizializza una nuova istanza del servizio per la gestione degli operatori normalizzati.
        /// </summary>
        /// <param name="mapper">Mapper Mapperly per OperatoriNormalizzati ↔ DTO.</param>
        /// <param name="configUser">Configurazione utente per controllo autorizzazioni.</param>
        /// <param name="contextFactory">Factory per la creazione di contesti database.</param>
        /// <param name="logger">Logger per registrare operazioni e errori.</param>
        public ServiceOperatoriNormalizzati(OperatoriMapper mapper, ConfigUser configUser, IDbContextFactory<DematReportsContext> contextFactory, ILogger<ServiceOperatoriNormalizzati> logger)
            : base(contextFactory, logger, configUser)
        {
            _mapper = mapper;
        }

        /// <inheritdoc/>
        public async Task<List<OperatoriNormalizzati>> GetOperatoriNormalizzatiAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            await using var context = await contextFactory.CreateDbContextAsync();
            return await context.OperatoriNormalizzatis.ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<List<OperatoriNormalizzatiDto>> GetOperatoriNormalizzatiDtoAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            await using var context = await contextFactory.CreateDbContextAsync();
            var lstOper = await context.OperatoriNormalizzatis.ToListAsync();
            var listOperatoriNormalizzati = lstOper.Select(_mapper.NormalizzatoToDto).OrderBy(x => x.OperatoreNormalizzato).ToList();
            return listOperatoriNormalizzati;
        }

        /// <inheritdoc/>
        public async Task AddOperatoriNormalizzatiAsync(OperatoriNormalizzatiDto operatoriNormalizzatiDto)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            var entity = _mapper.DtoToNormalizzato(operatoriNormalizzatiDto);
            await using var context = await contextFactory.CreateDbContextAsync();
            context.OperatoriNormalizzatis.Add(entity);
            await context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task DeleteOperatoriNormalizzatiAsync(int idOper)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            await using var context = await contextFactory.CreateDbContextAsync();
            var entity = await context.OperatoriNormalizzatis.FindAsync(idOper);
            if (entity != null)
            {
                context.OperatoriNormalizzatis.Remove(entity);
                await context.SaveChangesAsync();
            }
        }

        /// <inheritdoc/>
        public async Task UpdateOperatoriNormalizzatiAsync(OperatoriNormalizzatiDto operatoriNormalizzatiDto)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            await using var context = await contextFactory.CreateDbContextAsync();
            var oper = await context.OperatoriNormalizzatis.FirstOrDefaultAsync(x => x.IdNorm.Equals(operatoriNormalizzatiDto.IdNorm));
            if (oper != null)
            {
                oper.OperatoreDaNormalizzare = operatoriNormalizzatiDto.OperatoreDaNormalizzare!;
                oper.OperatoreNormalizzato = operatoriNormalizzatiDto.OperatoreNormalizzato!;
                await context.SaveChangesAsync();
            }
        }
    }
}
