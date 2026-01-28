using DataReading.Dto;
using DataReading.Models;
using Entities.Models.DbApplication;

/// <summary>
/// Interfaccia per la gestione dei task di lavorazione e la costruzione dei relativi DTO.
/// </summary>
public interface ITaskService
{
    /// <summary>
    /// Asynchronously retrieves a task to be executed based on the specified task ID.
    /// </summary>
    /// <remarks>This method performs an asynchronous operation to retrieve a task. Ensure that the provided
    /// <paramref name="idTaskDaEseguire"/> is valid and corresponds to an existing task. If no task is found, the
    /// result will be <see langword="null"/> or an exception may be thrown, depending on the implementation.</remarks>
    /// <param name="idTaskDaEseguire">The unique identifier of the task to retrieve. Must be a positive integer.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation. The result contains the <see
    /// cref="TaskDaEseguire"/> object corresponding to the specified ID, or <see langword="null"/> if no matching task
    /// is found.</returns>
    Task<TaskDaEseguire> GetTaskDaEseguireAsync(int idTaskDaEseguire);

    /// <summary>
    /// Recupera una query associata a una lavorazione tramite il suo identificativo.
    /// </summary>
    /// <param name="idQuery">Identificativo della query.</param>
    /// <returns>Oggetto <see cref="QueryProcedureLavorazioni"/> corrispondente all'ID specificato.</returns>
    /// <exception cref="NullReferenceException">Se la query non è presente.</exception>
    Task<QueryProcedureLavorazioni> GetQueryAsync(int idQuery);

    /// <summary>
    /// Costruisce un oggetto <see cref="TaskDaEseguireDto"/> a partire da un task, un job Hangfire serializzato e parametri aggiuntivi.
    /// </summary>
    /// <param name="task">Task da eseguire.</param>
    /// <param name="jsonJobHangfire">Job Hangfire serializzato.</param>
    /// <param name="startDate">Data di inizio.</param>
    /// <param name="endDate">Data di fine.</param>
    /// <param name="idCentro">Identificativo del centro di lavorazione.</param>
    /// <returns>DTO del task da eseguire.</returns>
    Task<TaskDaEseguireDto> BuildTaskDtoAsync(TaskDaEseguire task, JsonJobHangfire jsonJobHangfire, DateTime startDate, DateTime endDate, int idCentro);
}
