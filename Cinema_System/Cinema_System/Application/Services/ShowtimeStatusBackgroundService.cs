using Cinema_System.Application.Common;
using Cinema_System.Application.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Cinema_System.Application.Services;

/// <summary>
/// Tự động đồng bộ trạng thái Suất chiếu (Scheduled -> Live -> Completed) và
/// </summary>
public class ShowtimeStatusBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ShowtimeStatusBackgroundService> _logger;

    public ShowtimeStatusBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ShowtimeStatusBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ShowtimeStatusBackgroundService is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Vì BackgroundService là Singleton, ta cần tạo Scope để resolve các Scoped Services (như IUnitOfWork)
                using var scope = _serviceProvider.CreateScope();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var now = DateTime.Now;

                await UpdateShowtimeStatusesAsync(unitOfWork, now);
                await UpdateMovieStatusesAsync(unitOfWork, now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing ShowtimeStatusBackgroundService.");
            }

            // Chờ 1 phút rồi chạy lại
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }

        _logger.LogInformation("ShowtimeStatusBackgroundService is stopping.");
    }

    /// <summary>
    /// Cập nhật "Scheduled" -> "Live" -> "Completed". Chỉ truy vấn các suất chiếu ĐÃ ĐẾN HẠN
    /// chuyển trạng thái (đã tới giờ bắt đầu/kết thúc) thay vì tải toàn bộ suất chiếu chưa hoàn tất,
    /// để tránh quét lại hàng loạt suất chiếu Scheduled còn ở tương lai xa mỗi phút.
    /// </summary>
    private async Task UpdateShowtimeStatusesAsync(IUnitOfWork unitOfWork, DateTime now)
    {
        var dueShowtimes = await unitOfWork.Showtimes.GetAllAsync(s =>
            ((s.Status == "Scheduled" || string.IsNullOrEmpty(s.Status)) && s.StartTime <= now) ||
            (s.Status == "Live" && s.EndTime <= now));

        var showtimeList = dueShowtimes.ToList();
        if (showtimeList.Count == 0)
            return;

        foreach (var showtime in showtimeList)
        {
            var oldStatus = string.IsNullOrEmpty(showtime.Status) ? "Scheduled" : showtime.Status;
            showtime.Status = now >= showtime.EndTime ? "Completed" : "Live";
            unitOfWork.Showtimes.Update(showtime);
            _logger.LogInformation("Updated Showtime {ShowtimeId} status from {OldStatus} to {NewStatus}", showtime.Id, oldStatus, showtime.Status);
        }

        await unitOfWork.SaveChangesAsync();
    }

    /// <summary>
    /// Cập nhật trạng thái Phim: "Coming Soon" -> "Now Showing" khi đến/qua ngày khởi chiếu,
    /// đang hoạt động (Scheduled/Live). Số phim đang "Coming Soon"/"Now Showing" luôn nhỏ
    /// (bị chặn bởi kích thước danh mục phim, không phình to như bảng Showtimes) nên quét toàn bộ
    /// </summary>
    private async Task UpdateMovieStatusesAsync(IUnitOfWork unitOfWork, DateTime now)
    {
        var today = DateOnly.FromDateTime(now);
        bool hasChanges = false;

        var moviesToLaunch = await unitOfWork.Movies.GetAllAsync(
            m => m.Status == MovieStatus.ComingSoon && m.ReleaseDate != null && m.ReleaseDate <= today);

        foreach (var movie in moviesToLaunch)
        {
            movie.Status = MovieStatus.NowShowing;
            movie.UpdatedAt = now;
            unitOfWork.Movies.Update(movie);
            hasChanges = true;
            _logger.LogInformation("Updated Movie {MovieId} status from Coming Soon to Now Showing (release date reached)", movie.Id);
        }
        if (hasChanges)
        {
            await unitOfWork.SaveChangesAsync();
        }
    }
}
