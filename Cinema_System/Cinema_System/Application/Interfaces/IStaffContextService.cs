using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Interfaces;

/// <summary>
/// Cung cấp "nhân viên đang thao tác" cho các nghiệp vụ tại quầy, dựa trên
/// Id người dùng đang đăng nhập (Controller đọc từ Claims rồi truyền vào).
/// </summary>
public interface IStaffContextService
{
    Task<User?> GetCurrentStaffAsync(Guid staffId);
}
