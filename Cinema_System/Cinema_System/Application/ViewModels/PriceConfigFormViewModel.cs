using System.ComponentModel.DataAnnotations;
using Cinema_System.Application.Common;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Cinema_System.Application.ViewModels;

/// <summary>
/// Form hợp nhất Thêm/Sửa cho cả 4 loại cấu hình giá, phân nhánh theo <see cref="Kind"/>.
/// effective_to và trạng thái KHÔNG do người dùng nhập — hệ thống tự quản theo dòng thời gian
/// (mỗi cấu hình tự kết thúc khi có cấu hình mới bắt đầu; tự "hết hạn" theo thời gian).
/// </summary>
public class PriceConfigFormViewModel : IValidatableObject
{
    [Required]
    public string Kind { get; set; } = PriceKind.Base;

    public Guid Id { get; set; }

    // Mốc bắt đầu áp dụng. Có thể đặt ở tương lai để "đặt lịch" đổi giá.
    [Required(ErrorMessage = "Vui lòng chọn thời điểm bắt đầu áp dụng")]
    [DataType(DataType.DateTime)]
    [Display(Name = "Áp dụng từ")]
    public DateTime EffectiveFrom { get; set; } = DateTime.Today;

    // ----- Giá cơ bản (Base) -----
    [Display(Name = "Áp dụng cho phim")]
    public Guid? MovieId { get; set; }

    [Range(typeof(decimal), "0", "100000000", ErrorMessage = "Giá cơ bản phải từ 0 đến 100.000.000 ₫")]
    [Display(Name = "Giá cơ bản (₫)")]
    public decimal BasePrice { get; set; }

    // ----- Phụ thu loại phòng (Room) -----
    [Display(Name = "Loại phòng")]
    public Guid? RoomTypeId { get; set; }

    [Range(typeof(decimal), "0", "100000000", ErrorMessage = "Phụ thu phải từ 0 đến 100.000.000 ₫")]
    [Display(Name = "Phụ thu loại phòng (₫)")]
    public decimal TypeSurcharge { get; set; }

    // ----- Phụ thu loại ghế (Seat) -----
    [Display(Name = "Loại ghế")]
    public Guid? SeatTypeId { get; set; }

    [Range(typeof(decimal), "0", "100000000", ErrorMessage = "Phụ thu phải từ 0 đến 100.000.000 ₫")]
    [Display(Name = "Phụ thu loại ghế (₫)")]
    public decimal SeatSurcharge { get; set; }

    // ----- Phụ thu theo giờ (Time) -----
    [Display(Name = "Nhóm quy tắc")]
    public string? RuleGroup { get; set; } = PriceRuleGroup.Day;

    // Điều kiện chi tiết: chỉ dùng khi RuleGroup = DAY (DayOfWeek | SpecificDate).
    [Display(Name = "Áp dụng theo")]
    public string? TimeCondition { get; set; } = PriceTimeCondition.DayOfWeek;

    [Display(Name = "Thứ trong tuần")]
    public int? DayOfWeek { get; set; }

    [Display(Name = "Ngày cụ thể")]
    public DateOnly? SpecificDate { get; set; }

    [Display(Name = "Từ giờ")]
    public TimeOnly? StartTime { get; set; }

    [Display(Name = "Đến giờ")]
    public TimeOnly? EndTime { get; set; }

    [Range(typeof(decimal), "0", "100000000", ErrorMessage = "Phụ thu phải từ 0 đến 100.000.000 ₫")]
    [Display(Name = "Phụ thu theo giờ (₫)")]
    public decimal TimeSurcharge { get; set; }

    [Display(Name = "Độ ưu tiên")]
    [Range(0, int.MaxValue, ErrorMessage = "Độ ưu tiên phải từ 0 trở lên")]
    public int Priority { get; set; }

