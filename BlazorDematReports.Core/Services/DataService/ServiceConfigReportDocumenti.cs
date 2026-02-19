using AutoMapper;
using BlazorDematReports.Core.Application;
using BlazorDematReports.Core.Interfaces.IDataService;
using ClosedXML.Excel;
using Entities.Helpers;
using Entities.Models;
using Entities.Models.DbApplication;
using FastMember;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;


namespace BlazorDematReports.Core.Services.DataService
{
    /// <summary>
    /// Servizio che gestisce la configurazione e la generazione di report e grafici per documenti lavorati.
    /// Fornisce funzionalità per l'aggregazione dei dati di produzione, la visualizzazione grafica
    /// e l'esportazione in formato Excel dei report generati.
    /// </summary>
    public class ServiceConfigReportDocumenti : ServiceBase<ConfigReportDocumenti>, IServiceConfigReportDocumenti
    {

        /// <summary>
        /// Inizializza una nuova istanza del servizio di configurazione report documenti.
        /// </summary>
        /// <param name="mapper">Servizio di mapping tra entità e DTO utilizzato per trasformare i dati tra diversi modelli.</param>
        /// <param name="configUser">Configurazione dell'utente corrente con informazioni su permessi e centro di appartenenza.</param>
        /// <param name="contextFactory">Factory per la creazione di istanze del contesto database DematReports.</param>
        /// <param name="logger">Logger per il tracking delle operazioni.</param>
        public ServiceConfigReportDocumenti(IMapper mapper, ConfigUser configUser, IDbContextFactory<DematReportsContext> contextFactory, ILogger<ServiceConfigReportDocumenti> logger)
            : base(contextFactory, logger, mapper, configUser)
        {
        }

        /// <inheritdoc/>
        /// <summary>
        /// Ottiene l'elenco delle lavorazioni presenti nel grafico finale documenti, raggruppate per procedura cliente,
        /// in base al periodo di tempo specificato e al centro di lavorazione.
        /// </summary>
        /// <param name="startDate">Data di inizio del periodo di report</param>
        /// <param name="endDate">Data di fine del periodo di report</param>
        /// <param name="idCentro">ID del centro di lavorazione</param>
        /// <returns>Lista dei report documenti aggregati per procedura cliente</returns>
        public async Task<List<ReportDocumenti>> GetGraficoDocumenti(DateTime startDate, DateTime endDate, int idCentro)
        {

            QueryLoggingHelper.LogQueryExecution(logger);

            // Valida i parametri di input
            if (startDate > endDate)
                throw new ArgumentException("La data di inizio deve essere precedente alla data di fine");

            using var context = contextFactory.CreateDbContext();

            // Query ottimizzata con parametrizzazione SQL e join appropriati
            string sql = @"
                SELECT  
                    pc.ProceduraCliente AS NomeProcedura, 
                    SUM(ISNULL(ps.Documenti, 0)) AS Documenti,
                    SUM(ISNULL(ps.Fogli, 0)) AS Fogli, 
                    SUM(ISNULL(ps.Pagine, 0)) AS Pagine, 
                    SUM(ISNULL(ps.PagineSenzaBianco, 0)) AS PagineSenzaBianco, 
                    SUM(ISNULL(ps.Scarti, 0)) AS Scarti, 
                    ps.IdCentro
                FROM ProduzioneSistema AS ps
                INNER JOIN ProcedureLavorazioni AS pl ON ps.IdProceduraLavorazione = pl.IDProceduraLavorazione
                INNER JOIN FasiLavorazione AS fl ON ps.IdFaseLavorazione = fl.IdFaseLavorazione
                INNER JOIN ProcedureCliente AS pc ON pl.IDproceduraCliente = pc.IDproceduraCliente
                WHERE EXISTS (
                    SELECT 1
                    FROM LavorazioniFasiDataReading AS l 
                    WHERE pl.IdProceduraLavorazione = l.IdProceduraLavorazione 
                      AND fl.IdFaseLavorazione = l.IdFaseLavorazione 
                      AND l.FlagGraficoDocumenti = 1
                )
                AND ps.IDCentro = @idCentro 
                AND CONVERT(date, ps.DataLavorazione) >= @startDate
                AND CONVERT(date, ps.DataLavorazione) <= @endDate
                GROUP BY ps.IdCentro, pc.ProceduraCliente
                ORDER BY pc.ProceduraCliente";


            var parameters = new[]
            {
                new SqlParameter("@idCentro", SqlDbType.Int) { Value = idCentro },
                new SqlParameter("@startDate", SqlDbType.Date) { Value = startDate.Date },
                new SqlParameter("@endDate", SqlDbType.Date) { Value = endDate.Date }
            };


            var risultati = await context.Set<ReportDocumenti>()
                .FromSqlRaw(sql, parameters)
                .AsNoTracking()
                .ToListAsync();

            return risultati;
        }

