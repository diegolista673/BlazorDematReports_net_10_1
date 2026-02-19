using BlazorDematReports.Core.Application.Dto;
using Entities.Models.DbApplication;

namespace BlazorDematReports.Core.Application.Interfaces
{
    /// <summary>
    /// Interfaccia per la gestione dei formati dati.
    /// </summary>
    public interface IServiceFormatoDati : IServiceBase<FormatoDati>
    {
        /// <summary>
        /// Restituisce la lista di tutti i formati dati.
        /// </summary>
        /// <returns>Lista di oggetti <see cref="FormatoDati"/>.</returns>
        Task<List<FormatoDati>> GetFormatoDatiAsync();

        /// <summary>
        /// Restituisce un formato dati tramite il suo identificativo.
        /// </summary>
        /// <param name="IdFormatoDati">Identificativo del formato dati.</param>
        /// <returns>Oggetto <see cref="FormatoDati"/> o null se non trovato.</returns>
        Task<FormatoDati?> GetFormatoDatiByIdAsync(int IdFormatoDati);

        /// <summary>
        /// Restituisce un formato dati tramite il nome.
        /// </summary>
        /// <param name="formatoDati">Nome del formato dati.</param>
        /// <returns>Oggetto <see cref="FormatoDati"/> o null se non trovato.</returns>
        Task<FormatoDati?> GetFormatoDatiByTextAsync(string formatoDati);

        /// <summary>
        /// Elimina un formato dati tramite il suo identificativo.
        /// </summary>
        /// <param name="IdFormatoDati">Identificativo del formato dati da eliminare.</param>
        /// <returns>Task asincrono.</returns>
        Task DeleteFormatoDati(int IdFormatoDati);

        /// <summary>
        /// Aggiunge un nuovo formato dati tramite DTO.
        /// </summary>
        /// <param name="formatoDatiDto">DTO del formato dati da aggiungere.</param>
        /// <returns>Task asincrono.</returns>
        Task AddFormatoDati(FormatoDatiDto formatoDatiDto);
    }
}
