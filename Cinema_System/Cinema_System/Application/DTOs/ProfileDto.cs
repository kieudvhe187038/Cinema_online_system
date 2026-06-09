namespace Cinema_System.Application.DTOs
{
    // DTO = dữ liệu trao đổi giữa Service và Controller (không phải Entity, không phải ViewModel).
    public class ProfileDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? AvatarUrl { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public int RewardPoints { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class UpdateProfileDto
    {
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? AvatarUrl { get; set; }   // đường dẫn ảnh đã lưu (nếu có đổi)
    }
}
