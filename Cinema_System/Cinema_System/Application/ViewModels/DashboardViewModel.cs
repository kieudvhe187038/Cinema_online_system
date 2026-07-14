namespace Cinema_System.Application.ViewModels;

// Dữ liệu trang Tổng quan (Manager Dashboard): các chỉ số KPI + booking gần đây.
public class DashboardViewModel
{
    // ── KPI ──
    public decimal RevenueToday { get; set; }        // doanh thu hôm nay (đơn đã thanh toán)
    public decimal RevenueMonth { get; set; }        // doanh thu tháng này
    public int TicketsSoldMonth { get; set; }        // số vé bán tháng này
    public int NowShowingMovies { get; set; }        // số phim đang chiếu
    public int UpcomingShowtimes { get; set; }       // số suất chiếu sắp tới
    public int CustomerCount { get; set; }           // số khách hàng

    // ── Danh sách đơn đặt vé gần đây ──
    public List<RecentBookingItem> RecentBookings { get; set; } = new();
}

public class RecentBookingItem
{
    public string Code { get; set; } = string.Empty;
    public string MovieTitle { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal FinalAmount { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
}
