namespace Cinema_System.Application.ViewModels;

// Dữ liệu cho trang Dashboard quản trị
public class AdminDashboardViewModel
{
    public decimal Revenue30Days { get; set; }
    public int TotalUsers { get; set; }         // tổng người dùng tham gia web
    public int TotalMovies { get; set; }        // tổng số phim hiện có
    public int TotalPaidBookings { get; set; }
    public int ActiveUsers { get; set; }
    public double OccupancyRate { get; set; }   // %

    public IReadOnlyList<RevenuePointViewModel> RevenueTrend { get; set; } = Array.Empty<RevenuePointViewModel>();
    public IReadOnlyList<RecentBookingViewModel> RecentBookings { get; set; } = Array.Empty<RecentBookingViewModel>();
    public IReadOnlyList<HotMovieViewModel> HotMovies { get; set; } = Array.Empty<HotMovieViewModel>();

    public string RevenueDisplay => Revenue30Days.ToString("#,##0") + "đ";
    public string OccupancyDisplay => OccupancyRate.ToString("0.0") + "%";
}

// 1 cột trong biểu đồ doanh thu theo ngày
public class RevenuePointViewModel
{
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string DayLabel => Date.ToString("dd/MM");
}

// 1 dòng giao dịch gần đây
public class RecentBookingViewModel
{
    public string CustomerName { get; set; } = string.Empty;
    public string MovieTitle { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
    public decimal FinalAmount { get; set; }
    public string? PaymentStatus { get; set; }

    public string AmountDisplay => FinalAmount.ToString("#,##0") + "đ";
    public string StatusLabel => PaymentStatus switch
    {
        "Paid" => "Thành công",
        "Pending" => "Đang chờ",
        "Cancelled" => "Đã hủy",
        "Refunded" => "Đã hoàn",
        _ => PaymentStatus ?? "-"
    };
    public string StatusClass => PaymentStatus switch
    {
        "Paid" => "bg-green-100 text-green-700",
        "Pending" => "bg-yellow-100 text-yellow-700",
        "Cancelled" => "bg-red-100 text-red-700",
        "Refunded" => "bg-blue-100 text-blue-700",
        _ => "bg-gray-100 text-gray-600"
    };
}

// 1 phim trong danh sách bán chạy
public class HotMovieViewModel
{
    public string MovieTitle { get; set; } = string.Empty;
    public string? PosterUrl { get; set; }
    public int TicketsSold { get; set; }
    public decimal Revenue { get; set; }
    public string RevenueDisplay => Revenue.ToString("#,##0") + "đ";
}