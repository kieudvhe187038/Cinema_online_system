using System.ComponentModel.DataAnnotations;

namespace Cinema_System.Application.ViewModels
{
    // ViewModel form Chỉnh sửa hồ sơ. Các attribute là Data Annotations -> ASP.NET tự validate khi submit.
    public class UpdateProfileViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [Display(Name = "Họ và tên")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên từ 2 đến 100 ký tự")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Display(Name = "Số điện thoại")]
        [RegularExpression(@"^(0\d{9})$", ErrorMessage = "Số điện thoại phải gồm 10 số và bắt đầu bằng 0")]
        public string? Phone { get; set; }

        [Display(Name = "Email")]
        public string? Email { get; set; }

        public string? CurrentAvatarUrl { get; set; } // ảnh hiện tại để preview

        [Display(Name = "Ảnh đại diện mới")]
        public IFormFile? AvatarFile { get; set; } // file upload từ máy
    }
}
