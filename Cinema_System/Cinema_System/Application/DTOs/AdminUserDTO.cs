namespace Cinema_System.Application.DTOs;

// DTO người dùng cho trang quản trị (danh sách/chi tiết).
// Đặt trong file AdminUserDTO.cs để tránh trùng tên file với UserDto.cs trên Windows
// (hệ thống file không phân biệt hoa/thường); tên class vẫn là UserDTO theo code module quản trị.
public class UserDTO
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }

    public DateOnly DateOfBirth { get; set; }

    public int? RewardPoints { get; set; }

    public string? Status { get; set; }

    public Guid RoleId { get; set; }

    public string RoleName { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }
}
