namespace Cinema_System.Application.ViewModels;

public class BookingDetailViewModel
{
    public Guid Id { get; set; }

    public string MovieTitle { get; set; } = string.Empty;
    public string? PosterUrl { get; set; }
    public string? AgeRating { get; set; }
    public int? DurationMinutes { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string RoomName { get; set; } = string.Empty;

    public List<BookingSeatLine> Seats { get; set; } = new();
    public List<BookingFoodLine> Foods { get; set; } = new();

    public decimal TotalAmount { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? VatAmount { get; set; }
    public decimal FinalAmount { get; set; }

    public string? PaymentStatus { get; set; }
    public string? BookingType { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? QrCode { get; set; }

    // ----- Hiển thị -----
    public string TotalAmountDisplay => TotalAmount.ToString("#,##0") + " đ";
    public string DiscountDisplay => "-" + (DiscountAmount ?? 0).ToString("#,##0") + " đ";
    public string VatDisplay => (VatAmount ?? 0).ToString("#,##0") + " đ";
    public string FinalAmountDisplay => FinalAmount.ToString("#,##0") + " đ";

    public string PaymentStatusLabel => PaymentStatus switch
    {
        "Paid" => "Đã thanh toán",
        "Pending" => "Chờ thanh toán",
        "Failed" => "Thất bại",
        "Cancelled" => "Đã huỷ",
        _ => PaymentStatus ?? "—"
    };
    public string PaymentStatusClass => PaymentStatus switch
    {
        "Paid" => "bg-green-500",
        "Pending" => "bg-yellow-500",
        "Failed" or "Cancelled" => "bg-red-500",
        _ => "bg-gray-400"
    };

    // Chỉ đơn ĐÃ THANH TOÁN mới có vé điện tử / QR hợp lệ
    public bool IsPaid => PaymentStatus == "Paid";
}

public class BookingSeatLine
{
    public string SeatLabel { get; set; } = string.Empty;
    public string SeatTypeName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string PriceDisplay => Price.ToString("#,##0") + " đ";
}

public class BookingFoodLine
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public string LineTotalDisplay => (Price * Quantity).ToString("#,##0") + " đ";
}
