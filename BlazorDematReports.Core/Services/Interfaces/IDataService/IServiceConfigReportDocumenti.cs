using Entities.Models;
using Entities.Models.DbApplication;

namespace BlazorDematReports.Core.Interfaces.IDataService
{
    public interface IServiceConfigReportDocumenti : IServiceBase<ConfigReportDocumenti>
    {


        /// <summary>
        /// Ottiene l'elenco delle lavorazioni presenti nel grafico finale documenti, raggruppate per procedura cliente,
        /// in base al periodo di tempo specificato e al centro di lavorazione.
        /// </summary>
        /// <param name="startDate">Data di inizio del periodo di report</param>
        /// <param name="endDate">Data di fine del periodo di report</param>
        /// <param name="idCentro">ID del centro di lavorazione</param>
        /// <returns>Lista dei report documenti aggregati per procedura cliente</returns>
        /// <returns></returns>
        Task<List<ReportDocumenti>> GetGraficoDocumenti(DateTime startDate, DateTime endDate, int idCentro);

        /// <summary>
        /// Ottiene il report dei fogli scansionati aggregati per procedura cliente,
        /// filtrati per periodo di tempo e centro di lavorazione.
        /// </summary>
        /// <param name="startDate">Data di inizio del periodo di report</param>
        /// <param name="endDate">Data di fine del periodo di report</param>
        /// <param name="idCentro">ID del centro di lavorazione</param>
        /// <returns>Lista di report fogli aggregati per procedura cliente</returns>
        Task<List<ReportFogli>> GetGraficoFogliScansionati(DateTime startDate, DateTime endDate, int idCentro);


        /// <summary>
        /// Modifica il grafico in base alla percentuale di raggruppamento
        /// </summary>
        /// <param name="lst"></param>
        /// <param name="perc"></param>
        /// <returns></returns>
        List<ReportDocumenti> GetGraficoDocumentiModified(List<ReportDocumenti> lst, double perc);



        /// <summary>
        /// Modifica il grafico Fogli in base alla percentuale di raggruppamento
        /// </summary>
        /// <param name="lst"></param>
        /// <param name="perc"></param>
        /// <returns></returns>
        List<ReportFogli> GetGraficoFogliModified(List<ReportFogli> lst, double perc);

        /// <summary>
        /// Combina dati provenienti da tre fonti diverse (documenti, fogli, ore) in un unico report aggregato per periodo,
        /// mantenendo i valori originali di ciascuna fonte quando disponibili.
        /// </summary>
        /// <param name="lstDocumenti">Lista dei documenti per periodo</param>
        /// <param name="lstFogli">Lista dei fogli per periodo</param>
        /// <param name="lstOre">Lista delle ore lavorate per periodo</param>
        /// <returns>Lista combinata di periodi con relativi dati su documenti, fogli e ore</returns>
        List<ReportChartStackedLine> GetChartStackdLine(List<ReportChartStackedLineDocumenti> lstDocumenti, List<ReportChartStackedLineFogli> lstFogli, List<ReportChartStackedLineOre> lstOre);

        /// <summary>
        /// Get elenco lavorazioni presenti nel grafico finale ore, raggruppate per procedura cliente, tramite data lavorazione e idcentro
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="idCentro"></param>
        /// <returns></returns>
        Task<List<ReportEsportazioneOreDocumenti>> GetGraficoOreAsync(DateTime startDate, DateTime endDate, int idCentro);


        /// <summary>
        /// Modifica il grafico in base alla percentuale di raggruppamento
        /// </summary>
        /// <param name="lst"></param>
        /// <param name="perc"></param>
        /// <returns></returns>
        List<ReportEsportazioneOreDocumenti> GetGraficoOreModified(List<ReportEsportazioneOreDocumenti> lst, double perc);



        /// <summary>
        /// Ottiene il report dei clienti con dati aggregati di produzione e ore di lavoro
        /// </summary>
        /// <param name="startDate">Data di inizio periodo</param>
        /// <param name="endDate">Data di fine periodo</param>
        /// <param name="idCentro">ID del centro di lavorazione</param>
        /// <returns>Lista di dati aggregati per cliente, procedura e fase</returns>
        Task<List<ReportEsportazioneOreDocumenti>> GetReportClientiAsync(DateTime startDate, DateTime endDate, int idCentro);


