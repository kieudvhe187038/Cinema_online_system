using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;
using Cinema_System.Domain.Entities;
using Cinema_System.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Cinema_System.Infrastructure.Repositories;

public class TicketRepository : GenericRepository<Ticket>, ITicketRepository
{
    private const string StatusCancelled = "Cancelled";
    private const string StatusCheckedIn = "Checked-in";

    public TicketRepository(CinemaWebDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<HotMovieStatDto>> GetTopMoviesByTicketsAsync(int top)
    {
        // GROUP BY + đếm/tính tổng chạy hoàn toàn dưới SQL, chỉ trả về `top` dòng.
        return await _dbSet.AsNoTracking()
            .Where(t => t.Showtime != null && t.Showtime.Movie != null)
            .GroupBy(t => new
            {
                t.Showtime.Movie.Title,
                t.Showtime.Movie.PosterUrl
            })
            .Select(g => new HotMovieStatDto
            {
                MovieTitle = g.Key.Title,
                PosterUrl = g.Key.PosterUrl,
                TicketsSold = g.Count(),
                Revenue = g.Sum(t => t.PriceAtBooking)
            })
            .OrderByDescending(m => m.TicketsSold)
            .Take(top)
            .ToListAsync();
    }

    public async Task<bool> TryCheckinAsync(Guid ticketId, Guid staffId, DateTime scannedAt)
    {
        // UPDATE trực tiếp có điều kiện trạng thái trong mệnh đề WHERE — nguyên tử ở
        // mức DB, không phụ thuộc vào trạng thái đã đọc trước đó trong request này.
        var rows = await _dbSet
            .Where(t => t.Id == ticketId
                && t.Status != StatusCancelled
                && t.Status != StatusCheckedIn)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(t => t.Status, StatusCheckedIn)
                .SetProperty(t => t.ScanBy, staffId)
                .SetProperty(t => t.ScannedAt, scannedAt));

        return rows > 0;
    }
}