        /// <inheritdoc/>
        public List<ReportDocumenti> GetGraficoDocumentiModified(List<ReportDocumenti> lst, double perc)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            var altro = new ReportDocumenti();
            altro.NomeProcedura = "ALTRO";
            altro.Documenti = 0;

            var elenco = new List<ReportDocumenti>();

            int TotaleDocumenti = lst.Sum(x => (int)x.Documenti!);

            //per ogni lavorazione al di sotto della % param del totale Documenti 
            //la aggiungo ad un oggetto unico sommando i doc e la tolgo dall'elenco
            List<ReportDocumenti> elencoFinale = lst.ToList();

            foreach (var el in elencoFinale.ToList())
            {
                if (el.Documenti > 0)
                {
                    var valorePerc = (double)(100 * (double)el.Documenti / TotaleDocumenti);
                    if (valorePerc < perc)
                    {
                        altro.Documenti = altro.Documenti + el.Documenti;
                        elencoFinale.RemoveAll(x => x.NomeProcedura == el.NomeProcedura);
                    }
                    else
                    {
                        var rep = new ReportDocumenti();

                        rep.NomeProcedura = el.NomeProcedura;
                        rep.Documenti = el.Documenti;
                        elenco.Add(rep);
                    }

                }

            }


            if (altro.Documenti > 0)
            {
                elenco.Add(altro);
            }

            return elenco;
        }

        /// <inheritdoc/>
        public async Task<List<ReportFogli>> GetGraficoFogliScansionati(DateTime startDate, DateTime endDate, int idCentro)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            // Validazione parametri di input
            if (startDate > endDate)
                throw new ArgumentException("La data di inizio deve essere precedente alla data di fine");

            using var context = contextFactory.CreateDbContext();

            // Query ottimizzata con parametrizzazione SQL e join appropriati
            string sql = @"
                SELECT 
                    pc.ProceduraCliente AS NomeProcedura,
                    SUM(ISNULL(ps.Fogli, 0)) AS Fogli
                FROM ProduzioneSistema AS ps
                INNER JOIN ProcedureLavorazioni AS pl ON ps.IdProceduraLavorazione = pl.IDProceduraLavorazione
                INNER JOIN FasiLavorazione AS fl ON ps.IdFaseLavorazione = fl.IdFaseLavorazione
                INNER JOIN ProcedureCliente AS pc ON pl.IDproceduraCliente = pc.IDproceduraCliente
                WHERE 
                    CONVERT(date, ps.DataLavorazione) >= @startDate 
                    AND CONVERT(date, ps.DataLavorazione) <= @endDate 
                    AND ps.IdCentro = @idCentro 
                    AND ps.IdFaseLavorazione = 4
                GROUP BY pc.ProceduraCliente
                ORDER BY pc.ProceduraCliente";

            // Creazione di parametri SQL per prevenire SQL injection e migliorare la cache del piano di esecuzione
            var parameters = new[]
            {
                new SqlParameter("@startDate", SqlDbType.Date) { Value = startDate.Date },
                new SqlParameter("@endDate", SqlDbType.Date) { Value = endDate.Date },
                new SqlParameter("@idCentro", SqlDbType.Int) { Value = idCentro }
            };

            // Esecuzione ottimizzata della query con disabilitazione del tracking
            var risultati = await context.Set<ReportFogli>()
                .FromSqlRaw(sql, parameters)
                .AsNoTracking() // Migliora le performance per dati di sola lettura
                .ToListAsync();

