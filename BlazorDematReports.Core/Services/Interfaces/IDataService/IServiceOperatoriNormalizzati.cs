
using BlazorDematReports.Core.Application.Dto;
using Entities.Models.DbApplication;

namespace BlazorDematReports.Core.Interfaces.IDataService
{
    /// <summary>
    /// Interfaccia per la gestione degli operatori normalizzati.
    /// </summary>
    public interface IServiceOperatoriNormalizzati : IServiceBase<OperatoriNormalizzati>
    {
        /// <summary>
        /// Restituisce la lista di tutti gli operatori normalizzati.
        /// </summary>
        /// <returns>Lista di oggetti <see cref="OperatoriNormalizzati"/>.</returns>
        Task<List<OperatoriNormalizzati>> GetOperatoriNormalizzatiAsync();

        /// <summary>
        /// Aggiunge un nuovo operatore normalizzato tramite DTO.
        /// </summary>
        /// <param name="operatoriNormalizzatiDto">DTO dell'operatore normalizzato da aggiungere.</param>
        /// <returns>Task asincrono.</returns>
        Task AddOperatoriNormalizzatiAsync(OperatoriNormalizzatiDto operatoriNormalizzatiDto);

        /// <summary>
        /// Elimina un operatore normalizzato tramite il suo identificativo.
        /// </summary>
        /// <param name="idCentro">Identificativo del centro dell'operatore da eliminare.</param>
        /// <returns>Task asincrono.</returns>
        Task DeleteOperatoriNormalizzatiAsync(int idCentro);

        /// <summary>
        /// Aggiorna un operatore normalizzato tramite DTO.
        /// </summary>
        /// <param name="operatoriNormalizzatiDto">DTO dell'operatore normalizzato da aggiornare.</param>
        /// <returns>Task asincrono.</returns>
        Task UpdateOperatoriNormalizzatiAsync(OperatoriNormalizzatiDto operatoriNormalizzatiDto);

        /// <summary>
        /// Restituisce la lista di tutti gli operatori normalizzati e li mappa su oggetti DTO.
        /// </summary>
        /// <returns>Lista di oggetti <see cref="OperatoriNormalizzatiDto"/>.</returns>
        Task<List<OperatoriNormalizzatiDto>> GetOperatoriNormalizzatiDtoAsync();
    }
}
