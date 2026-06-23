using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Interfaces;

/// <summary>
/// Cung cấp "nhân viên đang thao tác" cho các nghiệp vụ tại quầy.
/// Hiện hệ thống chưa có đăng nhập, nên tạm lấy nhân viên (role Staff) đầu tiên
/// đang hoạt động. Khi có đăng nhập, chỉ cần đổi phần lấy user ở đây.
/// </summary>
public interface IStaffContextService
{
    Task<User?> GetCurrentStaffAsync();
}
