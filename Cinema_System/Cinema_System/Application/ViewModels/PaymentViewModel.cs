namespace Cinema_System.Application.ViewModels;

// Dữ liệu trang thanh toán: tóm tắt đơn (ghế + đồ ăn) + tổng tiền + thời gian giữ còn lại.
public class PaymentViewModel
{
    public Guid ShowtimeId { get; set; }

    public string MovieTitle { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string CinemaName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }

    public List<SelectedSeatItem> Seats { get; set; } = new();
    public decimal SeatTotal { get; set; }

    public List<FoodLineItem> FoodLines { get; set; } = new();
    public decimal FoodTotal { get; set; }

    public decimal GrandTotal { get; set; }
    public int HoldSecondsLeft { get; set; }
}

// Một dòng đồ ăn trong đơn (kèm số lượng để giữ lại khi xác nhận thanh toán).
public class FoodLineItem
{
    public Guid FbId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal LineTotal => Price * Quantity;
}

// Dữ liệu trang xác nhận đặt vé thành công.
public class PaymentSuccessViewModel
{
    public Guid BookingId { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public List<string> SeatLabels { get; set; } = new();
    public List<FoodLineItem> FoodLines { get; set; } = new();
    public decimal GrandTotal { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public int PointsEarned { get; set; }
    public int RewardPointsTotal { get; set; }
}

// Kết quả xử lý xác nhận đặt vé.
public class BookingConfirmResult
{
    public bool Succeeded { get; set; }
    public string? Error { get; set; }
    public Guid BookingId { get; set; }
    public int PointsEarned { get; set; }

    public static BookingConfirmResult Fail(string error) => new() { Succeeded = false, Error = error };
    public static BookingConfirmResult Ok(Guid bookingId, int points) =>
        new() { Succeeded = true, BookingId = bookingId, PointsEarned = points };
}
