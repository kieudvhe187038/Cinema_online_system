using Cinema_System.Application.DTOs;

namespace Cinema_System.Application.ViewModels;

// Dữ liệu trang chọn đồ ăn/nước trong luồng đặt vé.
public class FoodOrderViewModel
{
    public Guid ShowtimeId { get; set; }

    public string MovieTitle { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string CinemaName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }

    // Ghế user đang giữ (nhãn + giá) và tổng tiền ghế.
    public List<SelectedSeatItem> Seats { get; set; } = new();
    public decimal SeatTotal { get; set; }

    // Thời gian giữ ghế còn lại (giây) để tiếp tục đếm ngược xuyên trang.
    public int HoldSecondsLeft { get; set; }

    // Danh sách món còn bán.
    public List<FoodBeverageDTO> FoodItems { get; set; } = new();
}

// Một ghế đã chọn (hiển thị ở tóm tắt đơn hàng).
public class SelectedSeatItem
{
    public string Label { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
