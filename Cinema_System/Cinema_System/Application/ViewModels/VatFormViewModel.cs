using System.ComponentModel.DataAnnotations;
using Cinema_System.Application.Common;

namespace Cinema_System.Application.ViewModels;

public class VatFormViewModel : IValidatableObject
{
    public Guid Id { get; set; }

    // Lưu dạng phân số (0.08 = 8%) để khớp cách các luồng đặt vé đang đọc VatRate trực tiếp.
    [Required(ErrorMessage = "Vui lòng nhập tỷ lệ VAT")]
    [Range(0, 1, ErrorMessage = "Tỷ lệ VAT phải nằm trong khoảng 0 đến 1 (VD: 0.08 = 8%)")]
    [Display(Name = "Tỷ lệ VAT")]
    public decimal VatRate { get; set; }

    [StringLength(255, ErrorMessage = "Mô tả tối đa 255 ký tự")]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn trạng thái")]
    [Display(Name = "Trạng thái")]
    public string Status { get; set; } = VatStatus.Active;

    // Validate liên trường: khớp đúng CK_VAT_status để request giả mạo (POST giá trị ngoài dropdown)
    // trả lỗi thân thiện thay vì lỗi 500 ở tầng DB.
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!VatStatus.AllowedValues.Contains(Status))
        {
            yield return new ValidationResult(
                "Trạng thái không hợp lệ.",
                new[] { nameof(Status) });
        }
    }
}