            return risultati;
        }

        /// <inheritdoc/>
        public List<ReportFogli> GetGraficoFogliModified(List<ReportFogli> lst, double perc)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            var altro = new ReportFogli();
            altro.NomeProcedura = "ALTRO";
            altro.Fogli = 0;

            var elenco = new List<ReportFogli>();

            int TotaleFogli = lst.Sum(x => (int)x.Fogli!);

            //per ogni lavorazione al di sotto della % param del totale Fogli 
            //la aggiungo ad un oggetto unico sommando i fogli e la tolgo dall'elenco

            List<ReportFogli> elencoFinale = lst.ToList();

            foreach (var el in elencoFinale.ToList())
            {

                //debug
                var a = el.NomeProcedura;
                //Debug.WriteLine(a);

                if (el.Fogli > 0)
                {
                    var valorePerc = (double)(100 * (double)el.Fogli / TotaleFogli);
                    if (valorePerc < perc)
                    {
                        altro.Fogli = altro.Fogli + el.Fogli;
                        elencoFinale.RemoveAll(x => x.NomeProcedura == el.NomeProcedura);
                    }
                    else
                    {
                        var rep = new ReportFogli();

                        rep.NomeProcedura = el.NomeProcedura;
                        rep.Fogli = el.Fogli;
                        elenco.Add(rep);
                    }

                }

            }


            if (altro.Fogli > 0)
            {
                elenco.Add(altro);
            }

            return elenco;
        }



        /// <summary>
        /// Ottiene il report dei clienti con dati aggregati di produzione e ore di lavoro
        /// </summary>
        /// <param name="startDate">Data di inizio periodo</param>
        /// <param name="endDate">Data di fine periodo</param>
        /// <param name="idCentro">ID del centro di lavorazione</param>
        /// <returns>Lista di dati aggregati per cliente, procedura e fase</returns>
        public async Task<List<ReportEsportazioneOreDocumenti>> GetReportClientiAsync(DateTime startDate, DateTime endDate, int idCentro)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            //Valida i parametri di input
            if (startDate > endDate)
                throw new ArgumentException("La data di inizio deve essere precedente alla data di fine");

            using var context = contextFactory.CreateDbContext();

            // Ottimizza la query utilizzando una CTE (Common Table Expression) per migliorare le prestazioni
            string sql = @"
            WITH OreOperatori AS (
                SELECT 
                    pc.ProceduraCliente,
                    pl.NomeProcedura, 
                    fl.FaseLavorazione, 
                    SUM(ISNULL(TempoLavOreCent, 0)) AS TempoLavOreCent,
                    pl.IdCentro
                FROM ProduzioneOperatori AS po
                INNER JOIN ProcedureLavorazioni AS pl ON po.IdProceduraLavorazione = pl.IDProceduraLavorazione
                INNER JOIN FasiLavorazione AS fl ON po.IdFaseLavorazione = fl.IdFaseLavorazione
                INNER JOIN ProcedureCliente AS pc ON pl.IDproceduraCliente = pc.IDproceduraCliente
                WHERE CONVERT(date, po.DataLavorazione) >= @startDate 
                  AND CONVERT(date, po.DataLavorazione) <= @endDate 
                  AND pl.IDCentro = @idCentro
                GROUP BY pc.ProceduraCliente, pl.NomeProcedura, fl.FaseLavorazione, pl.IdCentro
            ),
            DatiProduzione AS (
                SELECT 
                    pc.ProceduraCliente,
                    pl.NomeProcedura, 
                    fl.FaseLavorazione,
                    SUM(ISNULL(Documenti, 0)) AS Documenti,
                    SUM(ISNULL(Fogli, 0)) AS Fogli,
                    SUM(ISNULL(Pagine, 0)) AS Pagine,
                    SUM(ISNULL(Scarti, 0)) AS Scarti,
                    SUM(ISNULL(PagineSenzaBianco, 0)) AS PagineSenzaBianco,
                    ps.IdCentro
                FROM ProduzioneSistema AS ps
                INNER JOIN ProcedureLavorazioni AS pl ON ps.IdProceduraLavorazione = pl.IDProceduraLavorazione
                INNER JOIN FasiLavorazione AS fl ON ps.IdFaseLavorazione = fl.IdFaseLavorazione
                INNER JOIN ProcedureCliente AS pc ON pl.IDproceduraCliente = pc.IDproceduraCliente
                WHERE CONVERT(date, ps.DataLavorazione) >= @startDate 
                  AND CONVERT(date, ps.DataLavorazione) <= @endDate 
                  AND ps.IDCentro = @idCentro
                GROUP BY pc.ProceduraCliente, pl.NomeProcedura, fl.FaseLavorazione, ps.IdCentro
            )
            SELECT 
                COALESCE(o.ProceduraCliente, d.ProceduraCliente) AS ProceduraCliente,
                COALESCE(o.NomeProcedura, d.NomeProcedura) AS NomeProcedura,
                COALESCE(o.FaseLavorazione, d.FaseLavorazione) AS FaseLavorazione,
                ISNULL(o.TempoLavOreCent, 0) AS TempoLavOreCent,
                ISNULL(d.Documenti, 0) AS Documenti,
                ISNULL(d.Fogli, 0) AS Fogli,
                ISNULL(d.Pagine, 0) AS Pagine,
                ISNULL(d.Scarti, 0) AS Scarti,
                ISNULL(d.PagineSenzaBianco, 0) AS PagineSenzaBianco,
                COALESCE(o.IdCentro, d.IdCentro) AS IdCentro
            FROM OreOperatori o
            FULL OUTER JOIN DatiProduzione d ON 
                o.ProceduraCliente = d.ProceduraCliente AND
                o.NomeProcedura = d.NomeProcedura AND
                o.FaseLavorazione = d.FaseLavorazione
            ORDER BY 
                COALESCE(o.ProceduraCliente, d.ProceduraCliente),
                COALESCE(o.NomeProcedura, d.NomeProcedura),
                COALESCE(o.FaseLavorazione, d.FaseLavorazione)";

            var parameters = new[]
            {
                new SqlParameter("@startDate", SqlDbType.Date) { Value = startDate.Date },
                new SqlParameter("@endDate", SqlDbType.Date) { Value = endDate.Date },
                new SqlParameter("@idCentro", SqlDbType.Int) { Value = idCentro }
            };

            var risultati = await context.Set<ReportEsportazioneOreDocumenti>()
                .FromSqlRaw(sql, parameters)
                .AsNoTracking() // Aggiunto per ottimizzare performance visto che i dati sono di sola lettura
                .ToListAsync();

            return risultati;
        }


        /// <inheritdoc/>
        /// <summary>
        /// Ottiene dati aggregati delle ore lavorate per cliente, raggruppati per procedura cliente.
        /// </summary>
        /// <param name="startDate">Data di inizio periodo</param>
        /// <param name="endDate">Data di fine periodo</param>
        /// <param name="idCentro">ID del centro di lavorazione</param>
        /// <returns>Lista di report con ore lavorate aggregate per cliente</returns>
        public async Task<List<ReportEsportazioneOreDocumenti>> GetGraficoOreAsync(DateTime startDate, DateTime endDate, int idCentro)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            // Valida i parametri di input
            if (startDate > endDate)
                throw new ArgumentException("La data di inizio deve essere precedente alla data di fine");

            using var context = contextFactory.CreateDbContext();

            string sql = @"
                SELECT 
                    pc.ProceduraCliente, 
                    NULL as NomeProcedura, 
                    NULL as FaseLavorazione, 
                    SUM(ISNULL(po.TempoLavOreCent, 0)) as TempoLavOreCent, 
                    0 as Documenti, 
                    0 as Fogli, 
                    0 as Pagine, 
                    0 as Scarti, 
                    0 as PagineSenzaBianco,
                    pl.IdCentro
                FROM ProduzioneOperatori as po
                INNER JOIN ProcedureLavorazioni as pl ON po.IdProceduraLavorazione = pl.IDProceduraLavorazione
                INNER JOIN ProcedureCliente as pc ON pl.IDproceduraCliente = pc.IDproceduraCliente
                WHERE 
                    CONVERT(date, po.DataLavorazione) >= @startDate 
                    AND CONVERT(date, po.DataLavorazione) <= @endDate 
                    AND pl.IDCentro = @idCentro 
                    AND (pc.ProceduraCliente <> 'POSTEL' OR pc.ProceduraCliente IS NULL)
                GROUP BY pc.ProceduraCliente, pl.IdCentro
                ORDER BY pc.ProceduraCliente";


            var parameters = new[]
            {
                new SqlParameter("@startDate", SqlDbType.Date) { Value = startDate.Date },
                new SqlParameter("@endDate", SqlDbType.Date) { Value = endDate.Date },
                new SqlParameter("@idCentro", SqlDbType.Int) { Value = idCentro }
            };


            var risultati = await context.Set<ReportEsportazioneOreDocumenti>()
                .FromSqlRaw(sql, parameters)
                .AsNoTracking()
                .ToListAsync();

            return risultati;
        }



        /// <inheritdoc/>
        public List<ReportEsportazioneOreDocumenti> GetGraficoOreModified(List<ReportEsportazioneOreDocumenti> lst, double perc)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            var altro = new ReportEsportazioneOreDocumenti();
            altro.ProceduraCliente = "ALTRO";
            altro.TempoLavOreCent = 0;

            var elenco = new List<ReportEsportazioneOreDocumenti>();

            double TempoLavOreCent = lst.Sum(x => (double)x.TempoLavOreCent);

            //per ogni lavorazione al di sotto della % param del totale Documenti 
            //la aggiungo ad un oggetto unico sommando i doc e la tolgo dall'elenco

            List<ReportEsportazioneOreDocumenti> elencoFinale = lst.ToList();

            foreach (var el in elencoFinale.ToList())
            {
                if (el.TempoLavOreCent > 0)
                {

                    var valorePerc = (double)(100 * el.TempoLavOreCent / (double)TempoLavOreCent);

                    if (valorePerc < perc)
                    {
                        altro.TempoLavOreCent = altro.TempoLavOreCent + el.TempoLavOreCent;
                        elencoFinale.RemoveAll(x => x.ProceduraCliente == el.ProceduraCliente);
                    }
                    else
                    {
                        var rep = new ReportEsportazioneOreDocumenti();
                        //if (el.PROCEDURA_CLIENTE.Contains("POSTE_ITALIANE_"))
                        //{
                        //    el.PROCEDURA_CLIENTE = el.PROCEDURA_CLIENTE.Replace("POSTE_ITALIANE_", "");
                        //}
                        rep.ProceduraCliente = el.ProceduraCliente;
                        rep.TempoLavOreCent = el.TempoLavOreCent;
                        elenco.Add(rep);
                    }

                }

            }

            if (altro.TempoLavOreCent > 0)
            {
                elenco.Add(altro);
            }

            return elenco;
        }

        /// <inheritdoc/>
        public async Task<List<ReportChartStackedLineFogli>> GetGraficoFogliScansionatiPeriodo(DateTime startDate, DateTime endDate, int idCentro)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            // Validazione parametri di input
            if (startDate > endDate)
                throw new ArgumentException("La data di inizio deve essere precedente alla data di fine");

            using var context = contextFactory.CreateDbContext();

            // Query ottimizzata con parametrizzazione SQL e join appropriati
            string sql = @"
                SELECT 
                    CONVERT(varchar, YEAR(ps.DataLavorazione)) + '-' + RIGHT('00' + CONVERT(varchar, MONTH(ps.DataLavorazione)), 2) AS Periodo,
                    SUM(ISNULL(ps.Fogli, 0)) AS Fogli
                FROM ProduzioneSistema AS ps
                INNER JOIN ProcedureLavorazioni AS pl ON ps.IdProceduraLavorazione = pl.IDProceduraLavorazione
                INNER JOIN FasiLavorazione AS fl ON ps.IdFaseLavorazione = fl.IdFaseLavorazione
                INNER JOIN ProcedureCliente AS pc ON pl.IDproceduraCliente = pc.IDproceduraCliente
                WHERE 
                    CONVERT(date, ps.DataLavorazione) >= @startDate
                    AND CONVERT(date, ps.DataLavorazione) <= @endDate
                    AND ps.IDCentro = @idCentro
                    AND ps.IdFaseLavorazione = 4
                GROUP BY YEAR(ps.DataLavorazione), MONTH(ps.DataLavorazione)
                ORDER BY YEAR(ps.DataLavorazione), MONTH(ps.DataLavorazione)";

            // Creazione dei parametri SQL per prevenire SQL injection e migliorare la cache del piano di esecuzione
            var parameters = new[]
            {
                new SqlParameter("@startDate", SqlDbType.Date) { Value = startDate.Date },
                new SqlParameter("@endDate", SqlDbType.Date) { Value = endDate.Date },
                new SqlParameter("@idCentro", SqlDbType.Int) { Value = idCentro }
            };

            // Esecuzione ottimizzata della query con disabilitazione del tracking per migliori performance
            var risultati = await context.Set<ReportChartStackedLineFogli>()
                .FromSqlRaw(sql, parameters)
                .AsNoTracking() // Migliora le performance per dati di sola lettura
                .ToListAsync();

            return risultati;
        }

        /// <inheritdoc/>
        public async Task<List<ReportChartStackedLineFogli>> GetGraficoFogliLavorazionePeriodo(DateTime startDate, DateTime endDate, int? idProceduraLavorazione, int? idFaseLavorazione, int idCentro)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            // Validazione parametri di input
            if (startDate > endDate)
                throw new ArgumentException("La data di inizio deve essere precedente alla data di fine");

            if (idProceduraLavorazione == null)
                throw new ArgumentNullException(nameof(idProceduraLavorazione), "IdProceduraLavorazione non può essere null");

            if (idFaseLavorazione == null)
                throw new ArgumentNullException(nameof(idFaseLavorazione), "IdFaseLavorazione non può essere null");

            using var context = contextFactory.CreateDbContext();

            // Query ottimizzata con parametrizzazione SQL per sicurezza e prestazioni
            string sql = @"
                SELECT 
                    CONVERT(varchar, YEAR(DataLavorazione)) + '-' + RIGHT('00' + CONVERT(varchar, MONTH(DataLavorazione)), 2) AS Periodo,
                    SUM(ISNULL(Fogli, 0)) AS Fogli
                FROM ProduzioneSistema
                WHERE 
                    IdProceduraLavorazione = @idProceduraLavorazione
                    AND IdFaseLavorazione = @idFaseLavorazione
                    AND CONVERT(date, DataLavorazione) >= @startDate
                    AND CONVERT(date, DataLavorazione) <= @endDate
                    AND IDCentro = @idCentro
                GROUP BY 
                    YEAR(DataLavorazione), MONTH(DataLavorazione)
                ORDER BY 
                    YEAR(DataLavorazione), MONTH(DataLavorazione)";

            // Creazione parametri SQL
            var parameters = new[]
            {
                new SqlParameter("@idProceduraLavorazione", SqlDbType.Int) { Value = idProceduraLavorazione.Value },
                new SqlParameter("@idFaseLavorazione", SqlDbType.Int) { Value = idFaseLavorazione.Value },
                new SqlParameter("@startDate", SqlDbType.Date) { Value = startDate.Date },
                new SqlParameter("@endDate", SqlDbType.Date) { Value = endDate.Date },
                new SqlParameter("@idCentro", SqlDbType.Int) { Value = idCentro }
            };

            // Esecuzione query con ottimizzazione performance
            var risultati = await context.Set<ReportChartStackedLineFogli>()
                .FromSqlRaw(sql, parameters)
                .AsNoTracking() // Migliora le performance per dati di sola lettura
                .ToListAsync();

            return risultati;
        }


        /// <inheritdoc/>
        public async Task<List<ReportChartStackedLineDocumenti>> GetGraficoDocumentiPeriodo(DateTime startDate, DateTime endDate, int idCentro)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            // Validazione parametri di input
            if (startDate > endDate)
                throw new ArgumentException("La data di inizio deve essere precedente alla data di fine");

            using var context = contextFactory.CreateDbContext();

            // Query ottimizzata con parametrizzazione SQL e join appropriati
            string sql = @"
                SELECT 
                    CONVERT(varchar, YEAR(ps.DataLavorazione)) + '-' + RIGHT('00' + CONVERT(varchar, MONTH(ps.DataLavorazione)), 2) AS Periodo,
                    SUM(ISNULL(ps.Documenti, 0)) AS Documenti
                FROM ProduzioneSistema AS ps
                INNER JOIN ProcedureLavorazioni AS pl ON ps.IdProceduraLavorazione = pl.IDProceduraLavorazione
                INNER JOIN FasiLavorazione AS fl ON ps.IdFaseLavorazione = fl.IdFaseLavorazione
                INNER JOIN ProcedureCliente AS pc ON pl.IDproceduraCliente = pc.IDproceduraCliente
                WHERE EXISTS (
                    SELECT 1
                    FROM LavorazioniFasiDataReading AS l 
                    WHERE pl.IdProceduraLavorazione = l.IdProceduraLavorazione 
                      AND fl.IdFaseLavorazione = l.IdFaseLavorazione 
                      AND l.FlagGraficoDocumenti = 1
                )
                AND CONVERT(date, ps.DataLavorazione) >= @startDate
                AND CONVERT(date, ps.DataLavorazione) <= @endDate
                AND ps.IDCentro = @idCentro
                GROUP BY YEAR(ps.DataLavorazione), MONTH(ps.DataLavorazione)
                ORDER BY YEAR(ps.DataLavorazione), MONTH(ps.DataLavorazione)";

            // Creazione dei parametri SQL per prevenire SQL injection e migliorare la cache del piano di esecuzione
            var parameters = new[]
            {
                new SqlParameter("@startDate", SqlDbType.Date) { Value = startDate.Date },
                new SqlParameter("@endDate", SqlDbType.Date) { Value = endDate.Date },
                new SqlParameter("@idCentro", SqlDbType.Int) { Value = idCentro }
            };

            // Esecuzione ottimizzata della query con disabilitazione del tracking per migliori performance
            var risultati = await context.Set<ReportChartStackedLineDocumenti>()
                .FromSqlRaw(sql, parameters)
                .AsNoTracking() // Migliora le performance per dati di sola lettura
                .ToListAsync();

            return risultati;
        }

        /// <inheritdoc/>
        public async Task<List<ReportChartStackedLineDocumenti>> GetGraficoDocumentiLavorazionePeriodo(DateTime startDate, DateTime endDate, int? idProceduraLavorazione, int? idFaseLavorazione, int idCentro)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            // Validazione parametri di input
            if (startDate > endDate)
                throw new ArgumentException("La data di inizio deve essere precedente alla data di fine");

            if (idProceduraLavorazione == null)
                throw new ArgumentNullException(nameof(idProceduraLavorazione), "IdProceduraLavorazione non può essere null");

            if (idFaseLavorazione == null)
                throw new ArgumentNullException(nameof(idFaseLavorazione), "IdFaseLavorazione non può essere null");

            using var context = contextFactory.CreateDbContext();

            // Query ottimizzata con parametrizzazione SQL per sicurezza e prestazioni
            string sql = @"
                SELECT 
                    CONVERT(varchar, YEAR(DataLavorazione)) + '-' + RIGHT('00' + CONVERT(varchar, MONTH(DataLavorazione)), 2) AS Periodo,
                    SUM(ISNULL(Documenti, 0)) AS Documenti
                FROM ProduzioneSistema
                WHERE 
                    IdProceduraLavorazione = @idProceduraLavorazione
                    AND IdFaseLavorazione = @idFaseLavorazione
                    AND CONVERT(date, DataLavorazione) >= @startDate
                    AND CONVERT(date, DataLavorazione) <= @endDate
                    AND IDCentro = @idCentro
                GROUP BY 
                    YEAR(DataLavorazione), MONTH(DataLavorazione)
                ORDER BY 
                    YEAR(DataLavorazione), MONTH(DataLavorazione)";

            // Creazione parametri SQL
            var parameters = new[]
            {
                new SqlParameter("@idProceduraLavorazione", SqlDbType.Int) { Value = idProceduraLavorazione.Value },
                new SqlParameter("@idFaseLavorazione", SqlDbType.Int) { Value = idFaseLavorazione.Value },
                new SqlParameter("@startDate", SqlDbType.Date) { Value = startDate.Date },
                new SqlParameter("@endDate", SqlDbType.Date) { Value = endDate.Date },
                new SqlParameter("@idCentro", SqlDbType.Int) { Value = idCentro }
            };

            // Esecuzione query con ottimizzazione performance
            var risultati = await context.Set<ReportChartStackedLineDocumenti>()
                .FromSqlRaw(sql, parameters)
                .AsNoTracking() // Migliora le performance per dati di sola lettura
                .ToListAsync();

            return risultati;
        }

        /// <inheritdoc/>
        public async Task<List<ReportChartStackedLineOre>> GetGraficoOreLavorazioniPeriodo(DateTime startDate, DateTime endDate, int idCentro)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            // Validazione parametri di input
            if (startDate > endDate)
                throw new ArgumentException("La data di inizio deve essere precedente alla data di fine");

            using var context = contextFactory.CreateDbContext();

            // Query ottimizzata con parametrizzazione SQL e join appropriati
            string sql = @"
                SELECT 
                    CONVERT(varchar, YEAR(po.DataLavorazione)) + '-' + RIGHT('00' + CONVERT(varchar, MONTH(po.DataLavorazione)), 2) AS Periodo,
                    SUM(ISNULL(po.TempoLavOreCent, 0)) AS Ore
                FROM ProduzioneOperatori AS po
                INNER JOIN ProcedureLavorazioni AS pl ON po.IdProceduraLavorazione = pl.IDProceduraLavorazione
                INNER JOIN ProcedureCliente AS pc ON pl.IDproceduraCliente = pc.IDproceduraCliente
                WHERE 
                    CONVERT(date, po.DataLavorazione) >= @startDate
                    AND CONVERT(date, po.DataLavorazione) <= @endDate
                    AND pl.IDCentro = @idCentro
                    AND pc.ProceduraCliente <> 'POSTEL'
                GROUP BY 
                    YEAR(po.DataLavorazione), MONTH(po.DataLavorazione)
                ORDER BY 
                    YEAR(po.DataLavorazione), MONTH(po.DataLavorazione)";

            // Creazione dei parametri SQL per prevenire SQL injection e migliorare la cache del piano di esecuzione
            var parameters = new[]
            {
                new SqlParameter("@startDate", SqlDbType.Date) { Value = startDate.Date },
                new SqlParameter("@endDate", SqlDbType.Date) { Value = endDate.Date },
                new SqlParameter("@idCentro", SqlDbType.Int) { Value = idCentro }
            };

            // Esecuzione ottimizzata della query con disabilitazione del tracking per migliori performance
            var risultati = await context.Set<ReportChartStackedLineOre>()
                .FromSqlRaw(sql, parameters)
                .AsNoTracking() // Migliora le performance per dati di sola lettura
                .ToListAsync();

            return risultati;
        }

        /// <inheritdoc/>
        public async Task<List<ReportChartStackedLineOre>> GetGraficoOreLavorazionePeriodo(DateTime startDate, DateTime endDate, int? idProceduraLavorazione, int? idFaseLavorazione, int idCentro)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            // Validazione parametri di input
            if (startDate > endDate)
                throw new ArgumentException("La data di inizio deve essere precedente alla data di fine");

            if (idProceduraLavorazione == null)
                throw new ArgumentNullException(nameof(idProceduraLavorazione), "IdProceduraLavorazione non può essere null");

            if (idFaseLavorazione == null)
                throw new ArgumentNullException(nameof(idFaseLavorazione), "IdFaseLavorazione non può essere null");

            using var context = contextFactory.CreateDbContext();

            // Query ottimizzata con parametrizzazione SQL per sicurezza e prestazioni
            string sql = @"
                SELECT 
                    CONVERT(varchar, YEAR(DataLavorazione)) + '-' + RIGHT('00' + CONVERT(varchar, MONTH(DataLavorazione)), 2) AS Periodo,
                    SUM(ISNULL(TempoLavOreCent, 0)) AS Ore
                FROM ProduzioneOperatori
                WHERE 
                    IdProceduraLavorazione = @idProceduraLavorazione
                    AND IdFaseLavorazione = @idFaseLavorazione
                    AND CONVERT(date, DataLavorazione) >= @startDate
                    AND CONVERT(date, DataLavorazione) <= @endDate
                    AND IDCentro = @idCentro
                GROUP BY 
                    YEAR(DataLavorazione), MONTH(DataLavorazione)
                ORDER BY 
                    YEAR(DataLavorazione), MONTH(DataLavorazione)";

            // Creazione parametri SQL
            var parameters = new[]
            {
                new SqlParameter("@idProceduraLavorazione", SqlDbType.Int) { Value = idProceduraLavorazione.Value },
                new SqlParameter("@idFaseLavorazione", SqlDbType.Int) { Value = idFaseLavorazione.Value },
                new SqlParameter("@startDate", SqlDbType.Date) { Value = startDate.Date },
                new SqlParameter("@endDate", SqlDbType.Date) { Value = endDate.Date },
                new SqlParameter("@idCentro", SqlDbType.Int) { Value = idCentro }
            };

            // Esecuzione query con ottimizzazione performance
            var risultati = await context.Set<ReportChartStackedLineOre>()
                .FromSqlRaw(sql, parameters)
                .AsNoTracking() // Migliora le performance per dati di sola lettura
                .ToListAsync();

            return risultati;
        }

        /// <inheritdoc/>
        public List<ReportChartStackedLine> GetChartStackdLine(
            List<ReportChartStackedLineDocumenti> lstDocumenti,
            List<ReportChartStackedLineFogli> lstFogli,
            List<ReportChartStackedLineOre> lstOre)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            // Validazione input per evitare NullReferenceException
            lstDocumenti ??= new List<ReportChartStackedLineDocumenti>();
            lstFogli ??= new List<ReportChartStackedLineFogli>();
            lstOre ??= new List<ReportChartStackedLineOre>();

            // Raccoglie tutti i periodi unici dalle tre fonti in modo efficiente
            var periodiUnici = new HashSet<string>(
                lstDocumenti.Select(d => d.Periodo!)
                .Concat(lstFogli.Select(f => f.Periodo!))
                .Concat(lstOre.Select(o => o.Periodo!))
                .Where(p => !string.IsNullOrEmpty(p))
            );

            // Crea risultati pre-inizializzati per ogni periodo
            var risultati = periodiUnici.Select(periodo => new ReportChartStackedLine
            {
                Periodo = periodo,
                Documenti = 0,
                Fogli = 0,
                Ore = 0
            }).ToList();

            // Crea indici per ricerche efficienti
            var documentiLookup = lstDocumenti.ToDictionary(d => d.Periodo!, d => d.Documenti);
            var fogliLookup = lstFogli.ToDictionary(f => f.Periodo!, f => f.Fogli);
            var oreLookup = lstOre.ToDictionary(o => o.Periodo!, o => o.Ore);

            // Compila i risultati usando gli indici per una ricerca efficiente
            foreach (var report in risultati)
            {
                if (documentiLookup.TryGetValue(report.Periodo!, out int documenti))
                    report.Documenti = documenti;

                if (fogliLookup.TryGetValue(report.Periodo!, out int fogli))
                    report.Fogli = fogli;

                if (oreLookup.TryGetValue(report.Periodo!, out double ore))
                    report.Ore = ore;
            }

            // Ordina i risultati cronologicamente per una presentazione coerente
            return risultati.OrderBy(r => r.Periodo).ToList();
        }

        /// <inheritdoc/>
        public MemoryStream CreateExcelProduzioneCompleta(IEnumerable<ReportEsportazioneOreDocumenti> lst)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            DataTable table1 = new DataTable();


            if (lst != null)
            {
                IEnumerable<ReportEsportazioneOreDocumenti> data = lst;
                if (table1 != null)
                {
                    using (var reader = ObjectReader.Create(data, "ProceduraCliente", "NomeProcedura", "FaseLavorazione", "TempoLavOreCent", "Documenti", "Fogli", "Pagine", "PagineSenzaBianco", "Scarti"))
                    {
                        table1.Load(reader);
                    }
                }
            }



            using (XLWorkbook wb = new XLWorkbook())
            {
                wb.Worksheets.Add("Report");
                wb.Worksheet(1).Cell(1, 1).InsertTable(table1);

                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return stream;
                }
            }
        }

    }
}
