using System.ComponentModel.DataAnnotations;
using Cinema_System.Application.Common;

namespace Cinema_System.Application.ViewModels;

public class PromotionFormViewModel : IValidatableObject
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã voucher")]
    [StringLength(50, ErrorMessage = "Mã voucher tối đa 50 ký tự")]
    [RegularExpression(@"^[A-Za-z0-9_-]+$",
        ErrorMessage = "Mã chỉ gồm chữ cái, số, gạch ngang (-) hoặc gạch dưới (_), không dấu, không khoảng trắng")]
    [Display(Name = "Mã voucher")]
    public string Code { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng chọn loại giảm giá")]
    [Display(Name = "Loại giảm giá")]
    public string DiscountType { get; set; } = PromotionDiscountType.Percent;

    [Required(ErrorMessage = "Vui lòng nhập giá trị giảm")]
    [Range(0.01, 1_000_000_000, ErrorMessage = "Giá trị giảm phải lớn hơn 0")]
    [Display(Name = "Giá trị giảm")]
    public decimal DiscountAmount { get; set; }

    [Range(0, 1_000_000_000, ErrorMessage = "Giá trị đơn tối thiểu không hợp lệ")]
    [Display(Name = "Giá trị đơn tối thiểu (₫)")]
    public int? MinOrderValue { get; set; } = 0;

    [Range(1, 1_000_000_000, ErrorMessage = "Mức giảm tối đa phải lớn hơn 0")]
    [Display(Name = "Mức giảm tối đa (₫)")]
    public int? MaxDiscountAmount { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn phạm vi áp dụng")]
    [Display(Name = "Phạm vi áp dụng")]
    public string ApplicableTarget { get; set; } = PromotionTarget.All;

    [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu")]
    [DataType(DataType.DateTime)]
    [Display(Name = "Hiệu lực từ")]
    public DateTime ValidFrom { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc")]
    [DataType(DataType.DateTime)]
    [Display(Name = "Hiệu lực đến")]
    public DateTime ValidTo { get; set; } = DateTime.Today.AddDays(7);

    [Range(1, int.MaxValue, ErrorMessage = "Giới hạn lượt dùng phải từ 1 trở lên")]
    [Display(Name = "Giới hạn lượt dùng")]
    public int? UsageLimit { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn trạng thái")]
    [Display(Name = "Trạng thái")]
    public string Status { get; set; } = PromotionStatus.Active;

    // Validate liên trường: thời gian hợp lệ + ràng buộc theo loại giảm giá (khớp các CK trong DB).
    // Mọi giá trị enum (loại giảm/phạm vi/trạng thái) đều được kiểm tra theo đúng tập CHECK constraint
    // để request bị giả mạo (POST giá trị ngoài dropdown) trả lỗi thân thiện thay vì 500 ở tầng DB.
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!PromotionDiscountType.AllowedValues.Contains(DiscountType))
        {
            yield return new ValidationResult(
                "Loại giảm giá không hợp lệ.",
                new[] { nameof(DiscountType) });
        }

        if (!PromotionTarget.AllowedValues.Contains(ApplicableTarget))
        {
            yield return new ValidationResult(
                "Phạm vi áp dụng không hợp lệ.",
                new[] { nameof(ApplicableTarget) });
        }

        if (!PromotionStatus.AllowedValues.Contains(Status))
        {
            yield return new ValidationResult(
                "Trạng thái không hợp lệ.",
                new[] { nameof(Status) });
        }

        if (ValidTo <= ValidFrom)
        {
            yield return new ValidationResult(
                "Ngày kết thúc phải sau ngày bắt đầu.",
                new[] { nameof(ValidTo) });
        }

        if (DiscountType == PromotionDiscountType.Percent &&
            (DiscountAmount <= 0 || DiscountAmount > 100))
        {
            yield return new ValidationResult(
                "Giảm theo phần trăm phải nằm trong khoảng (0; 100].",
                new[] { nameof(DiscountAmount) });
        }
    }
}
