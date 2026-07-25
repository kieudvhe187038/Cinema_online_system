using System.Security.Claims;
using Cinema_System.Application.DTOs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers.Auth;

/// <summary>
/// Lớp cơ sở chứa logic xác thực dùng chung cho LoginController và RegisterController.
/// </summary>
public abstract class AuthControllerBase : Controller
{
    /// <summary>Thời hạn giữ đăng nhập: ở lại cho tới khi đăng xuất hoặc hết 15 ngày.</summary>
    private static readonly TimeSpan AuthCookieDuration = TimeSpan.FromDays(15);

    /// <summary>Claim lưu đường dẫn ảnh đại diện để header hiển thị không cần truy vấn DB mỗi request.</summary>
    public const string AvatarUrlClaimType = "avatar_url";

    /// <summary>
    /// Đăng nhập người dùng: tạo Claims (kèm Role) và phát hành cookie persistent (15 ngày).
    /// </summary>
    protected async Task SignInUserAsync(UserDto user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.RoleName)
        };

        // Chỉ thêm claim avatar khi user có ảnh (tránh claim rỗng vô nghĩa).
        if (!string.IsNullOrEmpty(user.AvatarUrl))
            claims.Add(new Claim(AvatarUrlClaimType, user.AvatarUrl));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        // Giữ đăng nhập qua cả khi tắt trình duyệt; tự đăng xuất sau 15 ngày hoặc khi bấm Đăng xuất.
        var properties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.Add(AuthCookieDuration)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            properties);
    }

    /// <summary>
    /// Trang đích sau khi đăng nhập: ưu tiên returnUrl hợp lệ (vd bị chặn truy cập rồi mới đăng nhập);
    /// nếu không có thì điều hướng theo role — STAFF/MANAGER/ADMIN về đúng màn quản lý của họ,
    /// CUSTOMER (và role khác) về trang chủ.
    /// </summary>
    protected IActionResult RedirectAfterLogin(string roleName, string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return roleName switch
        {
            "ADMIN" => RedirectToAction("Index", "AdminDashboard"),
            "MANAGER" => RedirectToAction("Index", "Dashboard"),
            "STAFF" => RedirectToAction("Index", "StaffCounter"),
            _ => RedirectToAction("Index", "Home")
        };
    }
}
