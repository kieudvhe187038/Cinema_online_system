namespace Cinema_System.Application.Interfaces;

/// <summary>
/// Quản lý transaction và điều phối nhiều Repository trong cùng một phiên DbContext.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }

    Task<int> SaveChangesAsync();
}
