using AutoMapper;
using AutoMapper.QueryableExtensions;
using BlazorDematReports.Application;
using BlazorDematReports.Dto;
using BlazorDematReports.Interfaces.IDataService;
using Entities.Helpers;
using Entities.Models;
using Entities.Models.DbApplication;
using Microsoft.EntityFrameworkCore;

namespace BlazorDematReports.Services.DataService
{
    /// <summary>
    /// ServiceProduzioneSistema per la gestione della produzione di sistema e delle relative operazioni sui dati.
    /// </summary>
    public class ServiceProduzioneSistema : ServiceBase<ProduzioneSistema>, IServiceProduzioneSistema
    {

        // Compiled queries (nessun DefaultIfEmpty per evitare DateTime.Min se assente)
        private static readonly Func<DematReportsContext, int, int, DateTime?> _getMinDateCompiled =
            EF.CompileQuery((DematReportsContext ctx, int idProc, int idFase) =>
                ctx.ProduzioneSistemas
                   .Where(x => x.IdProceduraLavorazione == idProc && x.IdFaseLavorazione == idFase)
                   .Select(x => (DateTime?)x.DataLavorazione)
                   .Min());

        private static readonly Func<DematReportsContext, int, int, DateTime?> _getMaxDateCompiled =
            EF.CompileQuery((DematReportsContext ctx, int idProc, int idFase) =>
                ctx.ProduzioneSistemas
                   .Where(x => x.IdProceduraLavorazione == idProc && x.IdFaseLavorazione == idFase)
                   .Select(x => (DateTime?)x.DataLavorazione)
                   .Max());

        /// <summary>
        /// ServiceProduzioneSistema per la gestione della produzione di sistema e delle relative operazioni sui dati.
        /// </summary>
        /// <param name="mapper"></param>
        /// <param name="configUser"></param>
        /// <param name="contextFactory"></param>
        /// <param name="logger"></param>
        public ServiceProduzioneSistema(IMapper mapper, ConfigUser configUser, IDbContextFactory<DematReportsContext> contextFactory, ILogger<ServiceProduzioneSistema> logger) : base(contextFactory, logger, mapper, configUser)
        {
        }

        /// <inheritdoc/>
        public async Task<bool> CheckProduzioneSistemaOperatoreAsync(ProduzioneSistemaDto produzioneSistemaDto)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            var rec = await context.ProduzioneSistemas
                .Where(x => x.IdProceduraLavorazione == produzioneSistemaDto.IdProceduraLavorazione &&
                            x.IdFaseLavorazione == produzioneSistemaDto.IdFaseLavorazione &&
                            x.IdOperatore == produzioneSistemaDto.IdOperatore &&
                            x.DataLavorazione.Date == produzioneSistemaDto.DataLavorazione!.Value.Date)
                .FirstOrDefaultAsync();
            return rec != null;
        }

