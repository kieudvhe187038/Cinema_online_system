using System.Security.Cryptography;
using Cinema_System.Application.Common;
using Cinema_System.Application.Common.Models;
using Cinema_System.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Cinema_System.Application.Services;

/// <summary>
/// Quên / đặt lại mật khẩu bằng OTP. Phiên chờ lưu ở Session (Presentation);
/// mật khẩu chỉ được cập nhật sau khi OTP hợp lệ.
/// </summary>
public class PasswordResetService : IPasswordResetService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ILogger<PasswordResetService> _logger;

    public PasswordResetService(IUnitOfWork unitOfWork, IEmailService emailService,
        ILogger<PasswordResetService> logger)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<PendingPasswordReset>> StartResetAsync(string email)
    {
        email = email.Trim().ToLowerInvariant();
        var user = await _unitOfWork.Users.GetByEmailWithRoleAsync(email);
        if (user is null)
        {
            return Result<PendingPasswordReset>.Failure("Email không tồn tại trong hệ thống.");
        }

        var otp = GenerateOtp();
        var pending = new PendingPasswordReset
        {
            Email = email,
            OtpHash = BCrypt.Net.BCrypt.HashPassword(otp),
            ExpiryAt = DateTime.UtcNow.Add(OtpPolicy.Expiry),
            AttemptCount = 0,
            LastSentAt = DateTime.UtcNow
        };

        if (!await TrySendOtpEmailAsync(email, user.FullName, otp))
        {
            return Result<PendingPasswordReset>.Failure("Không gửi được email. Vui lòng thử lại sau.");
        }

        return Result<PendingPasswordReset>.Success(pending);
    }

    public async Task<Result<PendingPasswordReset>> ResendOtpAsync(PendingPasswordReset pending)
    {
        // Lần gửi lại đầu tiên được miễn cooldown; từ lần thứ 2 mới phải chờ đủ cooldown.
        if (pending.ResendCount > 0)
        {
            var elapsed = DateTime.UtcNow - pending.LastSentAt;
            if (elapsed < OtpPolicy.ResendCooldown)
            {
                var waitMinutes = Math.Ceiling((OtpPolicy.ResendCooldown - elapsed).TotalMinutes);
                return Result<PendingPasswordReset>.Failure(
                    $"Vui lòng chờ {waitMinutes} phút trước khi gửi lại mã OTP.");
            }
        }

        var otp = GenerateOtp();
        pending.OtpHash = BCrypt.Net.BCrypt.HashPassword(otp);
        pending.ExpiryAt = DateTime.UtcNow.Add(OtpPolicy.Expiry);
        pending.AttemptCount = 0;
        pending.LastSentAt = DateTime.UtcNow;
        pending.ResendCount++;
        pending.OtpVerified = false;

        // Lấy tên để cá nhân hoá email (không bắt buộc).
        var user = await _unitOfWork.Users.GetByEmailWithRoleAsync(pending.Email);
        if (!await TrySendOtpEmailAsync(pending.Email, user?.FullName ?? "bạn", otp))
        {
            return Result<PendingPasswordReset>.Failure("Không gửi lại được email. Vui lòng thử lại sau.");
        }

        return Result<PendingPasswordReset>.Success(pending);
    }

    public Result<bool> VerifyOtp(PendingPasswordReset pending, string otp)
    {
        if (DateTime.UtcNow > pending.ExpiryAt)
        {
            return Result<bool>.Failure("Mã OTP đã hết hạn. Vui lòng gửi lại mã.");
        }

        if (pending.AttemptCount >= OtpPolicy.MaxVerifyAttempts)
        {
            return Result<bool>.Failure("Bạn đã nhập sai quá số lần cho phép. Vui lòng gửi lại mã.");
        }

        if (!BCrypt.Net.BCrypt.Verify(otp.Trim(), pending.OtpHash))
        {
            pending.AttemptCount++;
            var remaining = OtpPolicy.MaxVerifyAttempts - pending.AttemptCount;
            return Result<bool>.Failure(
                remaining > 0
                    ? $"Mã OTP không đúng. Bạn còn {remaining} lần thử."
                    : "Mã OTP không đúng. Bạn đã hết lượt thử, vui lòng gửi lại mã.");
        }

        pending.OtpVerified = true;
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> ResetPasswordAsync(PendingPasswordReset pending, string newPassword)
    {
        if (!pending.OtpVerified)
        {
            return Result<bool>.Failure("Bạn cần xác thực OTP trước khi đặt lại mật khẩu.");
        }

        var user = await _unitOfWork.Users.GetByEmailWithRoleAsync(pending.Email);
        if (user is null)
        {
            return Result<bool>.Failure("Tài khoản không tồn tại.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.UpdatedAt = DateTime.Now;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return Result<bool>.Success(true);
    }

    private static string GenerateOtp()
        => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    private async Task<bool> TrySendOtpEmailAsync(string email, string fullName, string otp)
    {
        var subject = "Mã đặt lại mật khẩu CineStar";
        var body = $@"
<div style='font-family:Arial,sans-serif;max-width:480px;margin:auto'>
  <h2 style='color:#a04100'>CineStar</h2>
  <p>Xin chào <b>{fullName}</b>,</p>
  <p>Mã OTP để đặt lại mật khẩu của bạn là:</p>
  <p style='font-size:32px;font-weight:bold;letter-spacing:6px;color:#f37021'>{otp}</p>
  <p>Mã có hiệu lực trong <b>{OtpPolicy.Expiry.TotalMinutes:0} phút</b>. Vui lòng không chia sẻ mã này cho bất kỳ ai.</p>
  <p style='color:#888;font-size:13px'>Nếu bạn không yêu cầu đặt lại mật khẩu, hãy bỏ qua email này.</p>
</div>";

        try
        {
            await _emailService.SendAsync(email, subject, body);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gửi email OTP đặt lại mật khẩu tới {Email} thất bại.", email);
            return false;
        }
    }
}
