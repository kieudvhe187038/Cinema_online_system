using Cinema_System.Application.Interfaces;
using Cinema_System.Domain.Entities;
using Cinema_System.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Cinema_System.Infrastructure.Repositories;

public class BookingRepository : GenericRepository<Booking>, IBookingRepository
{
    public BookingRepository(CinemaWebDbContext context) : base(context)
    {
    }

    public Task<(IReadOnlyList<Booking> Items, int TotalCount)> GetPagedListAsync(
        string? bookingType, string? paymentStatus, string? keyword, int page, int pageSize)
    {
        var typeFilter = string.IsNullOrEmpty(bookingType) ? null : bookingType;
        var statusFilter = string.IsNullOrEmpty(paymentStatus) ? null : paymentStatus;
        var key = string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim();

        // SQL Server dùng collation không phân biệt hoa thường nên Contains dịch
        // được sang LIKE. Lọc + phân trang đều thực hiện tại DB.
        return GetPagedAsync(
            page, pageSize,
            predicate: b =>
                (typeFilter == null || b.BookingType == typeFilter)
                && (statusFilter == null || b.PaymentStatus == statusFilter)
                && (key == null
                    || (b.QrCode != null && b.QrCode.Contains(key))
                    || (b.User != null && b.User.FullName.Contains(key))
                    || (b.User != null && b.User.Phone != null && b.User.Phone.Contains(key))
                    || (b.Staff != null && b.Staff.FullName.Contains(key))),
            include: q => q
                .Include(b => b.Showtime).ThenInclude(s => s.Movie)
                .Include(b => b.Showtime).ThenInclude(s => s.Room)
                .Include(b => b.User)
                .Include(b => b.Staff)
                .Include(b => b.Tickets)
                .AsSplitQuery(),
            orderBy: q => q.OrderByDescending(b => b.CreatedAt));
    }

    public async Task<Booking?> GetDetailByIdAsync(Guid id)
    {
        return await FirstOrDefaultAsync(
            b => b.Id == id,
            include: q => q
                .Include(b => b.Showtime).ThenInclude(s => s.Movie)
                .Include(b => b.Showtime).ThenInclude(s => s.Room).ThenInclude(r => r.Cinema)
                .Include(b => b.User)
                .Include(b => b.Staff)
                .Include(b => b.Tickets).ThenInclude(t => t.Seat).ThenInclude(s => s.SeatType)
                .Include(b => b.BookingFoods).ThenInclude(f => f.Fb)
                .Include(b => b.Payments)
                .AsSplitQuery());
    }
}
