namespace Cinema_System.Application.ViewModels;

// ViewModel 1 dòng lịch sử đặt vé (có thêm field hiển thị)
public class BookingHistoryViewModel
{
    public Guid Id { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public string? PosterUrl { get; set; }
    public DateTime StartTime { get; set; }
    public int SeatCount { get; set; }
    public decimal FinalAmount { get; set; }
    public string? PaymentStatus { get; set; }
    public DateTime? CreatedAt { get; set; }

    // Tiền dạng "120.000 đ"
    public string FinalAmountDisplay => FinalAmount.ToString("#,##0") + " đ";

    // Nhãn tiếng Việt cho trạng thái thanh toán
    public string PaymentStatusLabel => PaymentStatus switch
    {
        "Paid" => "Đã thanh toán",
        "Pending" => "Chờ thanh toán",
        "Failed" => "Thất bại",
        "Cancelled" => "Đã huỷ",
        _ => PaymentStatus ?? "—"
    };

    // Màu badge theo trạng thái
    public string PaymentStatusClass => PaymentStatus switch
    {
        "Paid" => "bg-green-500",
        "Pending" => "bg-yellow-500",
        "Failed" or "Cancelled" => "bg-red-500",
        _ => "bg-gray-400"
    };
}

// Gói 1 trang lịch sử đặt vé (phân trang)
public class BookingHistoryPageViewModel
{
    public List<BookingHistoryViewModel> Items { get; set; } = new();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }
}
