using System.Text.Json;
using AutoMapper;
using Cinema_System.Application.Common;
using Cinema_System.Application.Common.Models;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers;

/// <summary>
/// Xử lý đăng ký tài khoản và xác nhận email bằng OTP.
/// </summary>
public class RegisterController : AuthControllerBase
{
    /// <summary>Khoá Session lưu thông tin đăng ký đang chờ xác nhận OTP.</summary>
    private const string PendingRegistrationKey = "PendingRegistration";

    private readonly IRegistrationService _registrationService;
    private readonly IMapper _mapper;

    public RegisterController(IRegistrationService registrationService, IMapper mapper)
    {
        _registrationService = registrationService;
        _mapper = mapper;
    }

    [HttpGet("/register")]
    public IActionResult Register(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new RegisterViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost("/register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _registrationService.StartRegistrationAsync(_mapper.Map<RegisterDto>(model));

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        SavePending(result.Data!);
        return RedirectToAction(nameof(VerifyOtp));
    }

    [HttpGet("/register/verify-otp")]
    public IActionResult VerifyOtp()
    {
        var pending = GetPending();
        if (pending is null)
        {
            return RedirectToAction(nameof(Register));
        }

        var model = new VerifyOtpViewModel { Email = pending.Email };
        PopulateOtpTiming(model, pending);
        return View(model);
    }

    [HttpPost("/register/verify-otp")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel model)
    {
        var pending = GetPending();
        if (pending is null)
        {
            TempData["Message"] = "Phiên đăng ký đã hết hạn. Vui lòng đăng ký lại.";
            return RedirectToAction(nameof(Register));
        }

        // Email hiển thị từ Session, không post lên form -> bỏ lỗi "required" ngầm của ModelState.
        model.Email = pending.Email;
        ModelState.Remove(nameof(model.Email));
        if (!ModelState.IsValid)
        {
            PopulateOtpTiming(model, pending);
            return View(model);
        }

        var result = await _registrationService.CompleteRegistrationAsync(pending, model.Otp);
        if (!result.Succeeded)
        {
            // Lưu lại pending để cập nhật số lần thử sai.
            SavePending(pending);
            ModelState.AddModelError(string.Empty, result.Error!);
            PopulateOtpTiming(model, pending);
            return View(model);
        }

        HttpContext.Session.Remove(PendingRegistrationKey);
        await SignInUserAsync(result.Data!);
        TempData["Message"] = "Đăng ký thành công! Chào mừng bạn đến với CineStar.";
        return RedirectToAction("Index", "Home");
    }

    [HttpPost("/register/resend-otp")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendOtp()
    {
        var pending = GetPending();
        if (pending is null)
        {
            return RedirectToAction(nameof(Register));
        }

        var result = await _registrationService.ResendOtpAsync(pending);
        SavePending(pending);
        TempData["Message"] = result.Succeeded
            ? "Đã gửi lại mã OTP tới email của bạn."
            : result.Error;
        return RedirectToAction(nameof(VerifyOtp));
    }

    /// <summary>Tính số giây còn lại cho đếm ngược thời gian sống OTP trên UI.</summary>
    private static void PopulateOtpTiming(VerifyOtpViewModel model, PendingRegistration pending)
        => model.OtpExpiresInSeconds = Math.Max(0, (int)(pending.ExpiryAt - DateTime.UtcNow).TotalSeconds);

    private void SavePending(PendingRegistration pending)
        => HttpContext.Session.SetString(PendingRegistrationKey, JsonSerializer.Serialize(pending));

    private PendingRegistration? GetPending()
    {
        var json = HttpContext.Session.GetString(PendingRegistrationKey);
        return string.IsNullOrEmpty(json)
            ? null
            : JsonSerializer.Deserialize<PendingRegistration>(json);
    }
}
