using BlazorDematReports.Dto;
using Entities.Models.DbApplication;

namespace BlazorDematReports.Interfaces.IDataService
{
    public interface IServiceFasiLavorazioni : IServiceBase<FasiLavorazione>
    {

        /// <summary>
        /// Get tutte le fasi di lavorazione 
        /// </summary>
        /// <returns></returns>
        Task<List<FasiLavorazione>> GetFasiLavorazioneAsync();


        Task DeleteFasiLavorazioneAsync(int idFase);

        /// <summary>
        /// Add object and Save database
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        Task AddFasiLavorazioneAsync(FasiLavorazioneDto arg);

        /// <summary>
        /// Update object and save database
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        Task UpdateFasiLavorazioneAsync(FasiLavorazioneDto arg);



        /// <summary>
        /// Get all fasi di lavorazioni and map them to dto objects
        /// </summary>
        /// <returns></returns>
        Task<List<FasiLavorazioneDto>> GetFasiLavorazioneDtoAsync();

    }

}
