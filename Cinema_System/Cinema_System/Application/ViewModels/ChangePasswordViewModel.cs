using System.ComponentModel.DataAnnotations;

namespace Cinema_System.Application.ViewModels;

// ViewModel form Đổi mật khẩu (validate qua Data Annotations).
public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu hiện tại")]
    public string OldPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
    [StringLength(72, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 đến 72 ký tự.")]
    [RegularExpression(@"^[!-~]+$", ErrorMessage = "Mật khẩu chỉ gồm chữ, số và ký tự đặc biệt (không dấu tiếng Việt, không khoảng trắng hay emoji).")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu mới")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu mới")]
    [DataType(DataType.Password)]
    [Display(Name = "Nhập lại mật khẩu mới")]
    [Compare("NewPassword", ErrorMessage = "Mật khẩu nhập lại không khớp")] // phải trùng NewPassword
    public string ConfirmPassword { get; set; } = string.Empty;
}
