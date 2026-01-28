using BlazorDematReports.Dto;
using Entities.Models.DbApplication;

namespace BlazorDematReports.Interfaces.IDataService
{
    /// <summary>
    /// Interfaccia per la gestione della tabella dei task.
    /// </summary>
    public interface IServiceTabellaTask : IServiceBase<TabellaTask>
    {
        /// <summary>
        /// Restituisce la lista di tutti i task presenti in tabella.
        /// </summary>
        /// <returns>Lista di oggetti <see cref="TabellaTask"/>.</returns>
        Task<List<TabellaTask>> GetTabellaTaskAsync();

        /// <summary>
        /// Aggiunge un nuovo task tramite DTO.
        /// </summary>
        /// <param name="turnoDto">DTO del task da aggiungere.</param>
        /// <returns>Task asincrono.</returns>
        Task AddTaskAsync(TabellaTaskDto turnoDto);

        /// <summary>
        /// Elimina un task tramite il suo identificativo.
        /// </summary>
        /// <param name="idTask">Identificativo del task da eliminare.</param>
        /// <returns>Task asincrono.</returns>
        Task DeleteTask(int idTask);
    }
}
