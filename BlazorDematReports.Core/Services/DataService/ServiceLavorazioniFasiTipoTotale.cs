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
    /// Servizio per la gestione delle lavorazioni fasi tipo totale.
    /// </summary>
    public class ServiceLavorazioniFasiTipoTotale : ServiceBase<LavorazioniFasiTipoTotale>, IServiceLavorazioniFasiTipoTotale
    {
        private readonly LavorazioniFasiMapper _mapper;

        /// <summary>
        /// Inizializza una nuova istanza del servizio per la gestione delle lavorazioni fasi tipo totale.
        /// </summary>
        /// <param name="mapper">Mapper Mapperly per LavorazioniFasiTipoTotale ↔ DTO.</param>
        /// <param name="configUser">Configurazione utente per controllo autorizzazioni.</param>
        /// <param name="contextFactory">Factory per la creazione di contesti database.</param>
        /// <param name="logger">Logger per registrare operazioni e errori.</param>
        public ServiceLavorazioniFasiTipoTotale(LavorazioniFasiMapper mapper, ConfigUser configUser, IDbContextFactory<DematReportsContext> contextFactory, ILogger<ServiceLavorazioniFasiTipoTotale> logger)
            : base(contextFactory, logger, configUser)
        {
            _mapper = mapper;
        }

        /// <inheritdoc/>
        public async Task<List<LavorazioniFasiTipoTotale>> GetLavorazioniFasiTipoTotaleAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            await using var context = await contextFactory.CreateDbContextAsync();
            return await context.LavorazioniFasiTipoTotales
                .Include(x => x.IdProceduraLavorazioneNavigation)
                .Include(x => x.IdTipologiaTotaleNavigation)
                .Include(x => x.IdFaseNavigation)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<List<LavorazioniFasiTipoTotaleDto>> GetLavorazioniFasiTipoTotaleDtoAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            await using var context = await contextFactory.CreateDbContextAsync();
            var result = await context.LavorazioniFasiTipoTotales
                .Select(x => new LavorazioniFasiTipoTotaleDto
                {
                    IdLavorazioneFaseTipoTotale = x.IdLavorazioneFaseTipoTotale,
                    IdProceduraLavorazione = x.IdProceduraLavorazione,
                    IdFase = x.IdFase,
                    IdTipologiaTotale = x.IdTipologiaTotale,
                    NomeProcedura = x.IdProceduraLavorazioneNavigation.NomeProcedura,
                    Fase = x.IdFaseNavigation.FaseLavorazione,
                    TipologiaTotale = x.IdTipologiaTotaleNavigation.TipoTotale
                })
                .OrderBy(x => x.NomeProcedura)
                .ToListAsync();
            return result;
        }

        /// <inheritdoc/>
        public async Task DeleteLavorazioniFasiTipoTotaleAsync(int IdtipologieTotaliLavorazioneFase)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            await using var context = await contextFactory.CreateDbContextAsync();
            var entity = await context.LavorazioniFasiTipoTotales.FindAsync(IdtipologieTotaliLavorazioneFase);
            if (entity != null)
            {
                context.LavorazioniFasiTipoTotales.Remove(entity);
                await context.SaveChangesAsync();
            }
        }

        /// <inheritdoc/>
        public async Task AddLavorazioniFasiTipoTotaleAsync(LavorazioniFasiTipoTotaleDto arg)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            var entity = _mapper.DtoToTipoTotale(arg);
            await using var context = await contextFactory.CreateDbContextAsync();
            context.LavorazioniFasiTipoTotales.Add(entity);
            await context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task UpdateLavorazioniFasiTipoTotaleAsync(LavorazioniFasiTipoTotaleDto arg)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            await using var context = await contextFactory.CreateDbContextAsync();
            var lav = await context.LavorazioniFasiTipoTotales.FirstOrDefaultAsync(x => x.IdLavorazioneFaseTipoTotale.Equals(arg.IdLavorazioneFaseTipoTotale));
            if (lav != null)
            {
                lav.IdProceduraLavorazione = (int)arg.IdProceduraLavorazione!;
                lav.IdFase = (int)arg.IdFase!;
                lav.IdTipologiaTotale = (int)arg.IdTipologiaTotale!;
                await context.SaveChangesAsync();
            }
        }
    }
}
