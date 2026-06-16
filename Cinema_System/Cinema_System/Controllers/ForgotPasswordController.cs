using System.Text.Json;
using Cinema_System.Application.Common;
using Cinema_System.Application.Common.Models;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers;

/// <summary>
/// Quên mật khẩu (3 bước): nhập email -> nhập OTP (xác thực) -> đặt mật khẩu mới.
/// </summary>
public class ForgotPasswordController : Controller
{
    /// <summary>Khoá Session lưu yêu cầu đặt lại mật khẩu đang chờ.</summary>
    private const string PendingResetKey = "PendingPasswordReset";

    private readonly IPasswordResetService _passwordResetService;

    public ForgotPasswordController(IPasswordResetService passwordResetService)
    {
        _passwordResetService = passwordResetService;
    }

    // ----- Bước 1: nhập email -----

    [HttpGet("/forgot-password")]
    public IActionResult ForgotPassword()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new ForgotPasswordViewModel());
    }

    [HttpPost("/forgot-password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _passwordResetService.StartResetAsync(model.Email);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        SavePending(result.Data!);
        return RedirectToAction(nameof(VerifyOtp));
    }

    // ----- Bước 2: nhập OTP -----

    [HttpGet("/forgot-password/verify-otp")]
    public IActionResult VerifyOtp()
    {
        var pending = GetPending();
        if (pending is null)
        {
            return RedirectToAction(nameof(ForgotPassword));
        }

        var model = new VerifyOtpViewModel { Email = pending.Email };
        PopulateOtpTiming(model, pending);
        return View(model);
    }

    [HttpPost("/forgot-password/verify-otp")]
    [ValidateAntiForgeryToken]
    public IActionResult VerifyOtp(VerifyOtpViewModel model)
    {
        var pending = GetPending();
        if (pending is null)
        {
            TempData["Message"] = "Phiên đặt lại mật khẩu đã hết hạn. Vui lòng thử lại.";
            return RedirectToAction(nameof(ForgotPassword));
        }

        model.Email = pending.Email;
        ModelState.Remove(nameof(model.Email));
        if (!ModelState.IsValid)
        {
            PopulateOtpTiming(model, pending);
            return View(model);
        }

        var result = _passwordResetService.VerifyOtp(pending, model.Otp);
        SavePending(pending); // lưu lại số lần thử / cờ đã xác thực
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            PopulateOtpTiming(model, pending);
            return View(model);
        }

        return RedirectToAction(nameof(Reset));
    }

    // ----- Bước 3: đặt mật khẩu mới (chỉ khi đã xác thực OTP) -----

    [HttpGet("/forgot-password/reset")]
    public IActionResult Reset()
    {
        var pending = GetPending();
        if (pending is null || !pending.OtpVerified)
        {
            return RedirectToAction(nameof(ForgotPassword));
        }

        return View(new ResetPasswordViewModel { Email = pending.Email });
    }

    [HttpPost("/forgot-password/reset")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reset(ResetPasswordViewModel model)
    {
        var pending = GetPending();
        if (pending is null || !pending.OtpVerified)
        {
            TempData["Message"] = "Phiên đặt lại mật khẩu đã hết hạn. Vui lòng thử lại.";
            return RedirectToAction(nameof(ForgotPassword));
        }

        model.Email = pending.Email;
        ModelState.Remove(nameof(model.Email));
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _passwordResetService.ResetPasswordAsync(pending, model.NewPassword);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        HttpContext.Session.Remove(PendingResetKey);
        TempData["Message"] = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập bằng mật khẩu mới.";
        return RedirectToAction("Login", "Login");
    }

    [HttpPost("/forgot-password/resend-otp")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendOtp()
    {
        var pending = GetPending();
        if (pending is null)
        {
            return RedirectToAction(nameof(ForgotPassword));
        }

        var result = await _passwordResetService.ResendOtpAsync(pending);
        SavePending(pending);
        TempData["Message"] = result.Succeeded
            ? "Đã gửi lại mã OTP tới email của bạn."
            : result.Error;
        return RedirectToAction(nameof(VerifyOtp));
    }

    private static void PopulateOtpTiming(VerifyOtpViewModel model, PendingPasswordReset pending)
        => model.OtpExpiresInSeconds = Math.Max(0, (int)(pending.ExpiryAt - DateTime.UtcNow).TotalSeconds);

    private void SavePending(PendingPasswordReset pending)
        => HttpContext.Session.SetString(PendingResetKey, JsonSerializer.Serialize(pending));

    private PendingPasswordReset? GetPending()
    {
        var json = HttpContext.Session.GetString(PendingResetKey);
        return string.IsNullOrEmpty(json)
            ? null
            : JsonSerializer.Deserialize<PendingPasswordReset>(json);
    }
}
