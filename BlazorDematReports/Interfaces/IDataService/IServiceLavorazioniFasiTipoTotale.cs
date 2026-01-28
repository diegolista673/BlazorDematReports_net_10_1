using BlazorDematReports.Dto;
using Entities.Models.DbApplication;

namespace BlazorDematReports.Interfaces.IDataService
{
    /// <summary>
    /// Interfaccia per la gestione delle lavorazioni fasi tipo totale.
    /// </summary>
    public interface IServiceLavorazioniFasiTipoTotale : IServiceBase<LavorazioniFasiTipoTotale>
    {
        /// <summary>
        /// Restituisce la lista di tutte le lavorazioni fasi tipo totale.
        /// </summary>
        /// <returns>Lista di oggetti <see cref="LavorazioniFasiTipoTotale"/>.</returns>
        Task<List<LavorazioniFasiTipoTotale>> GetLavorazioniFasiTipoTotaleAsync();

        /// <summary>
        /// Elimina una lavorazione fasi tipo totale tramite il suo identificativo.
        /// </summary>
        /// <param name="IdtipologieTotaliLavorazioneFase">Identificativo della lavorazione da eliminare.</param>
        /// <returns>Task asincrono.</returns>
        Task DeleteLavorazioniFasiTipoTotaleAsync(int IdtipologieTotaliLavorazioneFase);

        /// <summary>
        /// Aggiunge una nuova lavorazione fasi tipo totale tramite DTO.
        /// </summary>
        /// <param name="arg">DTO della lavorazione da aggiungere.</param>
        /// <returns>Task asincrono.</returns>
        Task AddLavorazioniFasiTipoTotaleAsync(LavorazioniFasiTipoTotaleDto arg);

        /// <summary>
        /// Aggiorna una lavorazione fasi tipo totale tramite DTO.
        /// </summary>
        /// <param name="arg">DTO della lavorazione da aggiornare.</param>
        /// <returns>Task asincrono.</returns>
        Task UpdateLavorazioniFasiTipoTotaleAsync(LavorazioniFasiTipoTotaleDto arg);

        /// <summary>
        /// Restituisce la lista di tutte le lavorazioni fasi tipo totale e le mappa su oggetti DTO.
        /// </summary>
        /// <returns>Lista di oggetti <see cref="LavorazioniFasiTipoTotaleDto"/>.</returns>
        Task<List<LavorazioniFasiTipoTotaleDto>> GetLavorazioniFasiTipoTotaleDtoAsync();
    }
}