    // ----- Dữ liệu dropdown (controller đổ vào, không post lên) -----
    public IEnumerable<SelectListItem> MovieOptions { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> RoomTypeOptions { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> SeatTypeOptions { get; set; } = new List<SelectListItem>();

    // Validate liên trường theo từng loại giá — bổ sung cho các DataAnnotation ([Range]/[Required]).
    // Mục tiêu: chặn giá trị xấu trước khi xuống DB (tránh vi phạm CHECK → 500).
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!PriceKind.AllowedValues.Contains(Kind))
        {
            yield return new ValidationResult("Loại cấu hình giá không hợp lệ.", new[] { nameof(Kind) });
            yield break;
        }

        // EffectiveFrom là kiểu giá trị nên [Required] không bắt được giá trị mặc định/quá cũ.
        if (EffectiveFrom == default || EffectiveFrom.Year < 2000)
            yield return new ValidationResult("Thời điểm áp dụng không hợp lệ.", new[] { nameof(EffectiveFrom) });
        // Khi TẠO MỚI (Id rỗng) không cho đặt thời điểm áp dụng ở quá khứ (grace 1 phút tránh lệch giây).
        // Khi SỬA, việc "đổi sang mốc quá khứ" được PriceService kiểm (mốc cũ của mức đang chạy vẫn giữ được).
        else if (Id == Guid.Empty && EffectiveFrom < DateTime.Now.AddMinutes(-1))
            yield return new ValidationResult("Thời điểm áp dụng không được ở quá khứ.", new[] { nameof(EffectiveFrom) });

        switch (Kind)
        {
            case PriceKind.Base:
                break; // BasePrice đã có [Range].

            case PriceKind.Room:
                if (RoomTypeId is null || RoomTypeId == Guid.Empty)
                    yield return new ValidationResult("Vui lòng chọn loại phòng.", new[] { nameof(RoomTypeId) });
                break;

            case PriceKind.Seat:
                if (SeatTypeId is null || SeatTypeId == Guid.Empty)
                    yield return new ValidationResult("Vui lòng chọn loại ghế.", new[] { nameof(SeatTypeId) });
                break;

            case PriceKind.Time:
                foreach (var r in ValidateTime()) yield return r;
                break;
        }
    }

    private IEnumerable<ValidationResult> ValidateTime()
    {
        if (string.IsNullOrWhiteSpace(RuleGroup) || !PriceRuleGroup.AllowedValues.Contains(RuleGroup))
        {
            yield return new ValidationResult("Vui lòng chọn nhóm quy tắc (Theo loại ngày hoặc Theo khung giờ).", new[] { nameof(RuleGroup) });
            yield break;
        }

        if (RuleGroup == PriceRuleGroup.Time)
        {
            // Khung giờ trong ngày (định dạng 24 giờ). Cho phép qua đêm: end < start = sang ngày hôm sau.
            // Chỉ cấm end == start (0 giờ / trùng — mơ hồ, và vi phạm CK_PTime_clock <>).
            if (StartTime is null || EndTime is null)
                yield return new ValidationResult("Vui lòng nhập đủ khung giờ bắt đầu và kết thúc (24 giờ).", new[] { nameof(StartTime) });
            else if (EndTime.Value == StartTime.Value)
                yield return new ValidationResult("Giờ kết thúc phải khác giờ bắt đầu (qua đêm thì đặt giờ kết thúc nhỏ hơn giờ bắt đầu).", new[] { nameof(EndTime) });
        }
        else // DAY
        {
            if (TimeCondition != PriceTimeCondition.DayOfWeek && TimeCondition != PriceTimeCondition.SpecificDate)
            {
                yield return new ValidationResult("Vui lòng chọn áp dụng theo thứ trong tuần hoặc ngày cụ thể.", new[] { nameof(TimeCondition) });
            }
            else if (TimeCondition == PriceTimeCondition.DayOfWeek)
            {
                if (DayOfWeek is null || DayOfWeek < 1 || DayOfWeek > 7)
                    yield return new ValidationResult("Vui lòng chọn thứ trong tuần.", new[] { nameof(DayOfWeek) });
            }
            else if (SpecificDate is null)
            {
                yield return new ValidationResult("Vui lòng chọn ngày cụ thể.", new[] { nameof(SpecificDate) });
            }
        }
    }
}
