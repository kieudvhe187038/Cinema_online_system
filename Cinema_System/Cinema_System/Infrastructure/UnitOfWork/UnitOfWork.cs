using Cinema_System.Application.Interfaces;
using Cinema_System.Infrastructure.Data;
using Cinema_System.Infrastructure.Repositories;

namespace Cinema_System.Infrastructure.UnitOfWork;

/// <summary>
/// Điều phối các Repository trên cùng một DbContext và commit transaction.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly CinemaWebDbContext _context;

    public UnitOfWork(CinemaWebDbContext context)
    {
        _context = context;
        Users = new UserRepository(_context);
    }

    public IUserRepository Users { get; }

    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();

    public void Dispose() => _context.Dispose();
}
