using Entities.Models.DbApplication;

namespace BlazorDematReports.Interfaces.IDataService
{
    /// <summary>
    /// Interfaccia per il servizio che gestisce i task da eseguire.
    /// Estende l'interfaccia di base IServiceBase.
    /// </summary>
    public interface IServiceTaskDaEseguire : IServiceBase<TaskDaEseguire>
    {
        /// <summary>
        /// Ottiene l'elenco completo dei task da eseguire.
        /// </summary>
        /// <returns>Lista di task da eseguire.</returns>
        Task<List<TaskDaEseguire>> GetTabellaTaskDaEseguireAsync();

        /// <summary>
        /// Ottiene l'elenco dei task da eseguire filtrati per ID della procedura di lavorazione.
        /// </summary>
        /// <param name="IdProceduraLavorazione">ID della procedura di lavorazione.</param>
        /// <returns>Lista di task da eseguire filtrati.</returns>
        Task<List<TaskDaEseguire>> GetTabellaTaskDaEseguireAsync(int IdProceduraLavorazione);
        
        /// <summary>Restituisce i task mail import (con IdConfigurazioneDatabase tipo EmailCSV).</summary>
        Task<List<TaskDaEseguire>> GetMailImportTasksAsync();
        
        /// <summary>Restituisce i task mail import per una procedura.</summary>
        Task<List<TaskDaEseguire>> GetMailImportTasksAsync(int idProceduraLavorazione);
        
        // REMOVED: UpsertMailTaskAsync - deprecated (use /admin/fonti-dati to create EmailCSV configurations)

        /// <summary>
        /// Ottiene tutti i task mail configurati nel sistema.
        /// </summary>
        /// <returns>Lista di task mail.</returns>
        Task<List<TaskDaEseguire>> GetMailJobsAsync();

        /// <summary>
        /// Salva o aggiorna un task mail.
        /// </summary>
        /// <param name="task">Task da salvare o aggiornare.</param>
        /// <returns>Task salvato/aggiornato.</returns>
        Task<TaskDaEseguire> SaveOrUpdateMailJobAsync(TaskDaEseguire task);

        /// <summary>
        /// Elimina un task specifico.
        /// </summary>
        /// <param name="taskId">ID del task da eliminare.</param>
        /// <returns>Task asincrono.</returns>
        Task DeleteTaskAsync(int taskId);
    }
}
