using Cinema_System.Application.Common;
using Cinema_System.Application.Interfaces;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Services;

/// <summary>
/// Nguồn duy nhất quyết định trạng thái phim: suy ra từ ngày khởi chiếu và lịch chiếu,
/// thay cho việc Manager chọn tay trong form.
/// </summary>
public class MovieStatusService : IMovieStatusService
{
    // Suất chiếu đã hủy không tính là "phim đã có lịch".
    private const string CancelledShowtime = "Cancelled";

    private readonly IUnitOfWork _unitOfWork;

    public MovieStatusService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> SyncAsync(Movie movie)
    {
        // Phim ngừng chiếu do Manager bật/tắt tay -> giữ nguyên.
        if (MovieStatusPolicy.IsManagerControlled(movie.Status))
            return false;

        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);
        var hasEarlyShowtime = await HasShowtimeBeforeReleaseAsync(movie, today);
        var status = MovieStatusPolicy.Resolve(movie.ReleaseDate, hasEarlyShowtime, today);

        if (string.Equals(movie.Status, status, StringComparison.OrdinalIgnoreCase))
            return false;

        movie.Status = status;
        movie.UpdatedAt = now;
        _unitOfWork.Movies.Update(movie);
        return true;
    }

    public async Task SyncAndSaveAsync(Guid movieId)
    {
        var movie = await _unitOfWork.Movies.GetByIdAsync(movieId);
        if (movie is null)
            return;

        if (await SyncAsync(movie))
            await _unitOfWork.SaveChangesAsync();
    }

    // Phim chưa tới ngày khởi chiếu mà đã có suất chiếu (chưa hủy) trước ngày đó -> phim chiếu sớm.
    private async Task<bool> HasShowtimeBeforeReleaseAsync(Movie movie, DateOnly today)
    {
        if (movie.ReleaseDate is null || movie.ReleaseDate.Value <= today)
            return false;

        var releaseStart = movie.ReleaseDate.Value.ToDateTime(TimeOnly.MinValue);
        return await _unitOfWork.Showtimes.ExistsAsync(
            s => s.MovieId == movie.Id && s.Status != CancelledShowtime && s.StartTime < releaseStart);
    }
}
