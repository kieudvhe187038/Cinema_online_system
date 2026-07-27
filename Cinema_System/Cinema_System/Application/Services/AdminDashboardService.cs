using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.EntityFrameworkCore;

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

        // --- 4 ô thống kê (đều là COUNT dưới SQL, không kéo dữ liệu) ---
        var totalPaid = await _unitOfWork.Bookings.CountAsync(b => b.PaymentStatus == "Paid");
        var totalUsers = await _unitOfWork.Users.CountAsync();
        var totalMovies = await _unitOfWork.Movies.CountAsync();
        var activeUsers = await _unitOfWork.Users.CountAsync(u => u.Status == "Active");

        // Doanh thu 30 ngày: chỉ lấy đơn ĐÃ THANH TOÁN trong cửa sổ 30 ngày, KHÔNG kèm
        // navigation (chỉ cần FinalAmount + CreatedAt) -> lọc đẩy xuống SQL.
        var revenueWindow = (await _unitOfWork.Bookings.GetAllAsync(
            b => b.PaymentStatus == "Paid" && b.CreatedAt >= windowStart)).ToList();

        var revenue30 = revenueWindow.Sum(b => b.FinalAmount);

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

        // --- 6 giao dịch gần nhất (mọi trạng thái) — TOP 6 đẩy xuống SQL ---
        var (recentRows, _) = await _unitOfWork.Bookings.GetPagedAsync(
            page: 1, pageSize: 6,
            include: q => q
                .Include(b => b.User)
                .Include(b => b.Showtime).ThenInclude(s => s.Movie),
            orderBy: q => q.OrderByDescending(b => b.CreatedAt));

        var recent = recentRows
            .Select(b => new RecentBookingViewModel
            {
                CustomerName = b.User?.FullName ?? "Khách vãng lai",
                MovieTitle = b.Showtime?.Movie?.Title ?? "-",
                CreatedAt = b.CreatedAt,
                FinalAmount = b.FinalAmount,
                PaymentStatus = b.PaymentStatus
            }).ToList();

        // --- Doanh thu theo phim: lấy TẤT CẢ phim (GROUP BY dưới SQL) rồi tách top 5 + phần còn lại ---
        var allMovieStats = await _unitOfWork.Tickets.GetTopMoviesByTicketsAsync(int.MaxValue);
        var hotMovies = allMovieStats.Take(5)
            .Select(m => new HotMovieViewModel
            {
                MovieTitle = m.MovieTitle,
                PosterUrl = m.PosterUrl,
                TicketsSold = m.TicketsSold,
                Revenue = m.Revenue
            })
            .ToList();
        var allMoviesRevenue = allMovieStats.Sum(m => m.Revenue);      // mẫu số cho biểu đồ tròn
        var otherMoviesRevenue = allMovieStats.Skip(5).Sum(m => m.Revenue); // gộp thành lát "Các phim khác"

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
            HotMovies = hotMovies,
            AllMoviesRevenue = allMoviesRevenue,
            OtherMoviesRevenue = otherMoviesRevenue
        };
    }
}