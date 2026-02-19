using System.Linq.Expressions;

namespace BlazorDematReports.Core.Application.Interfaces
{
    public interface IServiceBase<T> where T : class
    {
        /// <summary>
        /// Find ALL from this.context.Set<T>().AsNoTracking();
        /// </summary>
        /// <returns></returns>
        IQueryable<T> FindAll();

        /// <summary>
        /// Find by condition from this.context.Set<T>().Where(expression).AsNoTracking();
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression);

        /// <summary>
        /// Add object and Save Db
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        Task CreateAsync(T entity);


        /// <summary>
        /// Delete object by ID and Save Database
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task DeleteAsync(int id);


    }
}
