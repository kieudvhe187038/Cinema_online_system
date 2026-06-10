namespace Cinema_System.Application.ViewModels
{
    public class ProfileViewModel
    {
        public Guid Id { get; set; }
        public Guid RoleId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public int? Age
        {
            get
            {
                if (DateOfBirth == null) return null;
                var today = DateTime.Today;
                int age = today.Year - DateOfBirth.Value.Year;
                if (DateOfBirth.Value.Date > today.AddYears(-age)) age--;
                return age;
            }
        }
        public string? AvatarUrl { get; set; }  
        public string RoleName { get; set; } = string.Empty;
        public int RewardPoints { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
