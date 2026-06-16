using Cinema_System.Application.Common;
using Cinema_System.Application.Common.Models;
using Cinema_System.Application.DTOs;

namespace Cinema_System.Application.Interfaces;

/// <summary>
/// Nghiệp vụ đăng ký tài khoản có xác nhận email bằng OTP.
/// </summary>
public interface IRegistrationService
{
    /// <summary>
    /// Kiểm tra email/SĐT trùng, sinh OTP, gửi email và trả về thông tin chờ xác nhận
    /// (Presentation lưu vào Session). KHÔNG tạo tài khoản ở bước này.
    /// </summary>
    Task<Result<PendingRegistration>> StartRegistrationAsync(RegisterDto dto);

    /// <summary>
    /// Sinh OTP mới và gửi lại email cho phiên đăng ký đang chờ.
    /// </summary>
    Task<Result<PendingRegistration>> ResendOtpAsync(PendingRegistration pending);

    /// <summary>
    /// Xác minh OTP, nếu hợp lệ thì tạo tài khoản và trả về thông tin người dùng.
    /// Khi sai OTP, <paramref name="pending"/> sẽ được tăng AttemptCount (Presentation lưu lại Session).
    /// </summary>
    Task<Result<UserDto>> CompleteRegistrationAsync(PendingRegistration pending, string otp);
}
