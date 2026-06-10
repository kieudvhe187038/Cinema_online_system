namespace Cinema_System.Application.DTOs
{
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
        public string? AvatarUrl { get; set; } 
    }

    public class PointHistoryDto
    {
        public int PointsChanged {  get; set; }  // âm = trừ, dương = cộng
        public string ActionType {  get; set; } = string.Empty; // Earned / Redeemed / Refund_Rollback
        public string? Description {  get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
