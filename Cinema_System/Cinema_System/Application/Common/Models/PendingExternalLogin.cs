namespace Cinema_System.Application.Common.Models;

/// <summary>
/// Thông tin lấy từ nhà cung cấp ngoài (Google) cho tài khoản CHƯA tồn tại,
/// lưu tạm ở Session trong lúc người dùng hoàn thiện hồ sơ (SĐT, ngày sinh).
/// </summary>
public class PendingExternalLogin
{
    public string Email { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? ReturnUrl { get; set; }
}
