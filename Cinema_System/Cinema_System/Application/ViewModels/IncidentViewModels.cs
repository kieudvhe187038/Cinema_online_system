using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Cinema_System.Application.ViewModels;

// Trang Index: thống kê + danh sách suất chiếu (kèm cờ đã có sự cố)
public class IncidentListViewModel
{
    public IncidentStatsViewModel Stats { get; set; } = new();
    public IReadOnlyList<IncidentShowtimeItemViewModel> Items { get; set; } = Array.Empty<IncidentShowtimeItemViewModel>();
    public string Scope { get; set; } = "all";   // all | live | today | incident
    public string? Search { get; set; }          // từ khóa tìm phim/phòng
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
    public DateTime EndTime { get; set; }
    public string? Status { get; set; }
    public string? RoomStatus { get; set; }
    public bool IsRoomUnderMaintenance => RoomStatus == "Maintenance" || RoomStatus == "Inactive";
    // Suất đã kết thúc -> không cho khai báo sự cố (đồng bộ với form chỉ nạp suất chưa Completed).
    public bool IsEnded => Status == "Completed" || EndTime < DateTime.Now;
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
