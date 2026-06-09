using System.Security.Claims;
using System.Text.Json;
using AutoMapper;
using Cinema_System.Application.Common;
using Cinema_System.Application.Common.Models;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers;

/// <summary>
/// Xử lý đăng nhập, đăng xuất và trang từ chối truy cập.
/// </summary>
public class LoginController : AuthControllerBase
{
    /// <summary>Cookie ghi nhớ thông tin đăng nhập (email + mật khẩu, đã mã hoá) để tự điền lần sau.</summary>
    private const string RememberedLoginCookie = "RememberedLogin";

    /// <summary>Thời hạn lưu thông tin đăng nhập đã ghi nhớ.</summary>
    private static readonly TimeSpan RememberedLoginDuration = TimeSpan.FromDays(30);

    /// <summary>Khoá Session lưu thông tin Google chờ hoàn thiện hồ sơ.</summary>
    private const string PendingExternalKey = "PendingExternalLogin";

    private readonly IAuthService _authService;
    private readonly IMapper _mapper;
    private readonly IDataProtector _protector;
    private readonly IAuthenticationSchemeProvider _schemeProvider;

    public LoginController(IAuthService authService, IMapper mapper,
        IDataProtectionProvider dataProtectionProvider, IAuthenticationSchemeProvider schemeProvider)
    {
        _authService = authService;
        _mapper = mapper;
        _protector = dataProtectionProvider.CreateProtector("Cinema.Account.RememberLogin.v1");
        _schemeProvider = schemeProvider;
    }

    [HttpGet("/login")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        // Tự điền thông tin đăng nhập đã ghi nhớ từ lần trước (nếu có).
        var model = new LoginViewModel { ReturnUrl = returnUrl };
        var remembered = ReadRememberedLogin();
        if (remembered is not null)
        {
            model.Email = remembered.Value.Email;
            model.Password = remembered.Value.Password;
            model.RememberMe = true;
        }

        return View(model);
    }

    [HttpPost("/login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _authService.LoginAsync(_mapper.Map<LoginDto>(model));

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        RememberLogin(model.Email, model.Password, model.RememberMe);
        await SignInUserAsync(result.Data!);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost("/logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    // ----- Đăng nhập bằng Google -----

    [HttpPost("/external-login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExternalLogin(string provider, string? returnUrl = null)
    {
        // Nếu chưa cấu hình provider (vd Google chưa có ClientId) thì báo nhẹ nhàng.
        var scheme = await _schemeProvider.GetSchemeAsync(provider);
        if (scheme is null)
        {
            TempData["Message"] = "Đăng nhập bằng Google chưa được cấu hình.";
            return RedirectToAction(nameof(Login));
        }

        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Login", new { returnUrl });
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, provider);
    }

    [HttpGet("/external-login/callback")]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null)
    {
        var result = await HttpContext.AuthenticateAsync("External");
        if (!result.Succeeded || result.Principal is null)
        {
            TempData["Message"] = "Đăng nhập Google thất bại hoặc đã bị huỷ.";
            return RedirectToAction(nameof(Login));
        }

        var email = result.Principal.FindFirstValue(ClaimTypes.Email);
        var fullName = result.Principal.FindFirstValue(ClaimTypes.Name) ?? email ?? string.Empty;

        // Xoá cookie tạm của handshake Google.
        await HttpContext.SignOutAsync("External");

        var loginResult = await _authService.ExternalLoginAsync(email ?? string.Empty);
        if (!loginResult.Succeeded)
        {
            TempData["Message"] = loginResult.Error;
            return RedirectToAction(nameof(Login));
        }

        // Đã có tài khoản -> đăng nhập luôn.
        if (loginResult.Data is not null)
        {
            await SignInUserAsync(loginResult.Data);
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        // Chưa có tài khoản -> yêu cầu hoàn thiện hồ sơ trước khi tạo.
        HttpContext.Session.SetString(PendingExternalKey, JsonSerializer.Serialize(new PendingExternalLogin
        {
            Email = email!,
            FullName = fullName,
            ReturnUrl = returnUrl
        }));
        return RedirectToAction(nameof(CompleteProfile));
    }

    [HttpGet("/external-login/complete")]
    public IActionResult CompleteProfile()
    {
        var pending = GetPendingExternal();
        if (pending is null)
        {
            return RedirectToAction(nameof(Login));
        }

        return View(new CompleteGoogleProfileViewModel
        {
            Email = pending.Email,
            FullName = pending.FullName
        });
    }

    [HttpPost("/external-login/complete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteProfile(CompleteGoogleProfileViewModel model)
    {
        var pending = GetPendingExternal();
        if (pending is null)
        {
            TempData["Message"] = "Phiên đăng nhập Google đã hết hạn. Vui lòng thử lại.";
            return RedirectToAction(nameof(Login));
        }

        // Email lấy từ Google (Session), không cho sửa -> bỏ lỗi "required" ngầm.
        model.Email = pending.Email;
        ModelState.Remove(nameof(model.Email));
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _authService.CompleteExternalRegistrationAsync(
            pending.Email, model.FullName, model.Phone, model.DateOfBirth!.Value);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        HttpContext.Session.Remove(PendingExternalKey);
        await SignInUserAsync(result.Data!);

        if (!string.IsNullOrEmpty(pending.ReturnUrl) && Url.IsLocalUrl(pending.ReturnUrl))
        {
            return Redirect(pending.ReturnUrl);
        }
        return RedirectToAction("Index", "Home");
    }

    [HttpGet("/access-denied")]
    public IActionResult AccessDenied() => View();

    private PendingExternalLogin? GetPendingExternal()
    {
        var json = HttpContext.Session.GetString(PendingExternalKey);
        return string.IsNullOrEmpty(json)
            ? null
            : JsonSerializer.Deserialize<PendingExternalLogin>(json);
    }

    /// <summary>
    /// "Lưu thông tin đăng nhập": nếu được chọn thì ghi nhớ email + mật khẩu (đã mã hoá bằng
    /// Data Protection) vào cookie để tự điền cho lần sau; nếu bỏ chọn thì xoá.
    /// CẢNH BÁO: lưu mật khẩu vẫn có rủi ro nếu thiết bị bị truy cập trái phép.
    /// </summary>
    private void RememberLogin(string email, string password, bool remember)
    {
        if (remember)
        {
            var payload = _protector.Protect(JsonSerializer.Serialize(new RememberedLogin(email, password)));
            Response.Cookies.Append(RememberedLoginCookie, payload, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.Add(RememberedLoginDuration),
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax
            });
        }
        else
        {
            Response.Cookies.Delete(RememberedLoginCookie);
        }
    }

    private RememberedLogin? ReadRememberedLogin()
    {
        var cookie = Request.Cookies[RememberedLoginCookie];
        if (string.IsNullOrEmpty(cookie))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<RememberedLogin>(_protector.Unprotect(cookie));
        }
        catch
        {
            // Cookie hỏng hoặc khoá mã hoá đã đổi -> bỏ qua, coi như chưa ghi nhớ.
            return null;
        }
    }

    private readonly record struct RememberedLogin(string Email, string Password);
}
