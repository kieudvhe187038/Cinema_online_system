using System.Security.Cryptography;
using AutoMapper;
using Cinema_System.Application.Common;
using Cinema_System.Application.Common.Models;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;
using Cinema_System.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Cinema_System.Application.Services;

/// <summary>
/// Nghiệp vụ đăng ký tài khoản có xác nhận email bằng OTP.
/// Tài khoản chỉ được tạo sau khi OTP hợp lệ; trước đó dữ liệu nằm ở Session (Presentation).
/// </summary>
public class RegistrationService : IRegistrationService
{
    private const string CustomerRoleName = "CUSTOMER";

    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly IMapper _mapper;
    private readonly ILogger<RegistrationService> _logger;

    public RegistrationService(IUnitOfWork unitOfWork, IEmailService emailService, IMapper mapper,
        ILogger<RegistrationService> logger)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<PendingRegistration>> StartRegistrationAsync(RegisterDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var phone = dto.Phone.Trim();

        if (dto.DateOfBirth > DateOnly.FromDateTime(DateTime.Today))
        {
            return Result<PendingRegistration>.Failure("Ngày sinh không hợp lệ.");
        }

        if (await _unitOfWork.Users.EmailExistsAsync(email))
        {
            return Result<PendingRegistration>.Failure("Email đã được sử dụng.");
        }

        if (await _unitOfWork.Users.PhoneExistsAsync(phone))
        {
            return Result<PendingRegistration>.Failure("Số điện thoại đã được sử dụng.");
        }

        var otp = GenerateOtp();
        var pending = new PendingRegistration
        {
            FullName = dto.FullName.Trim(),
            Email = email,
            Phone = phone,
            DateOfBirth = dto.DateOfBirth,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            OtpHash = BCrypt.Net.BCrypt.HashPassword(otp),
            ExpiryAt = DateTime.UtcNow.Add(OtpPolicy.Expiry),
            AttemptCount = 0,
            LastSentAt = DateTime.UtcNow
        };

        if (!await TrySendOtpEmailAsync(pending.Email, pending.FullName, otp))
        {
            return Result<PendingRegistration>.Failure("Không gửi được email xác nhận. Vui lòng kiểm tra lại email và thử lại.");
        }

        return Result<PendingRegistration>.Success(pending);
    }

    public async Task<Result<PendingRegistration>> ResendOtpAsync(PendingRegistration pending)
    {
        // Lần gửi lại đầu tiên được miễn cooldown; từ lần thứ 2 mới phải chờ đủ cooldown.
        if (pending.ResendCount > 0)
        {
            var elapsed = DateTime.UtcNow - pending.LastSentAt;
            if (elapsed < OtpPolicy.ResendCooldown)
            {
                var waitMinutes = Math.Ceiling((OtpPolicy.ResendCooldown - elapsed).TotalMinutes);
                return Result<PendingRegistration>.Failure(
                    $"Vui lòng chờ {waitMinutes} phút trước khi gửi lại mã OTP.");
            }
        }

        var otp = GenerateOtp();
        pending.OtpHash = BCrypt.Net.BCrypt.HashPassword(otp);
        pending.ExpiryAt = DateTime.UtcNow.Add(OtpPolicy.Expiry);
        pending.AttemptCount = 0;
        pending.LastSentAt = DateTime.UtcNow;
        pending.ResendCount++;

        if (!await TrySendOtpEmailAsync(pending.Email, pending.FullName, otp))
        {
            return Result<PendingRegistration>.Failure("Không gửi lại được email. Vui lòng thử lại sau.");
        }

        return Result<PendingRegistration>.Success(pending);
    }

    public async Task<Result<UserDto>> CompleteRegistrationAsync(PendingRegistration pending, string otp)
    {
        if (DateTime.UtcNow > pending.ExpiryAt)
        {
            return Result<UserDto>.Failure("Mã OTP đã hết hạn. Vui lòng gửi lại mã.");
        }

        if (pending.AttemptCount >= OtpPolicy.MaxVerifyAttempts)
        {
            return Result<UserDto>.Failure("Bạn đã nhập sai quá số lần cho phép. Vui lòng gửi lại mã.");
        }

        if (!BCrypt.Net.BCrypt.Verify(otp.Trim(), pending.OtpHash))
        {
            pending.AttemptCount++;
            var remaining = OtpPolicy.MaxVerifyAttempts - pending.AttemptCount;
            return Result<UserDto>.Failure(
                remaining > 0
                    ? $"Mã OTP không đúng. Bạn còn {remaining} lần thử."
                    : "Mã OTP không đúng. Bạn đã hết lượt thử, vui lòng gửi lại mã.");
        }

        // Kiểm tra lại lần cuối phòng trường hợp email/SĐT bị đăng ký xen giữa.
        if (await _unitOfWork.Users.EmailExistsAsync(pending.Email))
        {
            return Result<UserDto>.Failure("Email đã được sử dụng.");
        }

        var role = await _unitOfWork.Roles.FirstOrDefaultAsync(r => r.Name == CustomerRoleName);
        if (role is null)
        {
            return Result<UserDto>.Failure("Hệ thống chưa cấu hình vai trò khách hàng. Vui lòng liên hệ quản trị viên.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            RoleId = role.Id,
            Role = role,
            FullName = pending.FullName,
            Email = pending.Email,
            Phone = pending.Phone,
            DateOfBirth = pending.DateOfBirth,
            PasswordHash = pending.PasswordHash,
            RewardPoints = 0,
            Status = "Active"
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return Result<UserDto>.Success(_mapper.Map<UserDto>(user));
    }

    private static string GenerateOtp()
        => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    private async Task<bool> TrySendOtpEmailAsync(string email, string fullName, string otp)
    {
        var subject = "Mã xác nhận đăng ký CineStar";
        var body = $@"
<div style='font-family:Arial,sans-serif;max-width:480px;margin:auto'>
  <h2 style='color:#a04100'>CineStar</h2>
  <p>Xin chào <b>{fullName}</b>,</p>
  <p>Mã OTP xác nhận đăng ký tài khoản của bạn là:</p>
  <p style='font-size:32px;font-weight:bold;letter-spacing:6px;color:#f37021'>{otp}</p>
  <p>Mã có hiệu lực trong <b>{OtpPolicy.Expiry.TotalMinutes:0} phút</b>. Vui lòng không chia sẻ mã này cho bất kỳ ai.</p>
  <p style='color:#888;font-size:13px'>Nếu bạn không yêu cầu đăng ký, hãy bỏ qua email này.</p>
</div>";

        try
        {
            await _emailService.SendAsync(email, subject, body);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gửi email OTP tới {Email} thất bại.", email);
            return false;
        }
    }
}
