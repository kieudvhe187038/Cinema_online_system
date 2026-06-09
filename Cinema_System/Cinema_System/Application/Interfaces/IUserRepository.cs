using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Interfaces;

/// <summary>
/// Repository chuyên biệt cho User, bổ sung các truy vấn ngoài CRUD chung.
/// </summary>
public interface IUserRepository : IGenericRepository<User>
{
    /// <summary>
    /// Lấy User theo email (kèm thông tin Role) phục vụ đăng nhập.
    /// </summary>
    Task<User?> GetByEmailWithRoleAsync(string email);
}
