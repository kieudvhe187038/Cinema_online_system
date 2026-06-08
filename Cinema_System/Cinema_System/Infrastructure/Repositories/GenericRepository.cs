using System.Linq.Expressions;
using Cinema_System.Application.Interfaces;
using Cinema_System.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Cinema_System.Infrastructure.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly CinemaWebDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public GenericRepository(CinemaWebDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(Guid id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<IEnumerable<T>> GetAllAsync(
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null)
    {
        IQueryable<T> query = _dbSet.AsNoTracking();

        if (include is not null)
            query = include(query);

        if (predicate is not null)
            query = query.Where(predicate);

        if (orderBy is not null)
            query = orderBy(query);

        return await query.ToListAsync();
    }

    public async Task<T?> FirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IQueryable<T>>? include = null)
    {
        IQueryable<T> query = _dbSet;

        if (include is not null)
            query = include(query);

        return await query.FirstOrDefaultAsync(predicate);
    }

    public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.AnyAsync(predicate);
    }

    public async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
    }

    public void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    public void Remove(T entity)
    {
        _dbSet.Remove(entity);
    }
}
