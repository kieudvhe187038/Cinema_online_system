using Cinema_System.Application.Common;
using Cinema_System.Application.Common.Models;

namespace Cinema_System.Application.Interfaces;

/// <summary>
/// Nghiệp vụ quên / đặt lại mật khẩu bằng OTP gửi qua email.
/// </summary>
public interface IPasswordResetService
{
    /// <summary>Bắt đầu quy trình: sinh OTP, gửi email, trả về phiên chờ.</summary>
    Task<Result<PendingPasswordReset>> StartResetAsync(string email);

    /// <summary>Gửi lại OTP (có giới hạn tần suất).</summary>
    Task<Result<PendingPasswordReset>> ResendOtpAsync(PendingPasswordReset pending);

    /// <summary>Kiểm tra OTP (bước riêng trước khi đổi mật khẩu). Không đổi mật khẩu.</summary>
    Result<bool> VerifyOtp(PendingPasswordReset pending, string otp);

    /// <summary>Đặt lại mật khẩu mới (chỉ khi OTP đã được xác thực).</summary>
    Task<Result<bool>> ResetPasswordAsync(PendingPasswordReset pending, string newPassword);
}
