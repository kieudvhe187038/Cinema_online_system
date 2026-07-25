using System.ComponentModel.DataAnnotations;

namespace Cinema_System.Application.ViewModels;

/// <summary>
/// Model cho View nhập OTP và mật khẩu mới khi đặt lại mật khẩu.
/// </summary>
public class ResetPasswordViewModel
{
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
    [StringLength(72, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 đến 72 ký tự.")]
    [RegularExpression(@"^[!-~]+$", ErrorMessage = "Mật khẩu chỉ gồm chữ, số và ký tự đặc biệt (không dấu tiếng Việt, không khoảng trắng hay emoji).")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu mới")]
    public string NewPassword { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu nhập lại không khớp.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nhập lại mật khẩu")]
    public string ConfirmPassword { get; set; } = null!;
}
