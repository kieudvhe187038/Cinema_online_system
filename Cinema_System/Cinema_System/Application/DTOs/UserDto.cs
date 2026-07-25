namespace Cinema_System.Application.DTOs;

/// <summary>
/// Dữ liệu người dùng trả về sau khi đăng nhập thành công.
/// Không chứa PasswordHash. Được map từ Entity User bằng AutoMapper.
/// </summary>
public class UserDto
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string RoleName { get; set; } = null!;

    public string? AvatarUrl { get; set; }

    public int RewardPoints { get; set; }
}
