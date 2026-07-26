using System.ComponentModel.DataAnnotations;

namespace Cinema_System.Application.ViewModels;

/// <summary>Dữ liệu staff gửi lên khi tạo đơn đặt vé tại quầy + thanh toán.</summary>
public class CounterBookingRequest
{
    [Required(ErrorMessage = "Chưa chọn suất chiếu.")]
    public Guid ShowtimeId { get; set; }

    /// <summary>Danh sách ghế đã chọn.</summary>
    public List<Guid> SeatIds { get; set; } = new();

    /// <summary>Phiên giữ ghế của tab/máy quầy đang tạo đơn (khớp các dòng Seat_Holds do tab này giữ).</summary>
    public Guid HoldToken { get; set; }

    /// <summary>Đồ ăn kèm (có thể rỗng).</summary>
    public List<FoodOrderItemRequest> Foods { get; set; } = new();

    // --- Khách hàng (offline có thể là khách lẻ hoặc thành viên) ---
    /// <summary>Id thành viên nếu đã tra cứu gắn vào đơn (để tích điểm).</summary>
    public Guid? CustomerId { get; set; }

    public string? CustomerPhone { get; set; }

    /// <summary>Mã khuyến mãi áp cho đơn (tùy chọn).</summary>
    public string? PromoCode { get; set; }

    /// <summary>Số điểm thưởng của thành viên dùng để giảm giá (0 nếu không dùng hoặc là khách lẻ).</summary>
    public int PointsUsed { get; set; }

    // --- Thanh toán tại quầy ---
    [Required(ErrorMessage = "Chưa chọn phương thức thanh toán.")]
    public string PaymentMethod { get; set; } = "Cash";

    /// <summary>Số tiền khách đưa (chỉ dùng khi trả tiền mặt) - để tính tiền thừa.</summary>
    public decimal? CashReceived { get; set; }
}

public class FoodOrderItemRequest
{
    public Guid FbId { get; set; }

    public int Quantity { get; set; }
}
