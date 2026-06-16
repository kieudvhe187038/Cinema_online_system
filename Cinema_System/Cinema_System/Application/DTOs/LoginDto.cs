namespace Cinema_System.Application.DTOs;

/// <summary>
/// Dữ liệu đầu vào cho nghiệp vụ đăng nhập (đi từ Presentation -> Application).
/// </summary>
public class LoginDto
{
    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;
}
