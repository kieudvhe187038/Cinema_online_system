using Cinema_System.Application.ViewModels;

namespace Cinema_System.Application.Interfaces;

// Dịch vụ lịch chiếu công khai (xem suất chiếu theo phim/phòng/ngày).
public interface IShowtimeService
{
    // Lấy dữ liệu trang lịch chiếu: lọc theo phim, phòng và ngày (mặc định hôm nay).
    Task<ShowtimePageViewModel> GetShowtimePageAsync(Guid? movieId, Guid? roomId, DateOnly? date);
}
