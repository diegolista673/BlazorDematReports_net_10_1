using BlazorDematReports.Core.Application.Dto;
using Entities.Models;
using Entities.Models.DbApplication;

namespace BlazorDematReports.Core.Application.Interfaces
{
    /// <summary>
    /// Interfaccia per la gestione della produzione operatori e delle relative operazioni sui dati.
    /// </summary>
    public interface IServiceProduzioneOperatori : IServiceBase<ProduzioneOperatori>
    {
        Task<List<ReportAnnualeTotaliDedicati>> GetReportTotaliDedicatiAsync(int IDProceduraLavorazione, int anno, int IDCentro);
        Task<List<ReportAnniSistema>> GetReportLast5YearsAsync(int IdProceduraLavorazione, int IdCentro);
        Task<List<ReportOreDocumenti>> GetReportProduzioneOreDocumentiAsync(int anno, int IDProceduraLavorazione, int IDCentro);

        /// <summary>
        /// Aggiunge una nuova produzione operatori e i totali produzione associati.
        /// </summary>
        /// <param name="produzioneOperatoriDto">DTO della produzione operatori da aggiungere.</param>
        /// <param name="totaliProduzioneDto">Lista dei totali produzione associati.</param>
        /// <returns>Task asincrono.</returns>
        Task AddProduzioneOperatoriAsync(ProduzioneOperatoriDto produzioneOperatoriDto, List<TipologieTotaliDto> totaliProduzioneDto);

        /// <summary>
        /// Elimina una produzione operatori tramite il suo identificativo.
        /// </summary>
        /// <param name="id">Identificativo della produzione operatori da eliminare.</param>
        /// <returns>Task asincrono.</returns>
        Task DeleteProduzioneOperatoriAsync(int id);

        /// <summary>
        /// Aggiorna una produzione operatori tramite DTO.
        /// </summary>
        /// <param name="produzioneOperatoriDto">DTO della produzione operatori da aggiornare.</param>
        /// <returns>Task asincrono.</returns>
        Task UpdateProduzioneOperatoriAsync(ProduzioneOperatoriDto produzioneOperatoriDto);

        /// <summary>
        /// Restituisce la lista delle produzioni operatori DTO per operatore e data.
        /// </summary>
        /// <param name="IdOperatore">Identificativo dell'operatore.</param>
        /// <param name="startDataLavorazione">Data di inizio lavorazione.</param>
        /// <returns>Lista di oggetti <see cref="ProduzioneOperatoriDto"/>.</returns>
        Task<List<ProduzioneOperatoriDto>> GetProduzioneOperatoriDtoAsync(int IdOperatore, DateTime startDataLavorazione);

        /// <summary>
        /// Restituisce la lista di tutte le produzioni operatori.
        /// </summary>
        /// <returns>Lista di oggetti <see cref="ProduzioneOperatori"/>.</returns>
        Task<List<ProduzioneOperatori>> GetProduzioneOperatoriAsync();

        /// <summary>
        /// Restituisce il report annuale di produzione sistema.
        /// </summary>
        /// <param name="reportAnnualeDto">DTO con i parametri di filtro.</param>
        /// <returns>Lista di oggetti <see cref="ReportProduzioneSistema"/>.</returns>
        Task<List<ReportProduzioneSistema>> GetReportAnnualeAsync(ReportAnnualeDto reportAnnualeDto);

        /// <summary>
        /// Restituisce il report degli ultimi 5 anni di lavorazione documenti, fogli, pagine.
        /// </summary>
        /// <param name="reportAnnualeDto">DTO con i parametri di filtro.</param>
        /// <returns>Lista di oggetti <see cref="ReportAnniSistema"/>.</returns>
        Task<List<ReportAnniSistema>> GetReportAnniPrecedentiAsync(ReportAnnualeDto reportAnnualeDto);



        /// <summary>
        /// Restituisce il report dei totali dedicati giornaliero.
        /// </summary>
        /// <param name="reportAnnualeDto">DTO con i parametri di filtro.</param>
        /// <returns>Lista di oggetti <see cref="ReportGiornalieroTotaliDedicati"/>.</returns>
        Task<List<ReportGiornalieroTotaliDedicati>> GetReportTotaliDedicatiGiornalieroAsync(ReportAnnualeDto reportAnnualeDto);

        /// <summary>
        /// Restituisce il report dei totali dedicati per periodo da/a.
        /// </summary>
        /// <param name="reportAnnualeDto">DTO con i parametri di filtro.</param>
        /// <returns>Lista di oggetti <see cref="ReportGiornalieroTotaliDedicati"/>.</returns>
        Task<List<ReportGiornalieroTotaliDedicati>> GetReportTotaliDedicatiPeriodoAsync(ReportAnnualeDto reportAnnualeDto);

        /// <summary>
        /// Restituisce il report produzione completa di ore e documenti giornaliera.
        /// </summary>
        /// <param name="startDataLavorazione">Data di inizio lavorazione.</param>
        /// <param name="endDataLavorazione">Data di fine lavorazione.</param>
        /// <param name="idCentro">Identificativo del centro.</param>
        /// <returns>Lista di oggetti <see cref="ReportProduzioneCompleta"/>.</returns>
        Task<List<ReportProduzioneCompleta>> GetReportProduzioneCompletaGiornalieraAsync(DateTime startDataLavorazione, DateTime endDataLavorazione, int idCentro);

        /// <summary>
        /// Verifica se un record di produzione operatori è già presente.
        /// </summary>
        /// <param name="produzioneOperatoriDto">DTO della produzione operatori da verificare.</param>
        /// <returns>True se il record è presente, altrimenti false.</returns>
        Task<bool> CheckProduzioneOperatori(ProduzioneOperatoriDto produzioneOperatoriDto);

        /// <summary>
        /// Crea un file Excel con la produzione annua.
        /// </summary>
        /// <param name="lstReportAnnualeView">Elenco dei report annuali.</param>
        /// <param name="lstReportTotaliDedicatiAnnualeView">Elenco dei totali dedicati annuali.</param>
        /// <returns>Oggetto MemoryStream con il file Excel.</returns>
        MemoryStream CreateExcelProduzioneAnnua(IEnumerable<ReportOreDocumenti> lstReportAnnualeView, IEnumerable<ReportAnnualeTotaliDedicati> lstReportTotaliDedicatiAnnualeView);

        /// <summary>
        /// Crea un file Excel con la produzione giornaliera completa.
        /// </summary>
        /// <param name="lstReportGiornalieroView">Elenco dei report giornalieri.</param>
        /// <param name="lstReportTotaliDedicatiGiornalieroView">Elenco dei totali dedicati giornalieri (opzionale).</param>
        /// <returns>Oggetto MemoryStream con il file Excel.</returns>
        MemoryStream CreateExcelProduzioneGiornalieraCompleta(IEnumerable<ReportProduzioneCompleta> lstReportGiornalieroView, IEnumerable<ReportGiornalieroTotaliDedicati>? lstReportTotaliDedicatiGiornalieroView = null);


    }
}
