using BlazorDematReports.Dto;

namespace BlazorDematReports.Interfaces.IDataService
{
    public interface IServiceTaskManagement
    {
        // Metodi esistenti
        Task<ConfigurazioneTaskDetailDto?> GetConfigurazioneWithTasksAsync(int idConfigurazione);
        Task<bool> ToggleTaskAsync(int idTask, bool enabled);
        Task<bool> ToggleMappingTasksAsync(int idFaseCentro, bool enabled);
        Task<bool> DeleteTaskAsync(int idTask);
        Task<string> GetActiveQueryForMappingAsync(int idFaseCentro);
        
        Task<ConfigurazioneTaskEditDto?> GetTaskForEditAsync(int idFaseCentro);
        Task<bool> UpdateTaskConfigurationAsync(ConfigurazioneTaskEditDto taskDto);
        Task<bool> ValidateUniqueTaskAsync(int idConfigurazione, int idFase, string cron, int? excludeIdFaseCentro = null);
    }
}


