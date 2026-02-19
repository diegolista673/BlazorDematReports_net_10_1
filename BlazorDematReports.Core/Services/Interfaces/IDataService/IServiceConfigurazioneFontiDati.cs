using BlazorDematReports.Core.Application.Dto;
using Entities.Models.DbApplication;

namespace BlazorDematReports.Core.Interfaces.IDataService
{
    public interface IServiceConfigurazioneFontiDati : IServiceBase<ConfigurazioneFontiDati>
    {

        /// <summary>
        /// Get all ConfigurazioneFontiDati
        /// </summary>
        /// <returns></returns>
        Task<List<ConfigurazioneRiepilogoDto>> GetConfigurazioneFontiDatiDtoAsync();


        /// <summary>
        /// Delete procedura di lavorazione by ID configurazione
        /// </summary>
        /// <param name="idConf"></param>
        /// <returns></returns>
        Task DeleteConfigurazioneFontiDatiAsync(int idConf);


        /// <summary>
        /// Asynchronously updates the data source configuration and its associated phase-to-center mappings.
        /// </summary>
        /// <param name="ConfigurazioneFontiDati">The configuration object representing the data sources to be updated. Cannot be null.</param>
        /// <param name="mappingFasi">A list of phase-to-center mapping objects to associate with the configuration. Cannot be null.</param>
        /// <param name="user">The username of the user performing the update operation. Cannot be null or empty.</param>
        /// <returns>A task that represents the asynchronous update operation.</returns>
        Task UpdateConfigurazioneFontiDatiAsync(ConfigurazioneFontiDati ConfigurazioneFontiDati, List<ConfigurazioneFaseCentro> mappingFasi, string user);


        /// <summary>
        /// Add configurazioneFontiDati
        /// </summary>
        /// <param name="configurazioneFontiDati"></param>
        /// <returns></returns>
        Task AddConfigurazioneFontiDatiAsync(ConfigurazioneFontiDati configurazioneFontiDati, List<ConfigurazioneFaseCentro> mappingFasi, string user);

    }
}
