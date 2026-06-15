namespace Cinema_System.Application.Interfaces
{
    // Gom các Repository dùng chung 1 DbContext và lưu (SaveChanges) 1 lần trong cùng 1 transaction.
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<T> Repository<T>() where T : class;
        Task<int> SaveChangesAsync();
    }
}
