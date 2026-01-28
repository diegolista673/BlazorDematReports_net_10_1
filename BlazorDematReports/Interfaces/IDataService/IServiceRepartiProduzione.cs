using BlazorDematReports.Dto;
using Entities.Models.DbApplication;

namespace BlazorDematReports.Interfaces.IDataService
{
    /// <summary>
    /// Interfaccia per la gestione dei reparti di produzione.
    /// </summary>
    public interface IServiceRepartiProduzione : IServiceBase<RepartiProduzione>
    {
        /// <summary>
        /// Restituisce la lista di tutti i reparti di produzione.
        /// </summary>
        /// <returns>Lista di oggetti <see cref="RepartiProduzione"/>.</returns>
        Task<List<RepartiProduzione>> GetRepartiProduzioneAsync();

        /// <summary>
        /// Restituisce un reparto di produzione tramite il suo identificativo.
        /// </summary>
        /// <param name="IdReparto">Identificativo del reparto.</param>
        /// <returns>Oggetto <see cref="RepartiProduzione"/> o null se non trovato.</returns>
        Task<RepartiProduzione?> GetRepartiProduzioneByIdAsync(int IdReparto);

        /// <summary>
        /// Restituisce un reparto di produzione tramite il nome.
        /// </summary>
        /// <param name="reparto">Nome del reparto.</param>
        /// <returns>Oggetto <see cref="RepartiProduzione"/> o null se non trovato.</returns>
        Task<RepartiProduzione?> GetRepartiProduzioneByTextAsync(string reparto);

        /// <summary>
        /// Elimina un reparto di produzione tramite il suo identificativo.
        /// </summary>
        /// <param name="idProduzione">Identificativo del reparto da eliminare.</param>
        /// <returns>Task asincrono.</returns>
        Task DeleteRepartiProduzione(int idProduzione);

        /// <summary>
        /// Aggiunge un nuovo reparto di produzione tramite DTO.
        /// </summary>
        /// <param name="repartiProduzioneDto">DTO del reparto di produzione da aggiungere.</param>
        /// <returns>Task asincrono.</returns>
        Task AddRepartiProduzione(RepartiProduzioneDto repartiProduzioneDto);
    }
}
