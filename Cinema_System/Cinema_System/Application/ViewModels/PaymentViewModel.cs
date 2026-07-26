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

    // Tạm tính (vé + đồ ăn) trước thuế.
    public decimal Subtotal { get; set; }
    // Thuế VAT: tỷ lệ (vd 0.08) và số tiền thuế trên Subtotal.
    public decimal VatRate { get; set; }
    public decimal VatAmount { get; set; }
    // Tổng trước giảm = Subtotal + VatAmount.
    public decimal GrandTotal { get; set; }

    // Điểm thưởng để giảm giá: số điểm đang có, giá trị 1 điểm (₫), và số điểm tối đa được dùng cho đơn này.
    public int AvailablePoints { get; set; }
    public int PointValueVnd { get; set; }
    public int MaxUsablePoints { get; set; }

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
    public string BookingCode { get; set; } = string.Empty;   // mã đơn để tạo QR quét vé
    public string MovieTitle { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string CinemaName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public List<string> SeatLabels { get; set; } = new();
    public List<FoodLineItem> FoodLines { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal VatRate { get; set; }
    public decimal VatAmount { get; set; }
    public int PointsUsed { get; set; }
    public decimal DiscountAmount { get; set; }          // giảm bằng điểm thưởng
    public string? PromoCode { get; set; }               // mã giảm giá đã dùng (nếu có)
    public decimal PromoDiscount { get; set; }           // giảm bằng mã khuyến mãi
    public decimal GrandTotal { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public int PointsEarned { get; set; }
    public int RewardPointsTotal { get; set; }
}

// Kết quả xem trước khi áp mã giảm giá ở trang thanh toán (gọi qua AJAX).
public class PromoPreviewResult
{
    public bool Ok { get; set; }
    public string? Message { get; set; }
    public string Code { get; set; } = string.Empty;
    public decimal PromoDiscount { get; set; }      // số tiền mã giảm
    public decimal FinalAmount { get; set; }        // tổng sau khi trừ mã (trước khi trừ điểm)
    public int MaxUsablePoints { get; set; }        // số điểm tối đa còn dùng được sau khi trừ mã
    public string? Target { get; set; }             // đối tượng áp dụng: Ticket_Only / Food_Only / All
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
