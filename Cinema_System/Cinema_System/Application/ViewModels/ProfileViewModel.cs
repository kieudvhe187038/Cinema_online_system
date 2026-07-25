namespace Cinema_System.Application.ViewModels;

// ViewModel cho trang Xem hồ sơ; có thêm Age tự tính.
public class ProfileViewModel
{
    public Guid Id { get; set; }
    public Guid RoleId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime? DateOfBirth { get; set; }

    // Tuổi tự tính từ ngày sinh (không lưu trong DB)
    public int? Age
    {
        get
        {
            if (DateOfBirth == null) return null;
            var today = DateTime.Today;
            int age = today.Year - DateOfBirth.Value.Year;
            if (DateOfBirth.Value.Date > today.AddYears(-age)) age--; // chưa tới sinh nhật năm nay thì -1
            return age;
        }
    }

    public string? AvatarUrl { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public int RewardPoints { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
