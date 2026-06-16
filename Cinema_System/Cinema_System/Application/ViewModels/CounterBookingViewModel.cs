using Cinema_System.Application.DTOs;

namespace Cinema_System.Application.ViewModels;

/// <summary>Dữ liệu khởi tạo cho trang đặt vé tại quầy (Counter Ticket Booking).</summary>
public class CounterBookingViewModel
{
    /// <summary>Danh sách phim đang có suất chiếu sắp tới.</summary>
    public IEnumerable<MovieOptionDTO> Movies { get; set; } = new List<MovieOptionDTO>();

    /// <summary>Đồ ăn/thức uống còn bán.</summary>
    public IEnumerable<FoodOptionDTO> Foods { get; set; } = new List<FoodOptionDTO>();

    /// <summary>Nhân viên đang thao tác (tạm lấy staff đầu tiên khi chưa có đăng nhập).</summary>
    public string ActingStaffName { get; set; } = string.Empty;

    public bool HasStaff { get; set; }

    /// <summary>Tỉ lệ VAT hiện hành (%) dùng để hiển thị, 0 nếu không có.</summary>
    public decimal VatRate { get; set; }
}
