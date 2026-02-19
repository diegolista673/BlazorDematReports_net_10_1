using BlazorDematReports.Core.Application.Dto;
using Entities.Models.DbApplication;

namespace BlazorDematReports.Core.Interfaces.IDataService
{
    /// <summary>
    /// Interfaccia per la gestione delle query procedure lavorazioni.
    /// </summary>
    public interface IServiceQueryProcedureLavorazioni : IServiceBase<QueryProcedureLavorazioni>
    {
        /// <summary>
        /// Restituisce la lista di tutte le query procedure lavorazioni DTO.
        /// </summary>
        /// <returns>Lista di oggetti <see cref="QueryProcedureLavorazioniDto"/>.</returns>
        Task<List<QueryProcedureLavorazioniDto>> GetAllQueryProcedureLavorazioniDtoAsync();

        /// <summary>
        /// Restituisce la lista di tutte le query procedure lavorazioni.
        /// </summary>
        /// <returns>Lista di oggetti <see cref="QueryProcedureLavorazioni"/>.</returns>
        Task<List<QueryProcedureLavorazioni>> GetAllQueryProcedureLavorazioniAsync();

        /// <summary>
        /// Restituisce la lista delle query procedure lavorazioni filtrata per id procedura lavorazione.
        /// </summary>
        /// <param name="idProceduraLavorazione">Identificativo della procedura lavorazione.</param>
        /// <returns>Lista di oggetti <see cref="QueryProcedureLavorazioni"/>.</returns>
        Task<List<QueryProcedureLavorazioni>> GetAllQueryProcedureLavorazioniByIdProceduraLavorazioneAsync(int idProceduraLavorazione);

        /// <summary>
        /// Aggiunge una nuova query procedure lavorazioni tramite DTO.
        /// </summary>
        /// <param name="arg">DTO della query da aggiungere.</param>
        /// <returns>Task asincrono.</returns>
        Task AddQueryProcedureLavorazioniAsync(QueryProcedureLavorazioniDto arg);

        /// <summary>
        /// Aggiorna una query procedure lavorazioni tramite DTO.
        /// </summary>
        /// <param name="arg">DTO della query da aggiornare.</param>
        /// <returns>Task asincrono.</returns>
        Task UpdateQueryProcedureLavorazioniAsync(QueryProcedureLavorazioniDto arg);

        /// <summary>
        /// Elimina una query procedure lavorazioni tramite il suo identificativo.
        /// </summary>
        /// <param name="id">Identificativo della query da eliminare.</param>
        /// <returns>Task asincrono.</returns>
        Task DeleteQueryProcedureLavorazioniAsync(int id);
    }
}
