using System.Linq.Expressions;

namespace Cinema_System.Application.Interfaces;

/// <summary>
/// Hợp đồng CRUD chung cho mọi Entity, hỗ trợ Dependency Injection và kiểm thử.
/// </summary>
public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);

    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);

    Task<IEnumerable<T>> GetAllAsync();

    Task AddAsync(T entity);

    void Update(T entity);

    void Remove(T entity);
}
