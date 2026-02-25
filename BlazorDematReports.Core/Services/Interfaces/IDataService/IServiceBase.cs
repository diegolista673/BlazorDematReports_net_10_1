using System.Linq.Expressions;

namespace BlazorDematReports.Core.Services.Interfaces.IDataService
{
    public interface IServiceBase<T> where T : class
    {
        /// <summary>
        /// Restituisce tutte le entità materializzate come lista read-only.
        /// Il contesto viene creato e disposto internamente.
        /// </summary>
        Task<IReadOnlyList<T>> FindAllAsync();

        /// <summary>
        /// Restituisce le entità filtrate tramite espressione lambda, materializzate come lista read-only.
        /// Il contesto viene creato e disposto internamente.
        /// </summary>
        Task<IReadOnlyList<T>> FindByConditionAsync(Expression<Func<T, bool>> expression);

        /// <summary>
        /// Aggiunge una nuova entità e salva le modifiche nel database.
        /// </summary>
        Task CreateAsync(T entity);

        /// <summary>
        /// Elimina un'entità tramite identificativo e salva le modifiche nel database.
        /// </summary>
        Task DeleteAsync(int id);
    }
}
