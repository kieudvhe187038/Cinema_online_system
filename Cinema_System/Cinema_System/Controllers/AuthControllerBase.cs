using System.Security.Claims;
using Cinema_System.Application.DTOs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers;

/// <summary>
/// Lớp cơ sở chứa logic xác thực dùng chung cho LoginController và RegisterController.
/// </summary>
public abstract class AuthControllerBase : Controller
{
    /// <summary>Thời hạn giữ đăng nhập: ở lại cho tới khi đăng xuất hoặc hết 15 ngày.</summary>
    private static readonly TimeSpan AuthCookieDuration = TimeSpan.FromDays(15);

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
}
