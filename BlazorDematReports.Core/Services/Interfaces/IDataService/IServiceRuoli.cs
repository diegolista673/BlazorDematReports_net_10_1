using BlazorDematReports.Core.Application.Dto;
using Entities.Models.DbApplication;

namespace BlazorDematReports.Core.Interfaces.IDataService
{
    /// <summary>
    /// Interfaccia per la gestione dei ruoli e delle relative operazioni sui dati.
    /// </summary>
    public interface IServiceRuoli : IServiceBase<Ruoli>
    {
        /// <summary>
        /// Restituisce la lista di tutti i ruoli.
        /// </summary>
        /// <returns>Lista di oggetti <see cref="Ruoli"/>.</returns>
        Task<List<Ruoli>> GetRuoliAsync();

        /// <summary>
        /// Restituisce un ruolo tramite il suo identificativo.
        /// </summary>
        /// <param name="IdRuolo">Identificativo del ruolo.</param>
        /// <returns>Oggetto <see cref="Ruoli"/> o null se non trovato.</returns>
        Task<Ruoli?> GetRuoliByIdAsync(int IdRuolo);

        /// <summary>
        /// Aggiunge un nuovo ruolo tramite DTO.
        /// </summary>
        /// <param name="ruoliDto">DTO del ruolo da aggiungere.</param>
        /// <returns>Task asincrono.</returns>
        Task AddRuoloAsync(RuoliDto ruoliDto);

        /// <summary>
        /// Elimina un ruolo tramite il suo identificativo.
        /// </summary>
        /// <param name="ruolo">Identificativo del ruolo da eliminare.</param>
        /// <returns>Task asincrono.</returns>
        Task DeleteRuoloAsync(int ruolo);

        /// <summary>
        /// Aggiorna un ruolo tramite DTO.
        /// </summary>
        /// <param name="arg">DTO del ruolo da aggiornare.</param>
        /// <returns>Task asincrono.</returns>
        Task UpdateRuoloAsync(RuoliDto arg);

        /// <summary>
        /// Restituisce la lista di tutti i ruoli e li mappa su oggetti DTO.
        /// </summary>
        /// <returns>Lista di oggetti <see cref="RuoliDto"/>.</returns>
        Task<List<RuoliDto>> GetRuoliDtoAsync();

        /// <summary>
        /// Restituisce la lista dei ruoli associati all'utente corrente.
        /// </summary>
        /// <returns>Lista di oggetti <see cref="Ruoli"/>.</returns>
        Task<List<Ruoli>> GetRuoliByUserAsync();
    }
}
