using Cinema_System.Application.Interfaces;
using Cinema_System.Domain.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Cinema_System.Application.Services;

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
                await UpdateShowtimeStatusesAsync(stoppingToken);
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

    private async Task UpdateShowtimeStatusesAsync(CancellationToken stoppingToken)
    {
        // Vì BackgroundService là Singleton, ta cần tạo Scope để resolve các Scoped Services (như IUnitOfWork)
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var now = DateTime.Now;

        // --- 1. CẬP NHẬT TRẠNG THÁI SUẤT CHIẾU ---
        // Lấy tất cả các lịch chiếu có trạng thái chưa phải là Completed hoặc Cancelled
        // Cần cập nhật trạng thái "Scheduled" -> "Live" (đang chiếu) -> "Completed" (đã chiếu xong)
        var showtimesToUpdate = await unitOfWork.Showtimes.GetAllAsync(s => 
            s.Status == "Scheduled" || s.Status == "Live" || string.IsNullOrEmpty(s.Status));

        bool hasChanges = false;

        foreach (var showtime in showtimesToUpdate)
        {
            var oldStatus = string.IsNullOrEmpty(showtime.Status) ? "Scheduled" : showtime.Status;

            if (now >= showtime.EndTime)
            {
                showtime.Status = "Completed";
            }
            else if (now >= showtime.StartTime && now < showtime.EndTime)
            {
                showtime.Status = "Live";
            }
            else if (string.IsNullOrEmpty(showtime.Status))
            {
                showtime.Status = "Scheduled";
            }

            if (showtime.Status != oldStatus || string.IsNullOrEmpty(showtime.Status))
            {
                unitOfWork.Showtimes.Update(showtime);
                hasChanges = true;
                _logger.LogInformation($"Updated Showtime {showtime.Id} status from {oldStatus} to {showtime.Status}");
            }
        }

        if (hasChanges)
        {
            await unitOfWork.SaveChangesAsync();
        }
    }
}
