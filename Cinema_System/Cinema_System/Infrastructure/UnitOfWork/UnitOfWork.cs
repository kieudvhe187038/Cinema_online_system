using Cinema_System.Application.Interfaces;
using Cinema_System.Infrastructure.Data;
using Cinema_System.Infrastructure.Repositories;

namespace Cinema_System.Infrastructure.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly CinemaWebDbContext _context;
        private readonly Dictionary<Type, object> _repositories = new();

        public UnitOfWork(CinemaWebDbContext context) => _context = context;

        // Trả về repository cho từng loại Entity, tái sử dụng nếu đã tạo
        public IGenericRepository<T> Repository<T>() where T : class
        {
            if (_repositories.TryGetValue(typeof(T), out var repo))
                return (IGenericRepository<T>)repo;

            var newRepo = new GenericRepository<T>(_context);
            _repositories[typeof(T)] = newRepo;
            return newRepo;
        }

        public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();
        public void Dispose() => _context.Dispose();
    }
}
