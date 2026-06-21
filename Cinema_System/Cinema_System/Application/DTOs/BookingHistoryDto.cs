namespace Cinema_System.Application.DTOs
{
    // Một dòng lịch sử đặt vé (để hiển thị danh sách)
    public class BookingHistoryDto
    {
        public Guid Id { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public string? PosterUrl { get; set; }
        public DateTime StartTime { get; set; }
        public int SeatCount { get; set; }
        public decimal FinalAmount { get; set; }
        public string? PaymentStatus { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
