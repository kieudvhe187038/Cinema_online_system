using System.Linq.Expressions;

namespace Cinema_System.Application.Interfaces;

/// <summary>
/// Hợp đồng CRUD chung cho mọi Entity, hỗ trợ Dependency Injection và kiểm thử.
/// </summary>
public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);

    Task<IEnumerable<T>> GetAllAsync(
        Expression<Func<T, bool>>? predicate = null,
        string[]? includeProperties = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null);

    Task<T?> FirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate,
        string[]? includeProperties = null);

    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);

    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);

    Task AddAsync(T entity);

    void Update(T entity);

    void Remove(T entity);
}
