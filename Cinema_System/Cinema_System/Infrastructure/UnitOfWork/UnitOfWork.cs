using Cinema_System.Application.Interfaces;
using Cinema_System.Domain.Entities;
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
        Roles = new GenericRepository<Role>(_context);
    }

    public IUserRepository Users { get; }

    public IGenericRepository<Role> Roles { get; }

    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();

    public void Dispose() => _context.Dispose();
}
