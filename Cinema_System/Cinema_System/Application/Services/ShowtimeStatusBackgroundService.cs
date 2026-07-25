using Cinema_System.Application.Common;
using Cinema_System.Application.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Cinema_System.Application.Services;

/// <summary>
/// Tự động đồng bộ trạng thái Suất chiếu (Scheduled -> Live -> Completed) và
/// Phim (Sắp chiếu / Chiếu sớm -> Đang chiếu khi tới ngày khởi chiếu) theo thời gian thực.
/// Không tự động chuyển sang Stopped — việc dừng chiếu do Manager chủ động thao tác
/// (ToggleStatus trong MovieManagement).
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
                var movieStatusService = scope.ServiceProvider.GetRequiredService<IMovieStatusService>();
                var now = DateTime.Now;

                await UpdateShowtimeStatusesAsync(unitOfWork, now);
                await UpdateMovieStatusesAsync(unitOfWork, movieStatusService);
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
    /// Cập nhật trạng thái Phim CHƯA tới ngày khởi chiếu (Sắp chiếu / Chiếu sớm): tới ngày khởi chiếu
    /// thì thành "Đang chiếu", có suất chiếu trước ngày khởi chiếu thì thành "Chiếu sớm".
    /// KHÔNG tự động chuyển "Now Showing" -> "Stopped" dù phim hết suất chiếu đang hoạt động —
    /// việc dừng chiếu là quyết định nghiệp vụ của Manager (ToggleStatus), không suy ra
    /// tự động từ dữ liệu suất chiếu để tránh dừng nhầm phim đang tạm hết suất chờ xếp lịch tiếp.
    /// </summary>
    private async Task UpdateMovieStatusesAsync(IUnitOfWork unitOfWork, IMovieStatusService movieStatusService)
    {
        var pendingMovies = await unitOfWork.Movies.GetAllAsync(
            m => m.Status == MovieStatus.ComingSoon || m.Status == MovieStatus.Special);

        var pendingList = pendingMovies.ToList();
        if (pendingList.Count == 0)
            return;

        var changed = false;
        foreach (var movie in pendingList)
        {
            var oldStatus = movie.Status;
            if (!await movieStatusService.SyncAsync(movie))
                continue;

            changed = true;
            _logger.LogInformation("Updated Movie {MovieId} status from {OldStatus} to {NewStatus}", movie.Id, oldStatus, movie.Status);
        }

        if (changed)
            await unitOfWork.SaveChangesAsync();
    }
}
