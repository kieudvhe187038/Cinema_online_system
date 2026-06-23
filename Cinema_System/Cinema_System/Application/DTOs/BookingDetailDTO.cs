namespace Cinema_System.Application.DTOs;

/// <summary>Chi tiết một đơn đặt vé (trang chi tiết quản lý đơn).</summary>
public class BookingDetailDTO
{
    public Guid Id { get; set; }

    public string BookingCode { get; set; } = string.Empty;

    public string BookingType { get; set; } = string.Empty;

    public string PaymentStatus { get; set; } = string.Empty;

    public DateTime? CreatedAt { get; set; }

    // Suất chiếu
    public string MovieTitle { get; set; } = string.Empty;

    public string CinemaName { get; set; } = string.Empty;

    public string RoomName { get; set; } = string.Empty;

    public DateTime ShowStartTime { get; set; }

    public DateTime ShowEndTime { get; set; }

    // Khách & nhân viên
    public string CustomerName { get; set; } = string.Empty;

    public string? CustomerPhone { get; set; }

    public string? StaffName { get; set; }

    // Tiền
    public decimal TotalAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal VatAmount { get; set; }

    public decimal FinalAmount { get; set; }

    public List<BookingSeatLineDTO> Seats { get; set; } = new();

    public List<BookingFoodLineDTO> Foods { get; set; } = new();

    public List<BookingPaymentLineDTO> Payments { get; set; } = new();
}

public class BookingSeatLineDTO
{
    public string SeatLabel { get; set; } = string.Empty;

    public string SeatType { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string Status { get; set; } = string.Empty;
}

public class BookingFoodLineDTO
{
    public string Name { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal LineTotal => UnitPrice * Quantity;
}

public class BookingPaymentLineDTO
{
    public string Method { get; set; } = string.Empty;

    public string? Source { get; set; }

    public decimal Amount { get; set; }

    public decimal? CashReceived { get; set; }

    public decimal? ChangeAmount { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime? PaidAt { get; set; }
}
