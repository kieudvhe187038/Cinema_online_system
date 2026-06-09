using System.ComponentModel.DataAnnotations;

namespace Cinema_System.Application.ViewModels;

/// <summary>
/// Model cho View nhập mã OTP xác nhận email.
/// </summary>
public class VerifyOtpViewModel
{
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập mã OTP.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Mã OTP gồm 6 chữ số.")]
    [Display(Name = "Mã OTP")]
    public string Otp { get; set; } = null!;

    /// <summary>Số giây còn lại trước khi OTP hết hạn (phục vụ đếm ngược trên UI).</summary>
    public int OtpExpiresInSeconds { get; set; }
}
