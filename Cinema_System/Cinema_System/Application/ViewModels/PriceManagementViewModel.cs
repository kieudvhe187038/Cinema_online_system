using Cinema_System.Application.DTOs;

namespace Cinema_System.Application.ViewModels;

/// <summary>
/// Dữ liệu cho màn quản lý giá: 4 nhóm cấu hình giá hiển thị theo tab.
/// </summary>
public class PriceManagementViewModel
{
    // Tab đang xem: Base | Room | Seat | Time (xem PriceKind).
    public string ActiveTab { get; set; } = Common.PriceKind.Base;

    public IReadOnlyList<PriceConfigDTO> BaseConfigs { get; set; } = new List<PriceConfigDTO>();
    public IReadOnlyList<PriceConfigDTO> RoomConfigs { get; set; } = new List<PriceConfigDTO>();
    public IReadOnlyList<PriceConfigDTO> SeatConfigs { get; set; } = new List<PriceConfigDTO>();
    public IReadOnlyList<PriceConfigDTO> TimeConfigs { get; set; } = new List<PriceConfigDTO>();

    public int BaseCount => BaseConfigs.Count;
    public int RoomCount => RoomConfigs.Count;
    public int SeatCount => SeatConfigs.Count;
    public int TimeCount => TimeConfigs.Count;
}
