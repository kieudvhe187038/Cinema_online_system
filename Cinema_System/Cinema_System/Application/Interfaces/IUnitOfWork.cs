namespace Cinema_System.Application.Interfaces
{
    // Gom các Repository + 1 lần lưu (SaveChanges) trong cùng 1 transaction.
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<T> Repository<T>() where T : class;
        Task<int> SaveChangesAsync();
    }
}
