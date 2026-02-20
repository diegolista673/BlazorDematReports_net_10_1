using AutoMapper;
using AutoMapper.QueryableExtensions;
using BlazorDematReports.Core.Application;
using BlazorDematReports.Core.Application.Dto;
using BlazorDematReports.Core.Interfaces.IDataService;
using ClosedXML.Excel;
using Entities.Helpers;
using Entities.Models;
using Entities.Models.DbApplication;
using FastMember;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Globalization;

namespace BlazorDematReports.Core.Services.DataService
{
    /// <summary>
    /// ServiceProduzioneOperatori per la gestione della produzione operatori e delle relative operazioni sui dati
    /// </summary>
    public class ServiceProduzioneOperatori : ServiceBase<ProduzioneOperatori>, IServiceProduzioneOperatori
    {
        /// <summary>
        /// Inizializza una nuova istanza del servizio per la gestione della produzione operatori.
        /// </summary>
        /// <param name="mapper">Mapper per conversioni tra entità e DTO.</param>
        /// <param name="configUser">Configurazione utente per controllo autorizzazioni.</param>
        /// <param name="contextFactory">Factory per la creazione di contesti database.</param>
        /// <param name="logger">Logger per registrare operazioni e errori.</param>
        public ServiceProduzioneOperatori(IMapper mapper, ConfigUser configUser, IDbContextFactory<DematReportsContext> contextFactory, ILogger<ServiceProduzioneOperatori> logger)
            : base(contextFactory, logger, mapper, configUser)
        {
        }

        /// <summary>
        /// Restituisce tutti i record di produzione operatori con le relative procedure di lavorazione.
        /// </summary>
        /// <returns>Lista completa dei record di produzione operatori.</returns>
        public async Task<List<ProduzioneOperatori>> GetProduzioneOperatoriAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            return await FindAll().Include(x => x.IdProceduraLavorazioneNavigation).ToListAsync();
        }

