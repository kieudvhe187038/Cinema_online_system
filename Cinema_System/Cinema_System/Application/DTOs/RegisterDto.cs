using System;

namespace Cinema_System.Application.DTOs;

/// <summary>
/// Dữ liệu đầu vào cho nghiệp vụ đăng ký tài khoản (Presentation -> Application).
/// </summary>
public class RegisterDto
{
    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public DateOnly DateOfBirth { get; set; }

    public string Password { get; set; } = null!;
}
