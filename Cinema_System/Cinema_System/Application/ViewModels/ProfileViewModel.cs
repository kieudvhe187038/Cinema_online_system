namespace Cinema_System.Application.ViewModels
{
    public class ProfileViewModel
    {
        //ViewModel = "Túi đựng dữ liệu" Controller gửi sang View để hiển thị thông tin hồ sơ.
        //Các thuộc tính tương ứng cột trong bảng Users (theo thiết kế DB của nhóm).
        public Guid Id { get; set; }
        public Guid RoleId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public DateTime? DateOfBirth { get; set; }   // lấy từ Users.date_of_birth

        // Tuổi tính từ ngày sinh — dùng so với Movies.age_rating khi đặt vé.
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
