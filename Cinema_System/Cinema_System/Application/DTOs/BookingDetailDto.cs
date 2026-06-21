namespace Cinema_System.Application.DTOs
{
    // Chi tiết 1 đơn đặt vé
    public class BookingDetailDto
    {
        public Guid Id { get; set; }

        // Phim + suất
        public string MovieTitle { get; set; } = string.Empty;
        public string? PosterUrl { get; set; }
        public string? AgeRating { get; set; }
        public int? DurationMinutes { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string RoomName { get; set; } = string.Empty;

        // Ghế + đồ ăn
        public List<BookingSeatLineDto> Seats { get; set; } = new();
        public List<BookingFoodLineDto> Foods { get; set; } = new();

        // Tiền
        public decimal TotalAmount { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? VatAmount { get; set; }
        public decimal FinalAmount { get; set; }

        public string? PaymentStatus { get; set; }
        public string? BookingType { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? QrCode { get; set; }   // mã đặt vé
    }

    public class BookingSeatLineDto
    {
        public string SeatLabel { get; set; } = string.Empty;     // vd "A5"
        public string SeatTypeName { get; set; } = string.Empty;  // vd "VIP"
        public decimal Price { get; set; }
    }

    public class BookingFoodLineDto
    {
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
