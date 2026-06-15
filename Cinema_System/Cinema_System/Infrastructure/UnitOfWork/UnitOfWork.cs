using Cinema_System.Application.Interfaces;
using Cinema_System.Infrastructure.Data;
using Cinema_System.Infrastructure.Repositories;

namespace Cinema_System.Infrastructure.UnitOfWork
{
    // Cài đặt IUnitOfWork: giữ 1 DbContext và cache repository theo entity để mọi thao tác đi qua cùng 1 phiên DB.
    public class UnitOfWork : IUnitOfWork
    {
        private readonly CinemaWebDbContext _context;
        private readonly Dictionary<Type, object> _repositories = new(); // cache repo đã tạo

        public UnitOfWork(CinemaWebDbContext context) => _context = context;

        public IGenericRepository<T> Repository<T>() where T : class
        {
            if (_repositories.TryGetValue(typeof(T), out var repo))
                return (IGenericRepository<T>)repo; // tái dùng repo cũ

            var newRepo = new GenericRepository<T>(_context);
            _repositories[typeof(T)] = newRepo;
            return newRepo;
        }

        public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();
        public void Dispose() => _context.Dispose();
    }
}
