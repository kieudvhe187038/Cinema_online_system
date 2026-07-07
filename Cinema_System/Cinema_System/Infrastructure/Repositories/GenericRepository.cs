using System.Linq.Expressions;
using Cinema_System.Application.Interfaces;
using Cinema_System.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Cinema_System.Infrastructure.Repositories;

/// <summary>
/// Triển khai CRUD chung trên EF Core cho mọi Entity.
/// </summary>
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
        string[]? includeProperties = null,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null)
    {
        IQueryable<T> query = _dbSet.AsNoTracking();

        if (includeProperties is not null && includeProperties.Length > 0)
        {
            foreach (var inc in includeProperties)
            {
                query = query.Include(inc);
            }
        }

        if (include is not null)
            query = include(query);

        if (predicate is not null)
            query = query.Where(predicate);

        if (orderBy is not null)
            query = orderBy(query);

        return await query.ToListAsync();
    }

    public async Task<(IReadOnlyList<T> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        IQueryable<T> query = _dbSet.AsNoTracking();

        if (predicate is not null)
            query = query.Where(predicate);

        // Đếm trước khi Include/Skip/Take để câu COUNT gọn (không kéo navigation).
        var totalCount = await query.CountAsync();

        if (include is not null)
            query = include(query);

        if (orderBy is not null)
            query = orderBy(query);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    // Lấy 1 bản ghi theo điều kiện, dùng để kiểm tra tồn tại hoặc lấy chi tiết đơn lẻ
    public async Task<T?> FirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate,
        string[]? includeProperties = null,
        Func<IQueryable<T>, IQueryable<T>>? include = null)
    {
        IQueryable<T> query = _dbSet;

        if (includeProperties is not null && includeProperties.Length > 0)
        {
            foreach (var inc in includeProperties)
            {
                query = query.Include(inc);
            }
        }

        if (include is not null)
            query = include(query);

        return await query.FirstOrDefaultAsync(predicate);
    }

    public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.AnyAsync(predicate);
    }

    public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        return predicate is null
            ? await _dbSet.CountAsync()
            : await _dbSet.CountAsync(predicate);
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