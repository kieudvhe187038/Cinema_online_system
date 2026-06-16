using AutoMapper;
using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Services;

/// <summary>
/// Xử lý nghiệp vụ đăng nhập: tìm user theo email, kiểm tra mật khẩu (BCrypt),
/// kiểm tra trạng thái tài khoản và map sang UserDto.
/// </summary>
public class AuthService : IAuthService
{
    private const string GenericLoginError = "Email hoặc mật khẩu không chính xác.";

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public AuthService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<UserDto>> LoginAsync(LoginDto loginDto)
    {
        var email = loginDto.Email.Trim().ToLowerInvariant();
        var user = await _unitOfWork.Users.GetByEmailWithRoleAsync(email);

        // Không tiết lộ email tồn tại hay không để tránh dò tài khoản.
        if (user is null || string.IsNullOrEmpty(user.PasswordHash)
            || !VerifyPassword(loginDto.Password, user.PasswordHash))
        {
            return Result<UserDto>.Failure(GenericLoginError);
        }

        if (!string.Equals(user.Status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            return Result<UserDto>.Failure("Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.");
        }

        return Result<UserDto>.Success(_mapper.Map<UserDto>(user));
    }

    public async Task<Result<UserDto?>> ExternalLoginAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Result<UserDto?>.Failure("Không lấy được email từ Google.");
        }

        email = email.Trim().ToLowerInvariant();
        var user = await _unitOfWork.Users.GetByEmailWithRoleAsync(email);

        // Chưa có tài khoản -> cần hoàn thiện hồ sơ (Success với Data = null).
        if (user is null)
        {
            return Result<UserDto?>.Success(null);
        }

        if (!string.Equals(user.Status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            return Result<UserDto?>.Failure("Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.");
        }

        return Result<UserDto?>.Success(_mapper.Map<UserDto>(user));
    }

    public async Task<Result<UserDto>> CompleteExternalRegistrationAsync(string email, string fullName, string phone, DateOnly dateOfBirth)
    {
        email = email.Trim().ToLowerInvariant();
        phone = phone.Trim();

        if (await _unitOfWork.Users.EmailExistsAsync(email))
        {
            return Result<UserDto>.Failure("Email đã được sử dụng.");
        }

        if (await _unitOfWork.Users.PhoneExistsAsync(phone))
        {
            return Result<UserDto>.Failure("Số điện thoại đã được sử dụng.");
        }

        var role = await _unitOfWork.Roles.FirstOrDefaultAsync(r => r.Name == "CUSTOMER");
        if (role is null)
        {
            return Result<UserDto>.Failure("Hệ thống chưa cấu hình vai trò khách hàng. Vui lòng liên hệ quản trị viên.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            RoleId = role.Id,
            Role = role,
            FullName = fullName.Trim(),
            Email = email,
            Phone = phone,
            DateOfBirth = dateOfBirth,
            PasswordHash = null, // tài khoản Google không có mật khẩu cục bộ
            RewardPoints = 0,
            Status = "Active"
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return Result<UserDto>.Success(_mapper.Map<UserDto>(user));
    }

    private static bool VerifyPassword(string password, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Hash trong DB không đúng định dạng BCrypt (vd: dữ liệu seed mẫu).
            return false;
        }
    }
}
