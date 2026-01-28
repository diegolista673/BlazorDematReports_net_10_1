namespace DataReading.Infrastructure
{
    /// <summary>
    /// Contratto per lo scheduler unificato produzione + mail import basato su TaskDaEseguire.
    /// </summary>
    public interface IProductionJobScheduler
    {
        System.Threading.Tasks.Task<string> AddOrUpdateAsync(int idTaskDaEseguire);
        System.Threading.Tasks.Task DisableAsync(int idTaskDaEseguire);
        System.Threading.Tasks.Task SyncAllAsync();
        System.Threading.Tasks.Task RemoveByKeyAsync(string hangfireKey);
        System.Threading.Tasks.Task<int> CleanupOrphansAsync();
    }
}