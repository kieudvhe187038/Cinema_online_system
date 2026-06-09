namespace Cinema_System.Application.Common;

/// <summary>
/// Chính sách OTP dùng chung giữa Service (sinh/kiểm tra OTP) và Controller (đếm ngược hiển thị).
/// </summary>
public static class OtpPolicy
{
    /// <summary>Thời gian sống của OTP.</summary>
    public static readonly TimeSpan Expiry = TimeSpan.FromMinutes(5);

    /// <summary>Khoảng chờ tối thiểu giữa 2 lần gửi OTP.</summary>
    public static readonly TimeSpan ResendCooldown = TimeSpan.FromMinutes(5);

    /// <summary>Số lần nhập sai OTP tối đa.</summary>
    public const int MaxVerifyAttempts = 5;
}
