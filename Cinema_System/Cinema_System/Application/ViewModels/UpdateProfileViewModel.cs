using System.ComponentModel.DataAnnotations;

namespace Cinema_System.Application.ViewModels
{
    public class UpdateProfileViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [Display(Name = "Họ và tên")]
        [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Số điện thoại")]
        [RegularExpression(@"^(0\d{9})$", ErrorMessage = "Số điện thoại phải gồm 10 số và bắt đầu bằng 0")]
        public string? Phone { get; set; }

        [Display(Name = "Email")]
        public string? Email { get; set; }

        // Ảnh hiện tại để hiển thị preview (không phải file upload)
        public string? CurrentAvatarUrl { get; set; }

        // File ảnh người dùng chọn để tải lên
        [Display(Name = "Ảnh đại diện mới")]
        public IFormFile? AvatarFile { get; set; }
    }
}
