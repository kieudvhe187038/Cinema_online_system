using System.ComponentModel.DataAnnotations;

namespace Cinema_System.Application.ViewModels;

/// <summary>
/// Model cho View hoàn thiện hồ sơ khi đăng nhập Google lần đầu (bổ sung SĐT, ngày sinh).
/// </summary>
public class CompleteGoogleProfileViewModel : IValidatableObject
{
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(255, ErrorMessage = "Họ tên tối đa 255 ký tự.")]
    [RegularExpression(@"^[\p{L}\p{M} ]+$", ErrorMessage = "Họ tên chỉ gồm chữ cái và khoảng trắng, không chứa số hay ký tự đặc biệt.")]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [RegularExpression(@"^0\d{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ (bắt đầu bằng 0, 10-11 số).")]
    [Display(Name = "Số điện thoại")]
    public string Phone { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng chọn ngày sinh.")]
    [DataType(DataType.Date)]
    [Display(Name = "Ngày sinh")]
    public DateOnly? DateOfBirth { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DateOfBirth.HasValue)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (DateOfBirth.Value > today)
            {
                yield return new ValidationResult("Ngày sinh không được ở tương lai.", new[] { nameof(DateOfBirth) });
            }
            else if (DateOfBirth.Value < today.AddYears(-120))
            {
                yield return new ValidationResult("Ngày sinh không hợp lệ.", new[] { nameof(DateOfBirth) });
            }
            else if (DateOfBirth.Value > today.AddYears(-12))
            {
                yield return new ValidationResult("Bạn phải đủ 12 tuổi trở lên để đăng ký.", new[] { nameof(DateOfBirth) });
            }
        }
    }
}