        /// <inheritdoc/>
        public async Task<List<ProduzioneSistema>> GetProduzioneSistemaByOperAndDate(int idOperatore, DateTime startDate)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            return await context.ProduzioneSistemas
                .Where(x => x.DataLavorazione == startDate.Date && x.IdOperatore == idOperatore)
                .Include(x => x.IdProceduraLavorazioneNavigation)
                .Include(x => x.IdFaseLavorazioneNavigation)
                .Include(x => x.IdCentroNavigation)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<List<ProduzioneSistema>> GetProduzioneSistemaByDateAsync(ReportAnnualeDto reportAnnualeDto)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            return await context.ProduzioneSistemas
                .Include(x => x.IdProceduraLavorazioneNavigation)
                .Include(x => x.IdFaseLavorazioneNavigation)
                .Include(x => x.IdCentroNavigation)
                .Where(x => x.DataLavorazione == reportAnnualeDto.StartDataLavorazione && x.IdCentro == reportAnnualeDto.IdCentro)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<List<ProduzioneSistema>> GetProduzioneSistemaAsync(ReportAnnualeDto reportAnnualeDto)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            if (reportAnnualeDto != null)
            {
                //chiamata singola
                return await context.ProduzioneSistemas
                    .Include(x => x.IdProceduraLavorazioneNavigation)
                    .Include(x => x.IdFaseLavorazioneNavigation)
                    .Where(x => x.DataAggiornamento == DateTime.Today &&
                                x.IdProceduraLavorazione.Equals(reportAnnualeDto.IdProceduraLavorazione))
                    .AsNoTracking()
                    .ToListAsync();
            }
            else
            {
                //chiamata multipla
                return await context.ProduzioneSistemas
                    .Include(x => x.IdProceduraLavorazioneNavigation)
                    .Include(x => x.IdFaseLavorazioneNavigation)
                    .Where(x => x.DataAggiornamento == DateTime.Today)
                    .AsNoTracking()
                    .ToListAsync();
            }
        }

        /// <inheritdoc/>
        public async Task UpdateProduzioneSistemaAsync(ProduzioneSistemaDto arg)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            var produzioneSistema = await context.ProduzioneSistemas
                .Where(x => x.IdProceduraLavorazione == arg.IdProceduraLavorazione &&
                            x.IdFaseLavorazione == arg.IdFaseLavorazione &&
                            x.IdOperatore == arg.IdOperatore &&
                            x.DataLavorazione == arg.DataLavorazione)
                .FirstOrDefaultAsync();

            if (produzioneSistema != null)
            {
                produzioneSistema.Documenti = arg.Documenti;
                produzioneSistema.Fogli = arg.Fogli;
                produzioneSistema.Pagine = arg.Pagine;
                produzioneSistema.PagineSenzaBianco = arg.PagineSenzaBianco;
                produzioneSistema.Scarti = arg.Scarti;
                produzioneSistema.FlagInserimentoAuto = false;
                produzioneSistema.FlagInserimentoManuale = true;
                await context.SaveChangesAsync();
            }
        }

        /// <inheritdoc/>
        public async Task AddProduzioneSistemaAsync(ProduzioneSistemaDto produzioneSistemaDto)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            ProduzioneSistema produzioneSistema = mapper.Map<ProduzioneSistema>(produzioneSistemaDto);
            using var context = contextFactory.CreateDbContext();
            context.ProduzioneSistemas.Add(produzioneSistema);
            await context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task DeleteProduzioneSistemaAsync(int idProduzione)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            var entity = await context.ProduzioneSistemas.FindAsync(idProduzione);
            if (entity != null)
            {
                context.ProduzioneSistemas.Remove(entity);
                await context.SaveChangesAsync();
            }
        }

        /// <inheritdoc/>
        public async Task<List<ReportProduzioneCompleta>> GetReportProduzioneInseritaManualeAsync(ProduzioneSistemaDto produzioneSistemaDto)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            string sql = @"SELECT IdProduzioneSistema,DataLavorazione, ps.IdOperatore, Operatore, OperatoreNonRiconosciuto, ps.IdFaseLavorazione, ps.IdProceduraLavorazione, pl.NomeProcedura, fl.FaseLavorazione, Documenti,Fogli, Pagine, Scarti, PagineSenzaBianco, null as TempoLavOreCent
                           FROM [ProduzioneGed].[dbo].[ProduzioneSistema] as ps
                           left join ProcedureLavorazioni as pl on ps.IdProceduraLavorazione = pl.IDProceduraLavorazione
                           left join FasiLavorazione as fl on ps.IdFaseLavorazione = fl.IdFaseLavorazione
                           where CONVERT(date,dataLavorazione) = {0} and pl.IDProceduraLavorazione = {1}                          
                           order by IdProduzioneSistema";
            return await context.Set<ReportProduzioneCompleta>().FromSqlRaw(sql, produzioneSistemaDto.DataLavorazione!.Value.Date, produzioneSistemaDto.IdProceduraLavorazione).ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<List<ProduzioneSistemaDto>> GetProduzioneSistemaDtoAsync(int? IdOperatore, DateTime? startDataLavorazione)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            return await context.ProduzioneSistemas
                .Include(x => x.IdProceduraLavorazioneNavigation)
                .Include(x => x.IdFaseLavorazioneNavigation)
                .Include(x => x.IdOperatoreNavigation)
                .Where(x => x.IdOperatore.Equals(IdOperatore) && x.DataLavorazione.Date == startDataLavorazione!.Value.Date)
                .AsNoTracking()
                .ProjectTo<ProduzioneSistemaDto>(mapper.ConfigurationProvider)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public Task<string?> GetPrimaDataInseritaAsync(int idProceduraLavorazione, int idFaseLavorazione)
        {
            QueryLoggingHelper.LogQueryExecution(logger);
            using var context = contextFactory.CreateDbContext();
            var minDate = _getMinDateCompiled(context, idProceduraLavorazione, idFaseLavorazione);
            return Task.FromResult(minDate?.ToShortDateString());
        }

        /// <inheritdoc/>
        public Task<string?> GetUltimaDataInseritaAsync(int idProceduraLavorazione, int idFaseLavorazione)
        {
            QueryLoggingHelper.LogQueryExecution(logger);
            using var context = contextFactory.CreateDbContext();
            var maxDate = _getMaxDateCompiled(context, idProceduraLavorazione, idFaseLavorazione);
            return Task.FromResult(maxDate?.ToShortDateString());
        }
    }
}
