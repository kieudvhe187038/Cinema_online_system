using System.Linq.Expressions;
using Cinema_System.Application.Interfaces;
using Cinema_System.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Cinema_System.Infrastructure.Repositories
{
    // Cài đặt IGenericRepository - nơi duy nhất thực sự gọi EF Core truy vấn DB.
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        private readonly CinemaWebDbContext _context;
        private readonly DbSet<T> _dbSet; // "bảng" tương ứng entity T

        public GenericRepository(CinemaWebDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public async Task<T?> GetByIdAsync(Guid id) => await _dbSet.FindAsync(id);

        public async Task<T?> FirstOrDefaultAsync(
            Expression<Func<T, bool>> predicate,
            params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet;
            foreach (var inc in includes) query = query.Include(inc); // nối JOIN nếu có
            return await query.FirstOrDefaultAsync(predicate);
        }

        public async Task<List<T>> GetAllAsync(
            Expression<Func<T, bool>> predicate,
            params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet;
            foreach (var inc in includes) query = query.Include(inc);
            return await query.Where(predicate).ToListAsync();
        }

        // Chỉ đánh dấu đã sửa; SaveChanges (qua UnitOfWork) mới thực sự ghi DB
        public void Update(T entity) => _dbSet.Update(entity);
    }
}
