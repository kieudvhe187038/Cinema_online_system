using System;

namespace Cinema_System.Application.Common.Models;

/// <summary>
/// Thông tin đăng ký đang chờ xác nhận OTP. Được lưu tạm ở Session (Presentation),
/// tài khoản chỉ thực sự tạo sau khi OTP hợp lệ. Mật khẩu và OTP đều lưu dạng băm.
/// </summary>
public class PendingRegistration
{
    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public DateOnly DateOfBirth { get; set; }

    public string PasswordHash { get; set; } = null!;

    public string OtpHash { get; set; } = null!;

    public DateTime ExpiryAt { get; set; }

    public int AttemptCount { get; set; }

    /// <summary>Thời điểm OTP được gửi gần nhất (dùng để giới hạn tần suất gửi lại).</summary>
    public DateTime LastSentAt { get; set; }

    /// <summary>Số lần đã bấm "gửi lại OTP" (lần đầu được miễn cooldown).</summary>
    public int ResendCount { get; set; }
}
