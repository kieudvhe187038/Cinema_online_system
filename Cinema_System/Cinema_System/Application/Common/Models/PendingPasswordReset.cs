using System;

namespace Cinema_System.Application.Common.Models;

/// <summary>
/// Yêu cầu đặt lại mật khẩu đang chờ xác nhận OTP (lưu tạm ở Session).
/// </summary>
public class PendingPasswordReset
{
    public string Email { get; set; } = null!;

    public string OtpHash { get; set; } = null!;

    public DateTime ExpiryAt { get; set; }

    public int AttemptCount { get; set; }

    /// <summary>Thời điểm gửi OTP gần nhất (giới hạn tần suất gửi lại).</summary>
    public DateTime LastSentAt { get; set; }

    /// <summary>Số lần đã bấm "gửi lại OTP" (lần đầu được miễn cooldown).</summary>
    public int ResendCount { get; set; }

    /// <summary>Đã xác thực OTP thành công hay chưa (mới được phép sang bước đổi mật khẩu).</summary>
    public bool OtpVerified { get; set; }
}