        /// <summary>
        /// Restituisce i record di produzione operatori per uno specifico operatore e data, con tutte le relazioni incluse.
        /// </summary>
        /// <param name="IdOperatore">ID dell'operatore per il filtro.</param>
        /// <param name="startDataLavorazione">Data di lavorazione per il filtro.</param>
        /// <returns>Lista di DTO della produzione operatori per l'operatore e data specificati.</returns>
        public async Task<List<ProduzioneOperatoriDto>> GetProduzioneOperatoriDtoAsync(int IdOperatore, DateTime startDataLavorazione)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            return await FindByCondition(x => x.IdOperatore.Equals(IdOperatore) && x.DataLavorazione.Date == startDataLavorazione.Date)
                            .Include(x => x.IdRepartiNavigation)
                            .Include(x => x.TipologieTotaliProduziones)
                            .Include(x => x.IdOperatoreNavigation)
                            .Include(x => x.IdTurnoNavigation)
                            .AsNoTracking()
                            .ProjectTo<ProduzioneOperatoriDto>(mapper.ConfigurationProvider)
                            .ToListAsync();
        }

        /// <summary>
        /// Verifica se esiste gi� un record di produzione operatori con i parametri specificati.
        /// </summary>
        /// <param name="produzioneOperatoriDto">DTO con i criteri di ricerca per la verifica duplicati.</param>
        /// <returns>True se il record esiste, False altrimenti.</returns>
        public async Task<bool> CheckProduzioneOperatori(ProduzioneOperatoriDto produzioneOperatoriDto)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            var rec = await FindByCondition(x => x.IdProceduraLavorazione == produzioneOperatoriDto.IdProceduraLavorazione &&
                                                    x.IdFaseLavorazione == produzioneOperatoriDto.IdFaseLavorazione &&
                                                    x.IdTurno == produzioneOperatoriDto.IdTurno &&
                                                    x.IdOperatore == produzioneOperatoriDto.IdOperatore &&
                                                    x.DataLavorazione.Date == produzioneOperatoriDto.DataLavorazione!.Value.Date).FirstOrDefaultAsync();

            if (rec != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Aggiorna il tempo di lavorazione in ore centesimali per un record di produzione operatori esistente.
        /// </summary>
        /// <param name="ProduzioneOperatoriDto">DTO con i dati per identificare e aggiornare il record.</param>
        /// <returns>Task asincrono per l'operazione di aggiornamento.</returns>
        public async Task UpdateProduzioneOperatoriAsync(ProduzioneOperatoriDto ProduzioneOperatoriDto)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using (var context = contextFactory.CreateDbContext())
            {

                var ProduzioneOperatori = await context.ProduzioneOperatoris.Where(x => x.IdProceduraLavorazione == ProduzioneOperatoriDto.IdProceduraLavorazione &
                                                                                        x.IdFaseLavorazione == ProduzioneOperatoriDto.IdFaseLavorazione &
                                                                                        x.IdTurno == ProduzioneOperatoriDto.IdTurno &
                                                                                        x.IdOperatore == ProduzioneOperatoriDto.IdOperatore &
                                                                                        x.DataLavorazione == ProduzioneOperatoriDto.DataLavorazione).FirstOrDefaultAsync();

                ProduzioneOperatori!.TempoLavOreCent = ProduzioneOperatoriDto.TempoLavOreCent;

                await context.SaveChangesAsync();

            }
        }

        #region PageGestioneOperatori

        /// <inheritdoc/>
        public async Task<List<ReportProduzioneCompleta>> GetReportProduzioneCompletaGiornalieraAsync(
            DateTime startDataLavorazione,
            DateTime endDataLavorazione,
            int idCentro)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            if (startDataLavorazione > endDataLavorazione)
                throw new ArgumentException("La data di inizio deve essere precedente o uguale alla data di fine", nameof(startDataLavorazione));

            var lstReportProduzioneCompleta = new List<ReportProduzioneCompleta>();

            try
            {
                using var context = contextFactory.CreateDbContext();

                string sql = @"
                    SELECT 
                        IdProduzioneSistema, p.DataLavorazione, p.IdOperatore, p.Operatore, p.OperatoreNonRiconosciuto,
                        p.AltraUtenza, p.IdFaseLavorazione, p.IdProceduraLavorazione, p.NomeProcedura, p.FaseLavorazione, 
                        p.TempoLavOreCent, p.Documenti, p.Fogli, p.Pagine, p.Scarti, p.PagineSenzaBianco, 
                        p.FlagDataReading, p.Esito, p.DescrizioneEsito, p.IdCentro
                    FROM (
                        -- Prima subquery: dati operatori (ore)
                        SELECT 
                            NULL as IdProduzioneSistema, 
                            CONVERT(date, po.dataLavorazione) as DataLavorazione, 
                            po.IdOperatore, 
                            op.Operatore, 
                            NULL as OperatoreNonRiconosciuto, 
                            AltraUtenza, 
                            po.IdFaseLavorazione, 
                            po.IdProceduraLavorazione, 
                            pl.NomeProcedura, 
                            fl.FaseLavorazione, 
                            SUM(TempoLavOreCent) as TempoLavOreCent, 
                            0 as Documenti, 
                            0 as Fogli, 
                            0 as Pagine, 
                            0 as Scarti, 
                            0 as PagineSenzaBianco,
                            ISNULL(lfdr.FlagDataReading, 0) as FlagDataReading, 
                            0 as Esito, 
                            '' as DescrizioneEsito, 
                            po.IdCentro
                        FROM ProduzioneOperatori as po
                        LEFT JOIN ProcedureLavorazioni as pl ON po.IdProceduraLavorazione = pl.IDProceduraLavorazione
                        LEFT JOIN FasiLavorazione as fl ON po.IdFaseLavorazione = fl.IdFaseLavorazione
                        LEFT JOIN Operatori as op ON po.IdOperatore = op.IDOperatore
                        LEFT JOIN LavorazioniFasiDataReading as lfdr ON po.IdFaseLavorazione = lfdr.IdFaseLavorazione 
                            AND po.IdProceduraLavorazione = lfdr.IdProceduraLavorazione
                        WHERE CONVERT(date, po.dataLavorazione) >= @startDate
                            AND CONVERT(date, po.dataLavorazione) <= @endDate
                            AND po.IdCentro = @idCentro
                        GROUP BY 
                            CONVERT(date, po.dataLavorazione), po.IdOperatore, op.Operatore, po.AltraUtenza, 
                            po.IdFaseLavorazione, po.IdProceduraLavorazione, pl.NomeProcedura, 
                            fl.FaseLavorazione, lfdr.FlagDataReading, po.IdCentro
                
                        UNION ALL
                
                        -- Seconda subquery: dati sistema (documenti, fogli, ecc.)
                        SELECT 
                            IdProduzioneSistema, 
                            DataLavorazione, 
                            ps.IdOperatore, 
                            Operatore, 
                            OperatoreNonRiconosciuto, 
                            NULL as AltraUtenza, 
                            ps.IdFaseLavorazione, 
                            ps.IdProceduraLavorazione, 
                            pl.NomeProcedura, 
                            fl.FaseLavorazione, 
                            0.0 as TempoLavOreCent, 
                            Documenti, 
                            Fogli, 
                            Pagine, 
                            Scarti, 
                            PagineSenzaBianco,
                            lfdr.FlagDataReading, 
                            0 as Esito, 
                            '' as DescrizioneEsito, 
                            ps.IdCentro
                        FROM ProduzioneSistema as ps
                        LEFT JOIN ProcedureLavorazioni as pl ON ps.IdProceduraLavorazione = pl.IDProceduraLavorazione
                        LEFT JOIN FasiLavorazione as fl ON ps.IdFaseLavorazione = fl.IdFaseLavorazione
                        LEFT JOIN LavorazioniFasiDataReading as lfdr ON ps.IdFaseLavorazione = lfdr.IdFaseLavorazione 
                            AND ps.IdProceduraLavorazione = lfdr.IdProceduraLavorazione
                        WHERE CONVERT(date, ps.DataLavorazione) >= @startDate
                            AND CONVERT(date, ps.DataLavorazione) <= @endDate
                            AND (ps.IDCentro = @idCentro OR ps.idCentro = 5)
                    ) AS p
                    ORDER BY p.Operatore, p.IdFaseLavorazione, p.IdProceduraLavorazione";


                var parameters = new[]
                {
                    new SqlParameter("@startDate", SqlDbType.Date) { Value = startDataLavorazione.Date },
                    new SqlParameter("@endDate", SqlDbType.Date) { Value = endDataLavorazione.Date },
                    new SqlParameter("@idCentro", SqlDbType.Int) { Value = idCentro }
                };

                var lstReport = await context.Set<ReportProduzioneCompleta>()
                    .FromSqlRaw(sql, parameters)
                    .AsNoTracking()
                    .ToListAsync();

                // Raggruppamento dei dati per chiave composita
                var groupedReports = lstReport.GroupBy(
                    x => new { x.IdOperatore, x.IdProceduraLavorazione, x.IdFaseLavorazione, x.DataLavorazione })
                    .ToList();

                // Elaborazione dei report raggruppati
                foreach (var group in groupedReports)
                {
                    // Inizializza un nuovo report per ogni gruppo
                    var report = new ReportProduzioneCompleta();

                    // Unisce i dati di tutte le righe del gruppo
                    foreach (var item in group)
                    {
                        report = mapper.Map(item, report);
                    }

                    lstReportProduzioneCompleta.Add(report);
                }

                // Post-elaborazione dei report (gestione "altra utenza" e valori null)
                PostProcessReports(lstReportProduzioneCompleta);

                // Aggiunta degli esiti di lettura dati
                return await AddEsitoReportProduzioneCompleta(lstReportProduzioneCompleta, startDataLavorazione, endDataLavorazione);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Esegue la post-elaborazione dei report, gestendo le utenze alternative e impostando valori predefiniti
        /// </summary>
        /// <param name="reports">Lista dei report da processare</param>
        private void PostProcessReports(List<ReportProduzioneCompleta> reports)
        {
            // Elaborazione in due passi per evitare modifiche durante l'iterazione
            List<ReportProduzioneCompleta> reportsToRemove = new List<ReportProduzioneCompleta>();

            // Primo passo: identifica le utenze alternative e prepara le modifiche
            foreach (var report in reports)
            {
                if (report.AltraUtenza != null)
                {
                    var targetReport = reports.FirstOrDefault(x =>
                        x.Operatore == report.AltraUtenza &&
                        x.IdProceduraLavorazione == report.IdProceduraLavorazione &&
                        x.IdFaseLavorazione == report.IdFaseLavorazione &&
                        x.DataLavorazione?.Date == report.DataLavorazione?.Date);

                    if (targetReport != null)
                    {
                        var existingReport = reports.FirstOrDefault(x =>
                            x.Operatore == targetReport.Operatore &&
                            x.IdProceduraLavorazione == targetReport.IdProceduraLavorazione &&
                            x.IdFaseLavorazione == targetReport.IdFaseLavorazione &&
                            x.DataLavorazione?.Date == targetReport.DataLavorazione?.Date);

                        if (existingReport != null)
                        {
                            existingReport.Operatore = report.Operatore;
                            existingReport.TempoLavOreCent = report.TempoLavOreCent;
                            reportsToRemove.Add(report);
                        }
                    }
                }

                // Imposta valori predefiniti per tutti i campi nullable
                report.TempoLavOreCent ??= 0;
                report.Documenti ??= 0;
                report.Fogli ??= 0;
                report.Pagine ??= 0;
                report.Scarti ??= 0;
                report.PagineSenzaBianco ??= 0;
            }

            // Secondo passo: rimuovi i report identificati
            foreach (var reportToRemove in reportsToRemove)
            {
                reports.Remove(reportToRemove);
            }
        }

        private async Task<List<ReportProduzioneCompleta>> AddEsitoReportProduzioneCompleta(
            List<ReportProduzioneCompleta> lstReportProduzioneCompleta,
            DateTime startDate,
            DateTime endDate)
        {
            using var context = contextFactory.CreateDbContext();

            var aggiornamenti = await context.TaskDataReadingAggiornamentos
                .Where(x => x.DataInizioLavorazione.Date >= startDate.Date &&
                           x.DataFineLavorazione != null &&
                           x.DataFineLavorazione.Value.Date <= endDate.Date)
                .AsNoTracking()
                .ToListAsync();

            var filteredAggiornamenti = aggiornamenti
                .Where(x => x.DataFineLavorazione!.Value.Date == endDate.Date)
                .ToList();

            // Raggruppamento per lavorazione e selezione dell'aggiornamento con ID massimo
            var aggiornamentoByLavorazione = filteredAggiornamenti
                .GroupBy(x => new
                {
                    DataInizio = x.DataInizioLavorazione.Date,
                    IdLavorazione = x.IdLavorazione,
                    IdFase = x.IdFase,
                    Lavorazione = x.Lavorazione  // Includiamo anche Lavorazione nella chiave
                })
                .ToDictionary(
                    g => g.Key,
                    g => g.Where(x => x.IdAggiornamento == g.Max(i => i.IdAggiornamento)).ToList()
                );

            foreach (var report in lstReportProduzioneCompleta)
            {
                if (report.DataLavorazione == null || report.IdProceduraLavorazione == null || report.IdFaseLavorazione == null)
                    continue;

                // Cerchiamo tutti gli aggiornamenti che corrispondono ai criteri
                var matchingAggiornamenti = filteredAggiornamenti
                    .Where(x => x.DataInizioLavorazione.Date == report.DataLavorazione.Value.Date &&
                               x.IdLavorazione == report.IdProceduraLavorazione.Value &&
                               x.IdFase == report.IdFaseLavorazione.Value)
                    .GroupBy(x => x.Lavorazione)
                    .SelectMany(y => y.Where(z => z.IdAggiornamento == y.Max(i => i.IdAggiornamento)))
                    .ToList();

                // Applichiamo gli aggiornamenti trovati al report
                foreach (var aggiornamento in matchingAggiornamenti)
                {
                    report.Esito = Convert.ToInt32(aggiornamento.EsitoLetturaDato);
                    report.DescrizioneEsito = aggiornamento.DescrizioneEsito;
                }
            }

            return lstReportProduzioneCompleta;
        }





        /// <inheritdoc/>
        public async Task<List<ReportGiornalieroTotaliDedicati>> GetReportTotaliDedicatiPeriodoAsync(ReportAnnualeDto reportAnnualeDto)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            List<ReportGiornalieroTotaliDedicati> lstReportTotaliAnnuale = new List<ReportGiornalieroTotaliDedicati>();
            using (var context = contextFactory.CreateDbContext())
            {
                string sql = @"SELECT CONVERT(date,dataLavorazione) AS DataLavorazione,op.Operatore,pl.NomeProcedura as Lavorazione, fl.FaseLavorazione as FaseLavorazione ,tt.TipoTotale, Sum(ttp.totale) as Totale
                               FROM TipologieTotaliProduzione as ttp 
                               LEFT JOIN TipologieTotali as tt on ttp.IDTipologiaTotale = tt.IdTipoTotale
                               LEFT JOIN ProduzioneOperatori as po on ttp.IDProduzioneOperatore = po.IdProduzione 
                               LEFT JOIN ProcedureLavorazioni as pl on po.IdProceduraLavorazione = pl.IdProceduraLavorazione 
                               LEFT JOIN Operatori as op on po.IdOperatore = op.IDOperatore
                               LEFT JOIN FasiLavorazione as fl on po.IdFaseLavorazione = fl.IdFaseLavorazione
                               WHERE pl.Idcentro = {0} and CONVERT(date,dataLavorazione) >= {1} and CONVERT(date,dataLavorazione) <= {2}
                               GROUP BY po.DataLavorazione,tt.TipoTotale, Operatore, pl.NomeProcedura,fl.FaseLavorazione
                               ORDER BY po.DataLavorazione,op.Operatore";

                lstReportTotaliAnnuale = await context.Set<ReportGiornalieroTotaliDedicati>().FromSqlRaw(sql, reportAnnualeDto.IdCentro!,
                                                                                                              reportAnnualeDto.StartDataLavorazione!,
                                                                                                              reportAnnualeDto.EndDataLavorazione!).ToListAsync();
            }

            return lstReportTotaliAnnuale;
        }

        /// <summary>
        /// Genera un report di produzione in ore aggregato per mese per un anno specifico.
        /// </summary>
        /// <param name="reportAnnualeDto">DTO contenente i parametri per il filtro (anno, fase, procedura, centro).</param>
        /// <returns>Lista di report annuali con totali ore-uomo e FTE per mese.</returns>
        private async Task<List<ReportAnnuale>> GetReportProduzioneOreAnnualeAsync(ReportAnnualeDto reportAnnualeDto)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            List<ReportAnnuale> lstReportAnnuale = new List<ReportAnnuale>();
            using (var context = contextFactory.CreateDbContext())
            {

                string sql = @"SELECT month(DataLavorazione) AS Mese, round(SUM(TempoLavOreCent), 2) as TotaleOreUomo, 
                               round((SUM(TempoLavOreCent) / 160), 1) as FteMese,  YEAR([DataLavorazione]) AS Anno 
                               FROM ProduzioneOperatori as pro 
                               LEFT JOIN ProcedureLavorazioni as pl on pro.IdProceduraLavorazione = pl.IdproceduraLavorazione 
                               WHERE pro.IdFaseLavorazione = {0} And pro.IdProceduraLavorazione = {1} and YEAR(DataLavorazione) = {2} and pl.Idcentro = {3} 
                               GROUP BY MONTH([DataLavorazione]), YEAR([DataLavorazione]),datename(month, DataLavorazione) 
                               ORDER BY YEAR([DataLavorazione]) DESC, MONTH([DataLavorazione])";

                lstReportAnnuale = await context.Set<ReportAnnuale>().FromSqlRaw(sql, reportAnnualeDto.IdFaseLavorazione!,
                                                                                      reportAnnualeDto.IdProceduraLavorazione!,
                                                                                      reportAnnualeDto.Anno!,
                                                                                      reportAnnualeDto.IdCentro!).ToListAsync();
            }


            return lstReportAnnuale;

        }

        /// <summary>
        /// Genera un report di produzione sistema aggregato per mese per un anno specifico con totali documenti, fogli, pagine e scarti.
        /// </summary>
        /// <param name="reportAnnualeDto">DTO contenente i parametri per il filtro (anno, fase, procedura, centro).</param>
        /// <returns>Lista di report annuali sistema con totali mensili di produzione.</returns>
        private async Task<List<ReportAnnualeSistema>> GetReportProduzioneSistemaAnnualeAsync(ReportAnnualeDto reportAnnualeDto)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            List<ReportAnnualeSistema> lstReportSistemaAnnuale = new List<ReportAnnualeSistema>();

            using (var context = contextFactory.CreateDbContext())
            {
                string sql = @"SELECT month(DataLavorazione) AS Mese, Sum(ps.Documenti) AS Documenti, Sum(ps.Fogli) AS Fogli, Sum(ps.Pagine) AS Pagine,
                               Sum(ps.Scarti) AS Scarti, Sum(pagineSenzaBianco) as pagineSenzaBianco
                               FROM ProduzioneSistema as ps
                               LEFT JOIN ProcedureLavorazioni as pl on ps.IdProceduraLavorazione = pl.IdproceduraLavorazione
                               LEFT JOIN FasiLavorazione as fl on ps.IdFaseLavorazione = fl.IdFaseLavorazione
                               WHERE ps.IdFaseLavorazione = {0} And pl.IdProceduraLavorazione = {1} and YEAR(DataLavorazione) = {2} and pl.Idcentro = {3}
                               GROUP BY MONTH([DataLavorazione]), YEAR([DataLavorazione]),datename(month, DataLavorazione)
                               ORDER BY YEAR([DataLavorazione]) DESC, MONTH([DataLavorazione])";

                lstReportSistemaAnnuale = await context.Set<ReportAnnualeSistema>().FromSqlRaw(sql, reportAnnualeDto.IdFaseLavorazione!,
                                                                                                    reportAnnualeDto.IdProceduraLavorazione!,
                                                                                                    reportAnnualeDto.Anno!,
                                                                                                    reportAnnualeDto.IdCentro!).ToListAsync();
            }


            return lstReportSistemaAnnuale;

        }


        /// <inheritdoc/>
        public async Task<List<ReportAnniSistema>> GetReportAnniPrecedentiAsync(ReportAnnualeDto reportAnnualeDto)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            List<ReportAnniSistema> lstReportAnniSistema = new List<ReportAnniSistema>();

            using (var context = contextFactory.CreateDbContext())
            {
                string sql = @"SELECT year(DataLavorazione) AS Anno, Sum(ps.Documenti) AS Documenti, Sum(ps.Fogli) AS Fogli, Sum(ps.Pagine) AS Pagine
                               FROM ProduzioneSistema as ps
                               LEFT JOIN ProcedureLavorazioni as pl on ps.IdProceduraLavorazione = pl.IdproceduraLavorazione
                               LEFT JOIN FasiLavorazione as fl on ps.IdFaseLavorazione = fl.IdFaseLavorazione
                               WHERE ps.IdFaseLavorazione = {0} And pl.IdProceduraLavorazione = {1} and pl.Idcentro = {2} and YEAR(DataLavorazione) >= YEAR(GETDATE()) - 4   
                               GROUP BY YEAR([DataLavorazione])
                               ORDER BY YEAR([DataLavorazione]) asc";

                lstReportAnniSistema = await context.Set<ReportAnniSistema>().FromSqlRaw(sql, reportAnnualeDto.IdFaseLavorazione!,
                                                                                              reportAnnualeDto.IdProceduraLavorazione!,
                                                                                              reportAnnualeDto.IdCentro!).ToListAsync();
            }

            // Gestione del caso in cui la lista � vuota o nulla
            if (lstReportAnniSistema == null || !lstReportAnniSistema.Any())
            {
                return new List<ReportAnniSistema>();
            }

            return lstReportAnniSistema;

        }

        /// <inheritdoc/>
        public async Task<List<ReportAnniSistema>> GetReportLast5YearsAsync(int IdProceduraLavorazione, int IdCentro)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            List<ReportAnniSistema> lstReportAnniSistema = new List<ReportAnniSistema>();

            using (var context = contextFactory.CreateDbContext())
            {
                string sql = @"SELECT year(DataLavorazione) AS Anno, ps.IdFaseLavorazione,Sum(ps.Documenti) AS Documenti, Sum(ps.Fogli) AS Fogli, Sum(ps.Pagine) AS Pagine
                               FROM ProduzioneSistema as ps
                               LEFT JOIN ProcedureLavorazioni as pl on ps.IdProceduraLavorazione = pl.IdproceduraLavorazione
                               LEFT JOIN FasiLavorazione as fl on ps.IdFaseLavorazione = fl.IdFaseLavorazione
                               WHERE pl.IdProceduraLavorazione = {0} and pl.Idcentro = {1} and YEAR(DataLavorazione) >= YEAR(GETDATE()) - 4   
                               GROUP BY YEAR([DataLavorazione]),ps.IdFaseLavorazione
                               ORDER BY YEAR([DataLavorazione]),ps.IdFaseLavorazione asc";

                lstReportAnniSistema = await context.Set<ReportAnniSistema>().FromSqlRaw(sql, IdProceduraLavorazione, IdCentro).ToListAsync();
            }

            // Gestione del caso in cui la lista � vuota o nulla
            if (lstReportAnniSistema == null || !lstReportAnniSistema.Any())
            {
                return new List<ReportAnniSistema>();
            }

            return lstReportAnniSistema;

        }





        /// <summary>
        /// Genera un report dei totali dedicati aggregato per mese per un anno specifico con tipologie totali.
        /// </summary>
        /// <param name="reportAnnualeDto">DTO contenente i parametri per il filtro (anno, fase, procedura, centro).</param>
        /// <returns>Lista di report annuali con totali dedicati per tipologia e mese.</returns>
        public async Task<List<ReportAnnualeTotaliDedicati>> GetReportTotaliDedicatiAsync(int idProceduraLavorazione, int anno, int idCentro)
        {
            QueryLoggingHelper.LogQueryExecution(logger);
            using var context = contextFactory.CreateDbContext();
            string sql = @"SET LANGUAGE Italian
                            SELECT YEAR(po.DataLavorazione) AS Anno,
                                   MONTH(po.DataLavorazione) AS Mese,
                                   DATENAME(month, DATEADD(month, MONTH(po.DataLavorazione) - 1, '2000-01-01')) AS MeseString,
                                   po.IdFaseLavorazione,
                                   tt.TipoTotale,
                                   SUM(ttp.Totale) AS Totale
                            FROM TipologieTotaliProduzione ttp
                            INNER JOIN TipologieTotali tt ON ttp.IDTipologiaTotale = tt.IdTipoTotale
                            INNER JOIN ProduzioneOperatori po ON ttp.IDProduzioneOperatore = po.IdProduzione
                            INNER JOIN ProcedureLavorazioni pl ON po.IdProceduraLavorazione = pl.IdProceduraLavorazione
                            WHERE po.IdProceduraLavorazione = {0}
                              AND YEAR(po.DataLavorazione) = {1}
                              AND pl.Idcentro = {2}
                            GROUP BY YEAR(po.DataLavorazione), MONTH(po.DataLavorazione), po.IdFaseLavorazione, tt.TipoTotale
                            ORDER BY YEAR(po.DataLavorazione) DESC, MONTH(po.DataLavorazione), po.IdFaseLavorazione";
            return await context.Set<ReportAnnualeTotaliDedicati>().FromSqlRaw(sql, idProceduraLavorazione, anno, idCentro).ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<List<ReportProduzioneSistema>> GetReportAnnualeAsync(ReportAnnualeDto reportAnnualeDto)
        {

            var lst = new List<ReportProduzioneSistema>();
            var reportOre = await GetReportProduzioneOreAnnualeAsync(reportAnnualeDto);
            var reportSistema = await GetReportProduzioneSistemaAnnualeAsync(reportAnnualeDto);

            for (int i = 1; i <= 12; i++)
            {
                var report = new ReportProduzioneSistema();
                report.Anno = reportAnnualeDto.Anno;
                report.Mese = i;
                //report.MeseString = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(i);
                report.MeseString = CultureInfo.GetCultureInfo("it-IT").DateTimeFormat.GetMonthName(i);
                report.TotaleOreUomo = reportOre.FirstOrDefault(x => x.Mese.Equals(i))?.TotaleOreUomo;
                report.FteMEse = reportOre.FirstOrDefault(x => x.Mese.Equals(i))?.FteMese;

                report.Documenti = reportSistema.FirstOrDefault(x => x.Mese.Equals(i))?.Documenti;
                report.Fogli = reportSistema.FirstOrDefault(x => x.Mese.Equals(i))?.Fogli;
                report.Pagine = reportSistema.FirstOrDefault(x => x.Mese.Equals(i))?.Pagine;
                report.Scarti = reportSistema.FirstOrDefault(x => x.Mese.Equals(i))?.Scarti;
                report.PagineSenzaBianco = reportSistema.FirstOrDefault(x => x.Mese.Equals(i))?.PagineSenzaBianco;

                lst.Add(report);

            }

            lst.OrderBy(x => x.Mese);
            return lst;
        }


        /// <summary>
        /// Genera report ore documenti annuale per centro selezionato
        /// </summary>
        /// <param name="anno"></param>
        /// <param name="IDProceduraLavorazione"></param>
        /// <param name="IDCentro"></param>
        /// <returns></returns>
        public async Task<List<ReportOreDocumenti>> GetReportProduzioneOreDocumentiAsync(int anno, int IDProceduraLavorazione, int IDCentro)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            List<ReportOreDocumenti> reportAnnuale = new List<ReportOreDocumenti>();
            using (var context = contextFactory.CreateDbContext())
            {

                string sql = @$"WITH Months AS (
                                    SELECT 1 AS Mese
                                    UNION ALL 
                                    SELECT Mese + 1 
                                    FROM Months 
                                    WHERE Mese < 12
                                ),
                                OreOperatori AS (
                                    SELECT 
                                           MONTH(pro.DataLavorazione) AS Mese,
                                           pro.IdFaseLavorazione, 
                                           SUM(pro.TempoLavOreCent) AS TotaleOreUomo
                                    FROM ProduzioneOperatori pro
                                    INNER JOIN ProcedureLavorazioni pl ON pro.IdProceduraLavorazione = pl.IdProceduraLavorazione
                                    WHERE YEAR(pro.DataLavorazione) = {{0}} -- Parametro Anno
                                        AND pro.IdProceduraLavorazione = {{1}} -- Parametro IdProceduraLavorazione
                                        AND pl.IdCentro = {{2}} -- Parametro IdCentro
                                    GROUP BY 
                                             MONTH(pro.DataLavorazione),
                                             pro.IdFaseLavorazione
                                ),
                                ProduzioneSistemaAgg AS (
                                    SELECT 
                                           MONTH(ps.DataLavorazione) AS Mese,
                                           ps.IdFaseLavorazione, 
                                           SUM(ps.Documenti) AS Documenti,
                                           SUM(ps.Fogli) AS Fogli,
                                           SUM(ps.Pagine) AS Pagine,
                                           SUM(ps.Scarti) AS Scarti,
                                           SUM(ps.PagineSenzaBianco) AS PagineSenzaBianco
                                    FROM ProduzioneSistema ps
                                    INNER JOIN ProcedureLavorazioni pl ON ps.IdProceduraLavorazione = pl.IdProceduraLavorazione
                                    WHERE YEAR(ps.DataLavorazione) = {{0}} -- Parametro Anno
                                        AND ps.IdProceduraLavorazione = {{1}} -- Parametro IdProceduraLavorazione
                                        AND pl.IdCentro = {{2}} -- Parametro IdCentro
                                    GROUP BY 
                                             MONTH(ps.DataLavorazione),
                                             ps.IdFaseLavorazione
                                ),
                                CombinedData AS (
                                    SELECT
                                        COALESCE(o.Mese, s.Mese) AS Mese,
                                        COALESCE(o.IdFaseLavorazione, s.IdFaseLavorazione) AS IdFaseLavorazione,
                                        o.TotaleOreUomo,
                                        s.Documenti,
                                        s.Fogli,
                                        s.Pagine,
                                        s.Scarti,
                                        s.PagineSenzaBianco
                                    FROM OreOperatori o
                                    FULL OUTER JOIN ProduzioneSistemaAgg s
                                        ON o.Mese = s.Mese
                                        AND o.IdFaseLavorazione = s.IdFaseLavorazione
                                )
                                SELECT
                                    {{0}} AS Anno, -- Parametro Anno
                                    m.Mese,
                                    DATENAME(month, DATEADD(month, m.Mese - 1, '2000-01-01')) AS MeseString,
                                    ISNULL(cd.IdFaseLavorazione, 0) as IdFaseLavorazione,
                                    ROUND(ISNULL(cd.TotaleOreUomo, 0), 2) AS TotaleOreUomo,
                                    ROUND(ISNULL(cd.TotaleOreUomo, 0) / 160.0, 1) AS FteMese,
                                    ISNULL(cd.Documenti, 0) AS Documenti,
                                    ISNULL(cd.Fogli, 0) AS Fogli,
                                    ISNULL(cd.Pagine, 0) AS Pagine,
                                    ISNULL(cd.Scarti, 0) AS Scarti,
                                    ISNULL(cd.PagineSenzaBianco, 0) AS PagineSenzaBianco
                                FROM Months m
                                LEFT JOIN CombinedData cd 
                                    ON cd.Mese = m.Mese
                                ORDER BY m.Mese, cd.IdFaseLavorazione
                                OPTION (MAXRECURSION 12);";

                reportAnnuale = await context.Set<ReportOreDocumenti>()
                    .FromSqlRaw(sql, anno, IDProceduraLavorazione, IDCentro)
                    .AsNoTracking()
                    .ToListAsync();
            }

            return reportAnnuale;
        }


        #endregion

        #region PageLavorazione(produzione giornaliera)

        /// <inheritdoc/>
        public async Task<List<ReportGiornalieroTotaliDedicati>> GetReportTotaliDedicatiGiornalieroAsync(ReportAnnualeDto reportAnnualeDto)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            if (reportAnnualeDto != null)
            {
                using (var context = contextFactory.CreateDbContext())
                {
                    string sql = @"SELECT po.DataLavorazione, op.Operatore, pl.NomeProcedura as Lavorazione, fl.FaseLavorazione, tt.TipoTotale,ttp.Totale
                               FROM TipologieTotaliProduzione as ttp 
                               LEFT JOIN TipologieTotali as tt on ttp.IDTipologiaTotale = tt.IdTipoTotale
                               LEFT JOIN ProduzioneOperatori as po on ttp.IDProduzioneOperatore = po.IdProduzione 
                               LEFT JOIN ProcedureLavorazioni as pl on po.IdProceduraLavorazione = pl.IdProceduraLavorazione 
                               LEFT JOIN FasiLavorazione as fl on po.IdFaseLavorazione = fl.IdFaseLavorazione 
                               Left join Operatori as op on po.IdOperatore = op.IDOperatore 
                               WHERE po.IdFaseLavorazione = {0} and po.IdProceduraLavorazione = {1} and convert(date,po.DataLavorazione) = {2} and pl.Idcentro = {3} ";

                    return await context.Set<ReportGiornalieroTotaliDedicati>().FromSqlRaw(sql, reportAnnualeDto.IdFaseLavorazione!,
                                                                                                reportAnnualeDto.IdProceduraLavorazione!,
                                                                                                reportAnnualeDto.DataLavorazione!,
                                                                                                reportAnnualeDto.IdCentro!).ToListAsync();
                }


            }
            else
            {
                return new List<ReportGiornalieroTotaliDedicati>();
            }

        }

        /// <summary>
        /// Crea un file Excel con i dati di produzione annua includendo report sistema e totali dedicati.
        /// </summary>
        /// <param name="lstReportAnnualeView">Dati del report di produzione annua.</param>
        /// <param name="lstReportTotaliDedicatiAnnualeView">Dati dei totali dedicati annuali.</param>
        /// <returns>MemoryStream contenente il file Excel generato.</returns>
        public MemoryStream CreateExcelProduzioneAnnua(IEnumerable<ReportOreDocumenti> lstReportAnnualeView, IEnumerable<ReportAnnualeTotaliDedicati> lstReportTotaliDedicatiAnnualeView)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            DataTable table1 = new DataTable();
            DataTable table2 = new DataTable();

            if (lstReportAnnualeView != null)
            {
                IEnumerable<ReportOreDocumenti> data = lstReportAnnualeView;

                using (var reader = ObjectReader.Create(data, "Anno", "MeseString", "Documenti", "Fogli", "Pagine", "PagineSenzaBianco", "Scarti", "TotaleOreUomo", "FteMese"))
                {
                    table1.Load(reader);
                }

            }


            if (lstReportTotaliDedicatiAnnualeView != null)
            {
                IEnumerable<ReportAnnualeTotaliDedicati> data2 = lstReportTotaliDedicatiAnnualeView;

                using (var reader = ObjectReader.Create(data2, "Anno", "MeseString", "TipoTotale", "Totale"))
                {
                    table2.Load(reader);
                }

            }


            using (XLWorkbook wb = new XLWorkbook())
            {
                wb.Worksheets.Add("ProduzioneAnnua");
                wb.Worksheet(1).Cell(1, 1).InsertTable(table1);

                int row = wb.Worksheet(1).RowsUsed().Count();

                wb.Worksheet(1).Cell(row + 2, 1).InsertTable(table2);

                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return stream;
                }
            }
        }

        /// <summary>
        /// Crea un file Excel con i dati di produzione giornaliera completa includendo report principale e totali dedicati.
        /// </summary>
        /// <param name="lstReportGiornalieroView">Dati del report di produzione giornaliera.</param>
        /// <param name="lstReportTotaliDedicatiGiornalieraView">Dati dei totali dedicati giornalieri.</param>
        /// <returns>MemoryStream contenente il file Excel generato.</returns>
        public MemoryStream CreateExcelProduzioneGiornalieraCompleta(IEnumerable<ReportProduzioneCompleta> lstReportGiornalieroView, IEnumerable<ReportGiornalieroTotaliDedicati>? lstReportTotaliDedicatiGiornalieraView)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            DataTable table1 = new DataTable();
            DataTable table2 = new DataTable();

            if (lstReportGiornalieroView != null)
            {
                IEnumerable<ReportProduzioneCompleta> data = lstReportGiornalieroView;
                if (table1 != null)
                {
                    using (var reader = ObjectReader.Create(data, "DataLavorazione", "Operatore", "OperatoreNonRiconosciuto", "NomeProcedura", "FaseLavorazione", "TempoLavOreCent", "Documenti", "Fogli", "Pagine", "Scarti", "PagineSenzaBianco", "IdOperatore", "IdProceduraLavorazione", "IdFaseLavorazione", "FlagDataReading", "Esito", "DescrizioneEsito"))
                    {
                        table1.Load(reader);
                    }
                }
            }


            if (lstReportTotaliDedicatiGiornalieraView != null)
            {
                IEnumerable<ReportGiornalieroTotaliDedicati> data2 = lstReportTotaliDedicatiGiornalieraView;
                if (table2 != null)
                {
                    using (var reader = ObjectReader.Create(data2, "DataLavorazione", "Operatore", "Lavorazione", "FaseLavorazione", "TipoTotale", "Totale"))
                    {
                        table2.Load(reader);
                    }
                }
            }


            using (XLWorkbook wb = new XLWorkbook())
            {
                wb.Worksheets.Add("ProduzioneGiornaliera");
                wb.Worksheet(1).Cell(1, 1).InsertTable(table1);


                int row = wb.Worksheet(1).RowsUsed().Count();

                wb.Worksheet(1).Cell(row + 2, 1).InsertTable(table2);

                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return stream;
                }
            }

        }

        /// <summary>
        /// Aggiunge un nuovo record di produzione operatori con le relative tipologie totali associate.
        /// </summary>
        /// <param name="produzioneOperatoriDto">DTO del record di produzione operatori da aggiungere.</param>
        /// <param name="totaliProduzioneDto">Lista delle tipologie totali da associare al record.</param>
        /// <returns>Task asincrono per l'operazione di inserimento.</returns>
        public async Task AddProduzioneOperatoriAsync(ProduzioneOperatoriDto produzioneOperatoriDto, List<TipologieTotaliDto> totaliProduzioneDto)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            ProduzioneOperatori ProduzioneOperatori = mapper.Map<ProduzioneOperatori>(produzioneOperatoriDto);

            foreach (var el in totaliProduzioneDto)
            {
                if (el.Totale > 0)
                {
                    TipologieTotaliProduzione tipo = new TipologieTotaliProduzione();
                    tipo.IdproduzioneOperatore = ProduzioneOperatori.IdProduzione;
                    tipo.Totale = el.Totale;
                    tipo.IdtipologiaTotale = el.IdTipoTotale;
                    tipo.TipoTotale = el.TipoTotale!;
                    ProduzioneOperatori.TipologieTotaliProduziones.Add(tipo);
                }
            }

            await CreateAsync(ProduzioneOperatori);
        }

        /// <summary>
        /// Elimina un record di produzione operatori specificato dall'ID.
        /// </summary>
        /// <param name="id">ID del record di produzione operatori da eliminare.</param>
        /// <returns>Task asincrono per l'operazione di eliminazione.</returns>
        public async Task DeleteProduzioneOperatoriAsync(int id)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            await DeleteAsync(id);
        }

        #endregion
    }
}
