using Cinema_System.Application.Common;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;

namespace Cinema_System.Application.Services;

// Tổng hợp số liệu cho trang Tổng quan quản lý (Manager Dashboard).
public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DashboardViewModel> GetDashboardAsync()
    {
        var now = DateTime.Now;
        var today = now.Date;
        var monthStart = new DateTime(now.Year, now.Month, 1);

        // Doanh thu: chỉ tính đơn đã thanh toán trong tháng, tách riêng phần hôm nay.
        var paidThisMonth = (await _unitOfWork.Bookings.GetAllAsync(
            predicate: b => b.PaymentStatus == "Paid" && b.CreatedAt >= monthStart)).ToList();
        var revenueMonth = paidThisMonth.Sum(b => b.FinalAmount);
        var revenueToday = paidThisMonth.Where(b => b.CreatedAt >= today).Sum(b => b.FinalAmount);

        // Vé bán tháng này: vé thuộc đơn đã thanh toán, chưa hủy.
        var ticketsSoldMonth = await _unitOfWork.Tickets.CountAsync(
            t => t.Booking.PaymentStatus == "Paid" && t.Booking.CreatedAt >= monthStart && t.Status != "Cancelled");

        // Phim đang chiếu / suất chiếu sắp tới / số khách hàng.
        var nowShowing = await _unitOfWork.Movies.CountAsync(m => m.Status == MovieStatus.NowShowing);
        var upcoming = await _unitOfWork.Showtimes.CountAsync(s => s.StartTime >= now && s.Status != "Cancelled");
        var customers = await _unitOfWork.Users.CountAsync(u => u.Role.Name == "CUSTOMER");

        // 5 đơn đặt vé gần đây nhất (đã kèm phim + khách, sắp xếp mới nhất tại DB).
        var (recent, _) = await _unitOfWork.Bookings.GetPagedListAsync(null, null, null, page: 1, pageSize: 5);
        var recentItems = recent.Select(b => new RecentBookingItem
        {
            Code = b.QrCode ?? b.Id.ToString("N")[..8].ToUpper(),
            MovieTitle = b.Showtime?.Movie?.Title ?? "(phim)",
            CustomerName = b.User?.FullName ?? "Khách vãng lai",
            FinalAmount = b.FinalAmount,
            PaymentStatus = b.PaymentStatus ?? string.Empty,
            CreatedAt = b.CreatedAt
        }).ToList();

        return new DashboardViewModel
        {
            RevenueToday = revenueToday,
            RevenueMonth = revenueMonth,
            TicketsSoldMonth = ticketsSoldMonth,
            NowShowingMovies = nowShowing,
            UpcomingShowtimes = upcoming,
            CustomerCount = customers,
            RecentBookings = recentItems
        };
    }
}
