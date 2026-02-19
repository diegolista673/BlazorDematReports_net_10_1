namespace BlazorDematReports.Core.DataReading.Infrastructure
{
    /// <summary>
    /// Adattatore astratto su Hangfire per gestione recurring job 
    /// (crea, aggiorna, rimuove, enumera).
    /// </summary>
    public interface IRecurringJobManagerAdapter
    {
        void AddOrUpdate(string jobKey, int configId, string cronExpression);
        void RemoveIfExists(string jobKey);
        System.Collections.Generic.IEnumerable<string> GetRecurringJobKeys();
    }
}