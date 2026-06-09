using System.Linq.Expressions;

namespace Cinema_System.Application.Interfaces
{
    // Hợp đồng CRUD chung cho mọi Entity (T). Service gọi qua đây, không đụng DbContext.
    public interface IGenericRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(Guid id);

        // Lấy 1 bản ghi theo điều kiện, kèm các bảng liên quan (Include)
        Task<T?> FirstOrDefaultAsync(
            Expression<Func<T, bool>> predicate,
            params Expression<Func<T, object>>[] includes);

        void Update(T entity);
    }
}
