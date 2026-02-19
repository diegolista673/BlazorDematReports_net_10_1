using BlazorDematReports.Core.Interfaces.IDataService;
using Entities.Models.DbApplication;

namespace BlazorDematReports.Core.Interfaces.IDataService
{
    /// <summary>
    /// Interfaccia per il servizio di gestione dei tipi di turno.
    /// Fornisce operazioni per la lettura dei tipi di turno lavorativo.
    /// </summary>
    public interface IServiceTipoTurni : IServiceBase<TipoTurni>
    {
        /// <summary>
        /// Ottiene tutti i tipi turni.
        /// </summary>
        /// <returns>Lista di tutti i tipi turni disponibili.</returns>
        Task<List<TipoTurni>> GetTipoTurniAsync();
    }
}
