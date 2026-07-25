using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Cinema_System.Application.Services;

// Báo cáo & thống kê kinh doanh cho quản lý (doanh thu, vé bán, phương thức thanh toán).
public class ReportService : IReportService
{
    private readonly IUnitOfWork _unitOfWork;

    public ReportService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ReportViewModel> GetReportAsync(DateOnly? from, DateOnly? to)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var dateFrom = from ?? today.AddDays(-29);   // mặc định 30 ngày gần nhất
        var dateTo = to ?? today;
        if (dateTo < dateFrom) dateTo = dateFrom;     // chống nhập ngược

        var fromDt = dateFrom.ToDateTime(TimeOnly.MinValue);
        var toEndDt = dateTo.AddDays(1).ToDateTime(TimeOnly.MinValue);   // hết ngày dateTo

        // Đơn đã thanh toán trong khoảng, kèm phim + vé (split query tránh nhân dòng).
        var bookings = (await _unitOfWork.Bookings.GetAllAsync(
            predicate: b => b.PaymentStatus == "Paid" && b.CreatedAt >= fromDt && b.CreatedAt < toEndDt,
            include: q => q
                .Include(b => b.Showtime).ThenInclude(s => s.Movie)
                .Include(b => b.Tickets)
                .AsSplitQuery())).ToList();

        // ── KPI ──
        var totalRevenue = bookings.Sum(b => b.FinalAmount);
        var totalBookings = bookings.Count;
        var totalTickets = bookings.Sum(b => b.Tickets.Count(t => t.Status != "Cancelled"));
        var avgOrder = totalBookings > 0 ? Math.Round(totalRevenue / totalBookings, 0) : 0m;

        // ── Doanh thu theo ngày (điền đủ mọi ngày trong khoảng, kể cả ngày = 0) ──
        var revByDay = bookings
            .Where(b => b.CreatedAt.HasValue)
            .GroupBy(b => DateOnly.FromDateTime(b.CreatedAt!.Value))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.FinalAmount));
        var daily = new List<DailyRevenuePoint>();
        for (var d = dateFrom; d <= dateTo; d = d.AddDays(1))
            daily.Add(new DailyRevenuePoint { Date = d, Revenue = revByDay.TryGetValue(d, out var r) ? r : 0m });

        // ── Top phim theo doanh thu vé (dựa trên giá vé lúc đặt, bỏ vé đã hủy) ──
        var topMovies = bookings
            .SelectMany(b => b.Tickets
                .Where(t => t.Status != "Cancelled")
                .Select(t => new { Title = b.Showtime.Movie.Title, t.PriceAtBooking }))
            .GroupBy(x => x.Title)
            .Select(g => new TopMovieItem
            {
                Title = g.Key,
                TicketsSold = g.Count(),
                Revenue = g.Sum(x => x.PriceAtBooking)
            })
            .OrderByDescending(x => x.Revenue)
            .Take(5)
            .ToList();

        // ── Tỉ lệ phương thức thanh toán (từ các payment thành công trong khoảng) ──
        var payments = await _unitOfWork.Payments.GetAllAsync(
            predicate: p => p.Status == "Success" && p.PaidAt >= fromDt && p.PaidAt < toEndDt);
        var payMethods = payments
            .GroupBy(p => p.PaymentMethod)
            .Select(g => new PaymentMethodItem
            {
                Method = g.Key,
                Count = g.Count(),
                Amount = g.Sum(x => x.Amount)
            })
            .OrderByDescending(x => x.Amount)
            .ToList();

        return new ReportViewModel
        {
            DateFrom = dateFrom,
            DateTo = dateTo,
            TotalRevenue = totalRevenue,
            TotalBookings = totalBookings,
            TotalTickets = totalTickets,
            AvgOrderValue = avgOrder,
            DailyRevenue = daily,
            TopMovies = topMovies,
            PaymentMethods = payMethods
        };
    }
}
