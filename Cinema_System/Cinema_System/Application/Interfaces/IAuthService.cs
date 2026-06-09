using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;

namespace Cinema_System.Application.Interfaces;

/// <summary>
/// Nghiệp vụ xác thực tài khoản.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Kiểm tra email/mật khẩu và trả về thông tin người dùng nếu hợp lệ.
    /// </summary>
    Task<Result<UserDto>> LoginAsync(LoginDto loginDto);
}