        /// <summary>
        /// Ottiene i dati sui fogli scansionati aggregati per periodo (mese-anno),
        /// filtrati per intervallo di date e centro di lavorazione.
        /// </summary>
        /// <param name="startDate">Data di inizio del periodo di report</param>
        /// <param name="endDate">Data di fine del periodo di report</param>
        /// <param name="idCentro">ID del centro di lavorazione</param>
        /// <returns>Lista di report con fogli aggregati per periodo temporale (formato YYYY-MM)</returns>
        Task<List<ReportChartStackedLineFogli>> GetGraficoFogliScansionatiPeriodo(DateTime startDate, DateTime endDate, int idCentro);


        /// <summary>
        /// Ottiene i dati sulle ore lavorate aggregate per periodo (mese-anno),
        /// filtrati per intervallo di date e centro di lavorazione, escludendo il cliente 'POSTEL'.
        /// </summary>
        /// <param name="startDate">Data di inizio del periodo di report</param>
        /// <param name="endDate">Data di fine del periodo di report</param>
        /// <param name="idCentro">ID del centro di lavorazione</param>
        /// <returns>Lista di report con ore lavorate aggregate per periodo temporale (formato YYYY-MM)</returns>
        Task<List<ReportChartStackedLineOre>> GetGraficoOreLavorazioniPeriodo(DateTime startDate, DateTime endDate, int idCentro);




        /// <summary>
        /// Ottiene i dati sui documenti lavorati aggregati per periodo (mese-anno),
        /// filtrati per intervallo di date e centro di lavorazione.
        /// </summary>
        /// <param name="startDate">Data di inizio del periodo di report</param>
        /// <param name="endDate">Data di fine del periodo di report</param>
        /// <param name="idCentro">ID del centro di lavorazione</param>
        /// <returns>Lista di report con documenti aggregati per periodo temporale (formato YYYY-MM)</returns>
        Task<List<ReportChartStackedLineDocumenti>> GetGraficoDocumentiPeriodo(DateTime startDate, DateTime endDate, int idCentro);

        /// <summary>
        /// Ottiene i dati sui documenti lavorati aggregati per periodo (mese-anno),
        /// filtrati per procedura di lavorazione, fase e centro di lavorazione.
        /// </summary>
        /// <param name="startDate">Data di inizio del periodo di report</param>
        /// <param name="endDate">Data di fine del periodo di report</param>
        /// <param name="idProceduraLavorazione">ID della procedura di lavorazione</param>
        /// <param name="idFaseLavorazione">ID della fase di lavorazione</param>
        /// <param name="idCentro">ID del centro di lavorazione</param>
        /// <returns>Lista di report con documenti aggregati per periodo temporale (formato YYYY-MM)</returns>
        Task<List<ReportChartStackedLineDocumenti>> GetGraficoDocumentiLavorazionePeriodo(DateTime startDate, DateTime endDate, int? idProceduraLavorazione, int? idFaseLavorazione, int idCentro);

        /// <summary>
        /// Ottiene i dati sui fogli lavorati aggregati per periodo (mese-anno),
        /// filtrati per procedura di lavorazione, fase e centro di lavorazione.
        /// </summary>
        /// <param name="startDate">Data di inizio del periodo di report</param>
        /// <param name="endDate">Data di fine del periodo di report</param>
        /// <param name="idProceduraLavorazione">ID della procedura di lavorazione</param>
        /// <param name="idFaseLavorazione">ID della fase di lavorazione</param>
        /// <param name="idCentro">ID del centro di lavorazione</param>
        /// <returns>Lista di report con fogli aggregati per periodo temporale (formato YYYY-MM)</returns>
        Task<List<ReportChartStackedLineFogli>> GetGraficoFogliLavorazionePeriodo(DateTime startDate, DateTime endDate, int? idProceduraLavorazione, int? idFaseLavorazione, int idCentro);



        /// <summary>
        /// Ottiene i dati sulle ore lavorate aggregate per periodo (mese-anno),
        /// filtrati per procedura di lavorazione, fase e centro di lavorazione.
        /// </summary>
        /// <param name="startDate">Data di inizio del periodo di report</param>
        /// <param name="endDate">Data di fine del periodo di report</param>
        /// <param name="idProceduraLavorazione">ID della procedura di lavorazione</param>
        /// <param name="idFaseLavorazione">ID della fase di lavorazione</param>
        /// <param name="idCentro">ID del centro di lavorazione</param>
        /// <returns>Lista di report con ore lavorate aggregate per periodo temporale (formato YYYY-MM)</returns>
        Task<List<ReportChartStackedLineOre>> GetGraficoOreLavorazionePeriodo(DateTime startDate, DateTime endDate, int? idProceduraLavorazione, int? idFaseLavorazione, int idCentro);
        MemoryStream CreateExcelProduzioneCompleta(IEnumerable<ReportEsportazioneOreDocumenti> lst);

    }

}
