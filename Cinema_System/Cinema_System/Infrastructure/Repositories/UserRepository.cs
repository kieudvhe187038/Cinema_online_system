using Cinema_System.Application.Interfaces;
using Cinema_System.Domain.Entities;
using Cinema_System.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Cinema_System.Infrastructure.Repositories;

/// <summary>
/// Repository cho User, kế thừa CRUD chung và bổ sung truy vấn theo email.
/// </summary>
public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(CinemaWebDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailWithRoleAsync(string email)
        => await _dbSet
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == email);
}
