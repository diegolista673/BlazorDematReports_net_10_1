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
    /// Servizio per la gestione degli operatori normalizzati e delle relative operazioni sui dati.
    /// </summary>
    public class ServiceOperatoriNormalizzati : ServiceBase<OperatoriNormalizzati>, IServiceOperatoriNormalizzati
    {

        /// <summary>
        /// Inizializza una nuova istanza del servizio per la gestione degli operatori normalizzati.
        /// </summary>
        /// <param name="mapper">Mapper per conversioni tra entit� e DTO.</param>
        /// <param name="configUser">Configurazione utente per controllo autorizzazioni.</param>
        /// <param name="contextFactory">Factory per la creazione di contesti database.</param>
        /// <param name="logger">Logger per registrare operazioni e errori.</param>
        public ServiceOperatoriNormalizzati(IMapper mapper, ConfigUser configUser, IDbContextFactory<DematReportsContext> contextFactory, ILogger<ServiceOperatoriNormalizzati> logger)
            : base(contextFactory, logger, mapper, configUser)
        {
        }

        /// <inheritdoc/>
        public async Task<List<OperatoriNormalizzati>> GetOperatoriNormalizzatiAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            return await context.OperatoriNormalizzatis.ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<List<OperatoriNormalizzatiDto>> GetOperatoriNormalizzatiDtoAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            var lstOper = await context.OperatoriNormalizzatis.ToListAsync();
            var listOperatoriNormalizzati = mapper.Map<List<OperatoriNormalizzati>, List<OperatoriNormalizzatiDto>>(lstOper);
            listOperatoriNormalizzati = listOperatoriNormalizzati.OrderBy(x => x.OperatoreNormalizzato).ToList();
            return listOperatoriNormalizzati;
        }

        /// <inheritdoc/>
        public async Task AddOperatoriNormalizzatiAsync(OperatoriNormalizzatiDto operatoriNormalizzatiDto)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            var entity = mapper.Map<OperatoriNormalizzati>(operatoriNormalizzatiDto);
            using var context = contextFactory.CreateDbContext();
            context.OperatoriNormalizzatis.Add(entity);
            await context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task DeleteOperatoriNormalizzatiAsync(int idOper)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
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

            using var context = contextFactory.CreateDbContext();
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
