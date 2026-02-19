using BlazorDematReports.Core.Application.Dto;
using Entities.Models.DbApplication;

namespace BlazorDematReports.Core.Application.Interfaces
{
    /// <summary>
    /// Interfaccia per il servizio di gestione delle tipologie di totali.
    /// Fornisce operazioni CRUD per le tipologie di totali di produzione.
    /// </summary>
    public interface IServiceTipologieTotali : IServiceBase<TipologieTotali>
    {
        /// <summary>
        /// Get TipologieTotali
        /// </summary>
        /// <returns></returns>
        Task<List<TipologieTotali>> GetTipologieTotaliAsync();

        /// <summary>
        /// Ottiene le tipologie di totali attive per una specifica lavorazione e fase.
        /// </summary>
        /// <param name="idLavorazione">Identificativo della lavorazione.</param>
        /// <param name="idFase">Identificativo della fase.</param>
        /// <returns>Lista delle tipologie di totali attive per la lavorazione e fase specificate.</returns>
        Task<List<TipologieTotali>> GetTipologieAttiveByIdLavorazioneAsync(int idLavorazione, int idFase);

        /// <summary>
        /// GET all ListTipologieTotali and maps them to Dto objects
        /// </summary>
        /// <returns></returns>
        Task<List<TipologieTotaliDto>> GetTipologieTotaliDtoAsync();

        /// <summary>
        /// Elimina una tipologia di totali specificata.
        /// </summary>
        /// <param name="idTotaliTipologie">Identificativo della tipologia di totali da eliminare.</param>
        /// <returns>Task per l'operazione asincrona.</returns>
        Task DeleteTipologieTotaliAsync(int idTotaliTipologie);

        /// <summary>
        /// Aggiunge una nuova tipologia di totali.
        /// </summary>
        /// <param name="arg">DTO contenente i dati della tipologia di totali da aggiungere.</param>
        /// <returns>Task per l'operazione asincrona.</returns>
        Task AddTipologieTotaliAsync(TipologieTotaliDto arg);

        /// <summary>
        /// Aggiorna una tipologia di totali esistente.
        /// </summary>
        /// <param name="arg">DTO contenente i dati aggiornati della tipologia di totali.</param>
        /// <returns>Task per l'operazione asincrona.</returns>
        Task UpdateTipologieTotaliAsync(TipologieTotaliDto arg);
    }
}
