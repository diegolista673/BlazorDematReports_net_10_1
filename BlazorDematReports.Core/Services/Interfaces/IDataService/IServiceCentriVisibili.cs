using Entities.Models.DbApplication;

namespace BlazorDematReports.Core.Services.Interfaces.IDataService
{
    public interface IServiceCentriVisibili : IServiceBase<CentriVisibili>
    {
        /// <summary>
        /// Get elenco dei centri in base ai permessi e ai centri relativi associati all'operatore, 
        /// non usata in Settings dove solo Admin può modificare clienti, lavorazioni ecc.. di più centri
        /// Esempio: un responsabile può vedere i dati di più centri 
        /// </summary>
        /// <returns></returns>
        Task<List<CentriVisibili>> GetCentriForShowDataAsync();

        /// <summary>
        /// Add object and Save Database
        /// </summary>
        /// <param name="CentriLavorazioneDto"></param>
        /// <returns></returns>
        //Task AddCentro(CentriLavorazioneDto CentriLavorazioneDto);

        /// Test per inserire dati in tabella centri visibili
        Task Fill();

    }
}
