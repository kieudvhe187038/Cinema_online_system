using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.ViewModels;

namespace Cinema_System.Application.Interfaces;

public interface IShowtimeService
{
    Task<ShowtimeFormViewModel?> GetForEditAsync(Guid id);
    Task<IEnumerable<ItemOptionDTO>> GetMovieOptionsAsync();
    Task<IEnumerable<ItemOptionDTO>> GetRoomOptionsAsync();
    Task<ShowtimeCalendarViewModel> GetCalendarAsync(Guid? roomId, string? status, string? search, DateTime? weekStart);
    Task<Result> CreateAsync(ShowtimeFormViewModel model);
    Task<Result> UpdateAsync(ShowtimeFormViewModel model);
    Task<Result> DeleteAsync(Guid id);
}
