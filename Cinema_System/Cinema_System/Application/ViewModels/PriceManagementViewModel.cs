using Cinema_System.Application.DTOs;

namespace Cinema_System.Application.ViewModels;

/// <summary>
/// Dữ liệu cho màn quản lý giá: 4 nhóm cấu hình giá hiển thị theo tab.
/// Chỉ tab đang xem (<see cref="ActiveTab"/>) được nạp dữ liệu (đã phân trang);
/// các tab khác chỉ giữ tổng số (badge).
/// </summary>
public class PriceManagementViewModel
{
    // Tab đang xem: Base | Room | Seat | Time (xem PriceKind).
    public string ActiveTab { get; set; } = Common.PriceKind.Base;

    // Chỉ danh sách của tab đang xem được nạp (theo trang); các tab còn lại rỗng.
    public IReadOnlyList<PriceConfigDTO> BaseConfigs { get; set; } = new List<PriceConfigDTO>();
    public IReadOnlyList<PriceConfigDTO> RoomConfigs { get; set; } = new List<PriceConfigDTO>();
    public IReadOnlyList<PriceConfigDTO> SeatConfigs { get; set; } = new List<PriceConfigDTO>();
    public IReadOnlyList<PriceConfigDTO> TimeConfigs { get; set; } = new List<PriceConfigDTO>();

    // Tổng số cấu hình mỗi tab (hiển thị trên badge tab) — không bị ảnh hưởng bởi phân trang.
    public int BaseCount { get; set; }
    public int RoomCount { get; set; }
    public int SeatCount { get; set; }
    public int TimeCount { get; set; }

    // Phân trang của tab đang xem.
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}
