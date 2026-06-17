using Cinema_System.Application.Interfaces;
using Cinema_System.Domain.Entities;
using Cinema_System.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Cinema_System.Infrastructure.Repositories;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(CinemaWebDbContext context) : base(context)
    {
    }

    public Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedWithRoleAsync(
        string? search, Guid? roleId, string? status, int page, int pageSize)
    {
        var keyword = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        return GetPagedAsync(
            page, pageSize,
            predicate: u =>
                (keyword == null || u.FullName.Contains(keyword) || u.Email.Contains(keyword)
                    || (u.Phone != null && u.Phone.Contains(keyword)))
                && (roleId == null || u.RoleId == roleId)
                && (status == null || u.Status == status),
            include: q => q.Include(u => u.Role),
            orderBy: q => q.OrderByDescending(u => u.CreatedAt));
    }

    public async Task<User?> GetByIdWithRoleAsync(Guid id)
    {
        return await FirstOrDefaultAsync(u => u.Id == id, q => q.Include(u => u.Role));
    }

    public async Task<User?> GetFirstActiveByRoleNameAsync(string roleName)
    {
        return await FirstOrDefaultAsync(
            u => u.Status == "Active" && u.Role.Name == roleName,
            q => q.Include(u => u.Role));
    }

    public async Task<User?> GetByPhoneAsync(string phone)
    {
        return await FirstOrDefaultAsync(u => u.Phone == phone);
    }
}
