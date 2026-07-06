using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Interfaces;

/// <summary>Repository chuyên biệt cho <see cref="Showtime"/> (bán vé tại quầy).</summary>
public interface IShowtimeRepository : IGenericRepository<Showtime>
{
    /// <summary>Các suất chiếu sắp tới (đã lên lịch) kèm phim — để liệt kê phim bán được.</summary>
    Task<IReadOnlyList<Showtime>> GetUpcomingWithMovieAsync(DateTime fromTime);

    /// <summary>Các suất chiếu sắp tới của một phim, kèm phòng/loại phòng/rạp, sắp theo giờ.</summary>
    Task<IReadOnlyList<Showtime>> GetUpcomingByMovieAsync(Guid movieId, DateTime fromTime);

    /// <summary>Một suất chiếu kèm phòng (+loại phòng) và phim — để dựng sơ đồ ghế / tạo đơn.</summary>
    Task<Showtime?> GetWithRoomAndMovieAsync(Guid showtimeId);
}
