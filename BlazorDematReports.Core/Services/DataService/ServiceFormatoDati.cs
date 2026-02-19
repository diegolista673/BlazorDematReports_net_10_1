using AutoMapper;
using BlazorDematReports.Core.Application;
using BlazorDematReports.Core.Application.Dto;
using BlazorDematReports.Core.Interfaces.IDataService;
using Entities.Helpers;
using Entities.Models.DbApplication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlazorDematReports.Core.Services.DataService
{
    /// <summary>
    /// Servizio per la gestione dei formati dati e delle relative operazioni sui dati.
    /// </summary>
    public class ServiceFormatoDati : ServiceBase<FormatoDati>, IServiceFormatoDati
    {

        /// <summary>
        /// Inizializza una nuova istanza del servizio per la gestione dei formati dati.
        /// </summary>
        /// <param name="mapper">Mapper per conversioni tra entit� e DTO.</param>
        /// <param name="configUser">Configurazione utente per controllo autorizzazioni.</param>
        /// <param name="contextFactory">Factory per la creazione di contesti database.</param>
        /// <param name="logger">Logger per registrare operazioni e errori.</param>
        public ServiceFormatoDati(IMapper mapper, ConfigUser configUser, IDbContextFactory<DematReportsContext> contextFactory, ILogger<ServiceFormatoDati> logger)
            : base(contextFactory, logger, mapper, configUser)
        {
        }

        /// <inheritdoc/>
        public async Task AddFormatoDati(FormatoDatiDto formatoDatiDto)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            var entity = mapper.Map<FormatoDati>(formatoDatiDto);
            using var context = contextFactory.CreateDbContext();
            context.FormatoDatis.Add(entity);
            await context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task DeleteFormatoDati(int IdFormatoDati)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            var entity = await context.FormatoDatis.FindAsync(IdFormatoDati);
            if (entity != null)
            {
                context.FormatoDatis.Remove(entity);
                await context.SaveChangesAsync();
            }
        }

        /// <inheritdoc/>
        public async Task<List<FormatoDati>> GetFormatoDatiAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            return await context.FormatoDatis.ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<FormatoDati?> GetFormatoDatiByIdAsync(int IdFormatoDati)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            return await context.FormatoDatis.FirstOrDefaultAsync(c => c.IdformatoDati.Equals(IdFormatoDati));
        }

        /// <inheritdoc/>
        public async Task<FormatoDati?> GetFormatoDatiByTextAsync(string formatoDati)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            return await context.FormatoDatis.FirstOrDefaultAsync(c => c.FormatoDatiProduzione == formatoDati);
        }
    }
}
