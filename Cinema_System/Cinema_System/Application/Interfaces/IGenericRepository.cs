using System.Linq.Expressions;

namespace Cinema_System.Application.Interfaces;

public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);

    Task<IEnumerable<T>> GetAllAsync(
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null);

    /// <summary>
    /// Lấy một trang dữ liệu kèm tổng số bản ghi (Skip/Take dịch xuống SQL).
    /// Dùng cho danh sách lớn để tránh tải toàn bộ bảng về bộ nhớ.
    /// </summary>
    Task<(IReadOnlyList<T> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null);

    Task<T?> FirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IQueryable<T>>? include = null);

    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);

    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);

    Task AddAsync(T entity);

    void Update(T entity);

    void Remove(T entity);
}
