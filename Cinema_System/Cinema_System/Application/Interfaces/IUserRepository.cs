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

    /// <summary>Kiểm tra email đã tồn tại hay chưa.</summary>
    Task<bool> EmailExistsAsync(string email);

    /// <summary>Kiểm tra số điện thoại đã tồn tại hay chưa.</summary>
    Task<bool> PhoneExistsAsync(string phone);

    /// <summary>Một trang người dùng (kèm Role) đã lọc theo từ khóa / vai trò / trạng thái.</summary>
    Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedWithRoleAsync(
        string? search, Guid? roleId, string? status, int page, int pageSize);

    /// <summary>Lấy người dùng theo Id kèm Role.</summary>
    Task<User?> GetByIdWithRoleAsync(Guid id);

    /// <summary>Nhân viên (theo tên vai trò) đang hoạt động đầu tiên — dùng cho StaffContext.</summary>
    Task<User?> GetFirstActiveByRoleNameAsync(string roleName);

    /// <summary>Tra cứu người dùng theo số điện thoại (đã chuẩn hóa).</summary>
    Task<User?> GetByPhoneAsync(string phone);
}
