namespace Cinema_System.Application.ViewModels;

// Dữ liệu trang quét VietQR: ảnh QR + thông tin chuyển khoản + dữ liệu để tạo đơn khi xác nhận.
public class VietQrViewModel
{
    public Guid ShowtimeId { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }

    public decimal Amount { get; set; }            // số tiền cần chuyển (đã trừ điểm)
    public string QrUrl { get; set; } = string.Empty;
    public string TransferContent { get; set; } = string.Empty;

    // Thông tin tài khoản nhận (hiển thị kèm QR).
    public string BankCode { get; set; } = string.Empty;
    public string AccountNo { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;

    // Mang theo lựa chọn đồ ăn + điểm để tạo đơn khi bấm "Đã chuyển khoản".
    public List<Guid> FoodIds { get; set; } = new();
    public List<int> FoodQtys { get; set; } = new();
    public int PointsUsed { get; set; }
    public string? PromoCode { get; set; }     // mã giảm giá mang theo để tạo đơn khi xác nhận

    public int HoldSecondsLeft { get; set; }
}
