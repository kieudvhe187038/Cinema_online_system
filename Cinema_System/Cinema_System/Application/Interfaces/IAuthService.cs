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

    /// <summary>
    /// Đăng nhập bằng nhà cung cấp ngoài (Google): TÌM user theo email (không tạo mới).
    /// Trả về: Success(user) nếu đã có; Success(null) nếu chưa có (cần hoàn thiện hồ sơ);
    /// Failure nếu email rỗng hoặc tài khoản bị khoá.
    /// </summary>
    Task<Result<UserDto?>> ExternalLoginAsync(string email);

    /// <summary>
    /// Tạo tài khoản mới (role CUSTOMER) cho người dùng đăng nhập Google lần đầu,
    /// sau khi họ đã hoàn thiện SĐT và ngày sinh.
    /// </summary>
    Task<Result<UserDto>> CompleteExternalRegistrationAsync(string email, string fullName, string phone, DateOnly dateOfBirth);
}
