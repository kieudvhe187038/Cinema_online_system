using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;

namespace Cinema_System.Application.Services;

public class AdminDashboardService : IAdminDashboardService
{
    private readonly IUnitOfWork _unitOfWork;

    public AdminDashboardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AdminDashboardViewModel> GetDashboardAsync()
    {
        var today = DateTime.Today;
        var windowStart = today.AddDays(-29); // 30 ngày gồm hôm nay

        // Đơn ĐÃ THANH TOÁN (kèm khách + phim) -> doanh thu, xu hướng
        var paidBookings = (await _unitOfWork.Bookings.GetAllAsync(
            b => b.PaymentStatus == "Paid",
            includeProperties: new[] { "User", "Showtime", "Showtime.Movie" })).ToList();

        var revenueWindow = paidBookings.Where(b => b.CreatedAt >= windowStart).ToList();

        // --- 4 ô thống kê ---
        var revenue30 = revenueWindow.Sum(b => b.FinalAmount);
        var totalPaid = paidBookings.Count;
        var totalUsers = await _unitOfWork.Users.CountAsync();
        var totalMovies = await _unitOfWork.Movies.CountAsync();
        var activeUsers = await _unitOfWork.Users.CountAsync(u => u.Status == "Active");

        // Tỉ lệ lấp đầy = ghế đã bán / tổng ghế các suất chiếu
        var soldSeats = await _unitOfWork.Tickets.CountAsync();
        var showtimes = await _unitOfWork.Showtimes.GetAllAsync(includeProperties: new[] { "Room" });
        var capacity = showtimes.Sum(s => s.Room?.TotalSeats ?? 0);
        var occupancy = capacity > 0 ? (double)soldSeats / capacity * 100 : 0;

        // --- Xu hướng doanh thu theo từng ngày trong 30 ngày ---
        var byDay = revenueWindow
            .GroupBy(b => b.CreatedAt!.Value.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.FinalAmount));

        var trend = Enumerable.Range(0, 30)
            .Select(i => windowStart.AddDays(i))
            .Select(d => new RevenuePointViewModel { Date = d, Amount = byDay.GetValueOrDefault(d) })
            .ToList();

        // --- 6 giao dịch gần nhất (mọi trạng thái) ---
        var recent = (await _unitOfWork.Bookings.GetAllAsync(
            includeProperties: new[] { "User", "Showtime", "Showtime.Movie" },
            orderBy: q => q.OrderByDescending(b => b.CreatedAt)))
            .Take(6)
            .Select(b => new RecentBookingViewModel
            {
                CustomerName = b.User?.FullName ?? "Khách vãng lai",
                MovieTitle = b.Showtime?.Movie?.Title ?? "-",
                CreatedAt = b.CreatedAt,
                FinalAmount = b.FinalAmount,
                PaymentStatus = b.PaymentStatus
            }).ToList();

        // --- Phim hot: bán nhiều vé nhất ---
        var tickets = await _unitOfWork.Tickets.GetAllAsync(
            includeProperties: new[] { "Showtime", "Showtime.Movie" });

        var hotMovies = tickets
            .Where(t => t.Showtime?.Movie != null)
            .GroupBy(t => new { t.Showtime!.Movie!.Id, t.Showtime.Movie.Title, t.Showtime.Movie.PosterUrl })
            .Select(g => new HotMovieViewModel
            {
                MovieTitle = g.Key.Title,
                PosterUrl = g.Key.PosterUrl,
                TicketsSold = g.Count(),
                Revenue = g.Sum(t => t.PriceAtBooking)
            })
            .OrderByDescending(m => m.TicketsSold)
            .Take(5)
            .ToList();

        return new AdminDashboardViewModel
        {
            Revenue30Days = revenue30,
            TotalUsers = totalUsers,
            TotalMovies = totalMovies,
            TotalPaidBookings = totalPaid,
            ActiveUsers = activeUsers,
            OccupancyRate = occupancy,
            RevenueTrend = trend,
            RecentBookings = recent,
            HotMovies = hotMovies
        };
    }
}