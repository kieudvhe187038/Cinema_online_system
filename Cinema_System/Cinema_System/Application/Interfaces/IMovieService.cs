using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.ViewModels;

namespace Cinema_System.Application.Interfaces;

public interface IMovieService
{
    // Lấy toàn bộ phim.
    Task<IEnumerable<MovieDTO>> GetAllMoviesAsync();
    // Lấy phim đang chiếu.
    Task<IEnumerable<MovieDTO>> GetNowShowingMoviesAsync();
    // Lấy phim sắp chiếu.
    Task<IEnumerable<MovieDTO>> GetComingSoonMoviesAsync();
    // Lấy phim có suất chiếu đặc biệt.
    Task<IEnumerable<MovieDTO>> GetSpecialShowtimeMoviesAsync();
    // Lấy chi tiết 1 phim theo Id.
    Task<MovieDTO?> GetMovieByIdAsync(Guid id);

    // Lấy tất cả thể loại.
    Task<IEnumerable<GenreDTO>> GetAllGenresAsync();
    // Lấy danh sách phim cho trang quản lý (lọc + phân trang).
    Task<MovieListViewModel> GetMoviesForManagerAsync(string? search, string? status, string? genre, int page, int pageSize);
    // Lấy dữ liệu phim để đổ vào form sửa.
    Task<MovieFormViewModel?> GetForEditAsync(Guid id);
    // Tạo phim mới.
    Task<Result> CreateAsync(MovieFormViewModel model);
    // Cập nhật phim.
    Task<Result> UpdateAsync(MovieFormViewModel model);
    // Đổi trạng thái chiếu/ngừng chiếu.
    Task<Result> ToggleStatusAsync(Guid id);
}
