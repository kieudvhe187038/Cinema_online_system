using System.Linq.Expressions;

namespace Cinema_System.Application.Interfaces
{
    // Repository tổng quát dùng chung cho mọi entity (T) -> không phải viết lại CRUD cho từng bảng.
    public interface IGenericRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(Guid id); // tìm theo khóa chính

        // Lấy 1 bản ghi theo điều kiện; includes = các bảng liên quan cần JOIN kèm
        Task<T?> FirstOrDefaultAsync(
            Expression<Func<T, bool>> predicate,
            params Expression<Func<T, object>>[] includes);

        void Update(T entity);

        // Lấy danh sách bản ghi theo điều kiện
        Task<List<T>> GetAllAsync(
            Expression<Func<T, bool>> predicate,
            params Expression<Func<T, object>>[] includes);
    }
}
