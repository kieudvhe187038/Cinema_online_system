using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IGenericRepository<User> Users { get; }

    IGenericRepository<Role> Roles { get; }

    IGenericRepository<SystemConfig> SystemConfigs { get; }

    Task<int> SaveChangesAsync();
}
