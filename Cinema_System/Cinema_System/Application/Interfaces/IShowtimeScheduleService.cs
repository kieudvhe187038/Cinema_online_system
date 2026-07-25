using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.ViewModels;

namespace Cinema_System.Application.Interfaces;

/// <summary>
/// Dịch vụ quản lý (CRUD) lịch chiếu cho Manager — tạo/sửa/hủy/xóa suất chiếu + xem lịch tuần.
/// Khác với <see cref="IShowtimeService"/> (chỉ phục vụ xem lịch chiếu công khai + luồng đặt vé).
/// </summary>
public interface IShowtimeScheduleService
{
    Task<ShowtimeFormViewModel?> GetForEditAsync(Guid id);
    Task<IEnumerable<ItemOptionDTO>> GetMovieOptionsAsync();
    Task<IEnumerable<ItemOptionDTO>> GetRoomOptionsAsync();
    Task<ShowtimeCalendarViewModel> GetCalendarAsync(Guid? roomId, string? status, string? search, DateTime? weekStart);
    Task<Result> CreateAsync(ShowtimeFormViewModel model);
    Task<Result> UpdateAsync(ShowtimeFormViewModel model);
    Task<bool> HasPaidBookingsAsync(Guid id);
    Task<Result> CancelAsync(Guid id);
    Task<Result> DeleteAsync(Guid id);
}
