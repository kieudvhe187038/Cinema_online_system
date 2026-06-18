namespace Cinema_System.Application.DTOs;

/// <summary>
/// DTO hợp nhất cho 1 dòng cấu hình giá ở màn danh sách (dùng chung cho cả 4 loại).
/// Các trường thời gian (DayOfWeek/StartTime/...) chỉ có nghĩa khi Kind = "Time".
/// </summary>
public class PriceConfigDTO
{
    public string Kind { get; set; } = null!;
    public Guid Id { get; set; }

    // Đối tượng áp dụng: tên phim / loại phòng / loại ghế / mô tả điều kiện giờ.
    public string TargetName { get; set; } = null!;

    // Số tiền: giá cơ bản hoặc mức phụ thu (đồng).
    public decimal Amount { get; set; }

    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    // Trạng thái hiển thị suy ra theo thời gian (Scheduled/Active/Expired) — xem PriceDisplayStatus.
    public string DisplayStatus { get; set; } = Common.PriceDisplayStatus.Active;

    // Riêng giá cơ bản: true nếu là mức "Áp dụng chung cho mọi phim" (MovieId = null).
    public bool IsGlobalBase { get; set; }

    // Riêng cho loại "Time".
    public string? TimeCondition { get; set; }
    public int? DayOfWeek { get; set; }
    public DateOnly? SpecificDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public string? RuleGroup { get; set; }
    public int? Priority { get; set; }
}
