using System.Linq.Expressions;

namespace Cinema_System.Application.Interfaces
{
    public interface IGenericRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(Guid id);

        Task<T?> FirstOrDefaultAsync(
            Expression<Func<T, bool>> predicate,
            params Expression<Func<T, object>>[] includes);

        void Update(T entity);

        Task<List<T>> GetAllAsync(
            System.Linq.Expressions.Expression<Func<T, bool>> predicate,
            params System.Linq.Expressions.Expression<Func<T, object>>[] includes);
    }
}
