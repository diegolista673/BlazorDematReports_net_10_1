using BlazorDematReports.Core.Application.Dto;
using Entities.Models.DbApplication;

namespace BlazorDematReports.Core.Services.Interfaces.IDataService
{
    public interface IServiceCentri : IServiceBase<CentriLavorazione>
    {
        /// <summary>
        /// Get elenco dei centri in base ai permessi, se Owner mostra tutti i centri altrimenti solo quelli appartenenti al centro di appartenenza dello user
        /// </summary>
        /// <returns></returns>
        Task<List<CentriLavorazione>> GetCentriByUserAsync();

        /// <summary>
        /// Add object and Save Database
        /// </summary>
        /// <param name="CentriLavorazioneDto"></param>
        /// <returns></returns>
        Task AddCentro(CentriLavorazioneDto CentriLavorazioneDto);

        /// <summary>
        /// Get i centri visibili per i dati di produzione
        /// </summary>
        /// <returns></returns>
        Task<List<CentriVisibiliDto>> GetCentriVisibiliDtoByUserAsync();

    }
}
