using Cinema_System.Application.Common;
using Cinema_System.Application.Interfaces;
using Cinema_System.Domain.Entities;
using Cinema_System.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Cinema_System.Infrastructure.Repositories;

public class ShowtimeRepository : GenericRepository<Showtime>, IShowtimeRepository
{
    public ShowtimeRepository(CinemaWebDbContext context) : base(context)
    {
    }

    // Chỉ suất CHƯA tới giờ chiếu và còn trạng thái Scheduled — suất ở quá khứ, đang chiếu (Live),
    // đã chiếu xong (Completed) hay đã hủy (Cancelled) đều không được bán.
    public async Task<IReadOnlyList<Showtime>> GetUpcomingWithMovieAsync(DateTime fromTime)
    {
        var items = await GetAllAsync(
            s => s.StartTime > fromTime && s.Status == ShowtimeStatus.Scheduled,
            include: q => q.Include(s => s.Movie));

        return items.ToList();
    }

    public async Task<IReadOnlyList<Showtime>> GetUpcomingByMovieAsync(Guid movieId, DateTime fromTime)
    {
        var items = await GetAllAsync(
            s => s.MovieId == movieId && s.StartTime > fromTime && s.Status == ShowtimeStatus.Scheduled,
            include: q => q
                .Include(s => s.Room).ThenInclude(r => r.RoomType)
                .Include(s => s.Room).ThenInclude(r => r.Cinema),
            orderBy: q => q.OrderBy(s => s.StartTime));

        return items.ToList();
    }

    public async Task<Showtime?> GetWithRoomAndMovieAsync(Guid showtimeId)
    {
        return await FirstOrDefaultAsync(
            s => s.Id == showtimeId,
            include: q => q
                .Include(s => s.Room).ThenInclude(r => r.RoomType)
                .Include(s => s.Movie));
    }
}
