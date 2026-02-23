using BlazorDematReports.Core.Application.Dto;

using Entities.Models;
using Entities.Models.DbApplication;

namespace BlazorDematReports.Core.Services.Interfaces.IDataService
{
    /// <summary>
    /// Interfaccia per la gestione della produzione di sistema e delle relative operazioni sui dati.
    /// </summary>
    public interface IServiceProduzioneSistema : IServiceBase<ProduzioneSistema>
    {
        /// <summary>
        /// Aggiunge un nuovo record di produzione sistema.
        /// </summary>
        /// <param name="ProduzioneSistemaDto">DTO della produzione sistema da aggiungere.</param>
        /// <returns>Task asincrono.</returns>
        Task AddProduzioneSistemaAsync(ProduzioneSistemaDto ProduzioneSistemaDto);

        /// <summary>
        /// Elimina un record di produzione sistema tramite il suo identificativo.
        /// </summary>
        /// <param name="idProduzione">Identificativo del record da eliminare.</param>
        /// <returns>Task asincrono.</returns>
        Task DeleteProduzioneSistemaAsync(int idProduzione);

        /// <summary>
        /// Verifica se un record di produzione operatore è già presente.
        /// </summary>
        /// <param name="ProduzioneSistemaDto">DTO della produzione sistema da verificare.</param>
        /// <returns>True se il record è presente, altrimenti false.</returns>
        Task<bool> CheckProduzioneSistemaOperatoreAsync(ProduzioneSistemaDto ProduzioneSistemaDto);

        /// <summary>
        /// Restituisce la lista di produzioni sistema per operatore e data.
        /// </summary>
        /// <param name="idOperatore">Identificativo dell'operatore.</param>
        /// <param name="startDate">Data di inizio.</param>
        /// <returns>Lista di oggetti <see cref="ProduzioneSistema"/>.</returns>
        Task<List<ProduzioneSistema>> GetProduzioneSistemaByOperAndDate(int idOperatore, DateTime startDate);

        /// <summary>
        /// Restituisce la lista di produzioni sistema filtrata per data tramite DTO annuale.
        /// </summary>
        /// <param name="reportAnnualeDto">DTO con i parametri di filtro.</param>
        /// <returns>Lista di oggetti <see cref="ProduzioneSistema"/>.</returns>
        Task<List<ProduzioneSistema>> GetProduzioneSistemaByDateAsync(ReportAnnualeDto reportAnnualeDto);

        /// <summary>
        /// Restituisce la lista di produzioni sistema tramite DTO annuale.
        /// </summary>
        /// <param name="reportAnnualeDto">DTO con i parametri di filtro.</param>
        /// <returns>Lista di oggetti <see cref="ProduzioneSistema"/>.</returns>
        Task<List<ProduzioneSistema>> GetProduzioneSistemaAsync(ReportAnnualeDto reportAnnualeDto);

        /// <summary>
        /// Restituisce la lista delle produzioni inserite manualmente.
        /// </summary>
        /// <param name="produzioneSistemaDto">DTO della produzione sistema da filtrare.</param>
        /// <returns>Lista di oggetti <see cref="ReportProduzioneCompleta"/>.</returns>
        Task<List<ReportProduzioneCompleta>> GetReportProduzioneInseritaManualeAsync(ProduzioneSistemaDto produzioneSistemaDto);

        /// <summary>
        /// Aggiorna un record di produzione sistema tramite DTO.
        /// </summary>
        /// <param name="arg">DTO della produzione sistema da aggiornare.</param>
        /// <returns>Task asincrono.</returns>
        Task UpdateProduzioneSistemaAsync(ProduzioneSistemaDto arg);

        /// <summary>
        /// Restituisce la lista di produzioni sistema e li mappa su oggetti DTO.
        /// </summary>
        /// <param name="IdOperatore">Identificativo dell'operatore.</param>
        /// <param name="startDataLavorazione">Data di inizio lavorazione.</param>
        /// <returns>Lista di oggetti <see cref="ProduzioneSistemaDto"/>.</returns>
        Task<List<ProduzioneSistemaDto>> GetProduzioneSistemaDtoAsync(int? IdOperatore, DateTime? startDataLavorazione);

        /// <summary>
        /// Restituisce la prima data inserita in tabella produzione sistema per una procedura e fase specifica.
        /// </summary>
        /// <param name="IdProceduraLavorazione">Identificativo della procedura di lavorazione.</param>
        /// <param name="idFaseLavorazione">Identificativo della fase di lavorazione.</param>
        /// <returns>Stringa rappresentante la prima data, o null se non presente.</returns>
        Task<string?> GetPrimaDataInseritaAsync(int IdProceduraLavorazione, int idFaseLavorazione);

        /// <summary>
        /// Restituisce l'ultima data inserita in tabella produzione sistema per una procedura e fase specifica.
        /// </summary>
        /// <param name="IdProceduraLavorazione">Identificativo della procedura di lavorazione.</param>
        /// <param name="idFaseLavorazione">Identificativo della fase di lavorazione.</param>
        /// <returns>Stringa rappresentante l'ultima data, o null se non presente.</returns>
        Task<string?> GetUltimaDataInseritaAsync(int IdProceduraLavorazione, int idFaseLavorazione);
    }
}
