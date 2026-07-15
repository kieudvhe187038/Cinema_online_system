using Cinema_System.Application.Common;
using Cinema_System.Application.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Cinema_System.Application.Services;

/// <summary>
/// Tự động đồng bộ trạng thái Suất chiếu (Scheduled -> Live -> Completed) và
/// Phim (Coming Soon -> Now Showing) theo thời gian thực. Không tự động chuyển
/// Now Showing -> Stopped — việc dừng chiếu do Manager chủ động thao tác
/// (Edit/ToggleStatus trong MovieManagement).
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
    /// Cập nhật trạng thái Phim: chỉ "Coming Soon" -> "Now Showing" khi đến/qua ngày khởi chiếu.
    /// KHÔNG tự động chuyển "Now Showing" -> "Stopped" dù phim hết suất chiếu đang hoạt động —
    /// việc dừng chiếu là quyết định nghiệp vụ của Manager (Edit/ToggleStatus), không suy ra
    /// tự động từ dữ liệu suất chiếu để tránh dừng nhầm phim đang tạm hết suất chờ xếp lịch tiếp.
    /// </summary>
    private async Task UpdateMovieStatusesAsync(IUnitOfWork unitOfWork, DateTime now)
    {
        var today = DateOnly.FromDateTime(now);

        var moviesToLaunch = await unitOfWork.Movies.GetAllAsync(
            m => m.Status == MovieStatus.ComingSoon && m.ReleaseDate != null && m.ReleaseDate <= today);

        var launchList = moviesToLaunch.ToList();
        if (launchList.Count == 0)
            return;

        foreach (var movie in launchList)
        {
            movie.Status = MovieStatus.NowShowing;
            movie.UpdatedAt = now;
            unitOfWork.Movies.Update(movie);
            _logger.LogInformation("Updated Movie {MovieId} status from Coming Soon to Now Showing (release date reached)", movie.Id);
        }

        await unitOfWork.SaveChangesAsync();
    }
}
