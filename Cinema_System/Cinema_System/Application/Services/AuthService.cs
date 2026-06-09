using AutoMapper;
using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;

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
        if (user is null || string.IsNullOrEmpty(user.PasswordHash))
        {
            return Result<UserDto>.Failure(GenericLoginError);
        }

        if (!VerifyPassword(loginDto.Password, user.PasswordHash))
        {
            return Result<UserDto>.Failure(GenericLoginError);
        }

        if (!string.Equals(user.Status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            return Result<UserDto>.Failure("Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.");
        }

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
