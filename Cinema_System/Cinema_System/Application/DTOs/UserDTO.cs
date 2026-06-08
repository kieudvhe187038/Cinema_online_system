namespace Cinema_System.Application.DTOs;

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
