using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Cinema_System.Application.ViewModels;

// Trang Index: thống kê + danh sách suất chiếu (kèm cờ đã có sự cố)
public class IncidentListViewModel
{
    public IncidentStatsViewModel Stats { get; set; } = new();
    public IReadOnlyList<IncidentShowtimeItemViewModel> Items { get; set; } = Array.Empty<IncidentShowtimeItemViewModel>();
    public string Scope { get; set; } = "all";   // all | live | today
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalItems { get; set; }
}

public class IncidentStatsViewModel
{
    public int ShowtimesToday { get; set; }
    public int IncidentsThisMonth { get; set; }
    public int TotalIncidents { get; set; }
}

// 1 dòng suất chiếu trong danh sách
public class IncidentShowtimeItemViewModel
{
    public Guid ShowtimeId { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public string? PosterUrl { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string RoomTypeName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public string? Status { get; set; }
    public string? RoomStatus { get; set; }
    public bool IsRoomUnderMaintenance => RoomStatus == "Maintenance" || RoomStatus == "Inactive";
    public int SeatsSold { get; set; }
    public int Capacity { get; set; }
    public bool HasIncident { get; set; }

    public string StatusLabel => Status switch
    {
        "Scheduled" => "Đã lên lịch",
        "Live" => "Đang chiếu",
        "Completed" => "Đã chiếu",
        "Cancelled" => "Đã hủy",
        _ => Status ?? "-"
    };
    public string StatusClass => Status switch
    {
        "Live" => "bg-green-100 text-green-700",
        "Cancelled" => "bg-red-100 text-red-700",
        "Completed" => "bg-gray-100 text-gray-600",
        _ => "bg-blue-100 text-blue-700"
    };
}

// Form khai báo sự cố (dùng cho cả GET hiển thị lẫn POST bind)
public class DeclareIncidentViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Vui lòng chọn suất chiếu.")]
    public Guid? ShowtimeId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mô tả sự cố.")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "Mô tả phải từ 5 đến 500 ký tự.")]
    public string Description { get; set; } = string.Empty;

    [Range(0, 5, ErrorMessage = "Hệ số hoàn điểm phải nằm trong khoảng 0 đến 5.")]
    public decimal RefundPointsRate { get; set; } = 1.0m;

    public Guid? CompensationPromoId { get; set; }
    public bool CancelShowtime { get; set; } = true;

    // Dữ liệu phụ trợ cho dropdown (không bind khi POST)
    public List<SelectListItem> ShowtimeOptions { get; set; } = new();
    public List<SelectListItem> PromoOptions { get; set; } = new();

    // Validation thêm: chặn chưa chọn suất + mô tả chỉ toàn khoảng trắng
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ShowtimeId is null || ShowtimeId == Guid.Empty)
            yield return new ValidationResult("Vui lòng chọn suất chiếu.", new[] { nameof(ShowtimeId) });

        if (!string.IsNullOrEmpty(Description) && string.IsNullOrWhiteSpace(Description))
            yield return new ValidationResult("Mô tả không được chỉ gồm khoảng trắng.", new[] { nameof(Description) });
    }
}

// Form bảo trì phòng theo khoảng thời gian (hủy + hoàn hàng loạt)
public class MaintainRoomViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Vui lòng chọn phòng.")]
    public Guid? RoomId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn thời gian bắt đầu.")]
    public DateTime? FromTime { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn thời gian kết thúc.")]
    public DateTime? ToTime { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập lý do bảo trì.")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "Mô tả phải từ 5 đến 500 ký tự.")]
    public string Description { get; set; } = string.Empty;

    [Range(0, 5, ErrorMessage = "Hệ số hoàn điểm phải nằm trong khoảng 0 đến 5.")]
    public decimal RefundPointsRate { get; set; } = 1.0m;

    public Guid? CompensationPromoId { get; set; }

    public List<SelectListItem> RoomOptions { get; set; } = new();
    public List<SelectListItem> PromoOptions { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (RoomId is null || RoomId == Guid.Empty)
            yield return new ValidationResult("Vui lòng chọn phòng.", new[] { nameof(RoomId) });

        // KHÔNG cho khóa trong quá khứ
        if (FromTime.HasValue && FromTime.Value < DateTime.Now)
            yield return new ValidationResult("Không được chọn thời gian bắt đầu trong quá khứ.", new[] { nameof(FromTime) });

        if (FromTime.HasValue && ToTime.HasValue && ToTime.Value <= FromTime.Value)
            yield return new ValidationResult("Thời gian kết thúc phải sau thời gian bắt đầu.", new[] { nameof(ToTime) });

        if (!string.IsNullOrEmpty(Description) && string.IsNullOrWhiteSpace(Description))
            yield return new ValidationResult("Mô tả không được chỉ gồm khoảng trắng.", new[] { nameof(Description) });
    }
}
