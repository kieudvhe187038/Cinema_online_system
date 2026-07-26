namespace Cinema_System.Application.ViewModels;

// Dữ liệu trang Báo cáo & Thống kê (Report Dashboard): KPI + biểu đồ theo khoảng thời gian.
public class ReportViewModel
{
    // Khoảng thời gian lọc (mặc định 30 ngày gần nhất).
    public DateOnly DateFrom { get; set; }
    public DateOnly DateTo { get; set; }

    // ── KPI tổng hợp trong khoảng ──
    public decimal TotalRevenue { get; set; }
    public int TotalBookings { get; set; }
    public int TotalTickets { get; set; }
    public decimal AvgOrderValue { get; set; }

    // ── Dữ liệu biểu đồ ──
    public List<DailyRevenuePoint> DailyRevenue { get; set; } = new();   // doanh thu theo ngày
    public List<TopMovieItem> TopMovies { get; set; } = new();           // top phim theo doanh thu vé
    public List<PaymentMethodItem> PaymentMethods { get; set; } = new(); // tỉ lệ phương thức thanh toán
}

public class DailyRevenuePoint
{
    public DateOnly Date { get; set; }
    public decimal Revenue { get; set; }
}

public class TopMovieItem
{
    public string Title { get; set; } = string.Empty;
    public int TicketsSold { get; set; }
    public decimal Revenue { get; set; }
}

public class PaymentMethodItem
{
    // Mã lưu trong DB (Cash/Transfer/VNPay/VietQR/Free).
    public string Method { get; set; } = string.Empty;

    // Nhãn tiếng Việt để hiển thị — xem PaymentMethod.Label.
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Amount { get; set; }
}
