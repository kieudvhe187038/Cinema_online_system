using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.ViewModels;

namespace Cinema_System.Application.Interfaces;

// Dịch vụ phim: truy vấn cho giao diện công khai + nghiệp vụ quản lý (CRUD).
public interface IMovieService
{
    // ── Public (xem phim) ──
    Task<IEnumerable<MovieDTO>> GetAllMoviesAsync();
    Task<IEnumerable<MovieDTO>> GetNowShowingMoviesAsync();
    Task<IEnumerable<MovieDTO>> GetComingSoonMoviesAsync();
    Task<IEnumerable<MovieDTO>> GetSpecialMoviesAsync();
    Task<IEnumerable<string>> GetAllGenresAsync();
    Task<IEnumerable<string>> GetAllAgeRatingsAsync();
    Task<MovieDTO?> GetMovieByIdAsync(Guid id);
    Task<MovieDTO?> GetMovieBySlugAsync(string slug);
    // Lấy dữ liệu trang phim theo tab (now/coming/special), lọc thể loại/độ tuổi và phân trang.
    Task<PagedResult<MovieDTO>> GetMoviesPageAsync(string tab, int page, int pageSize, string? genre = null, string? ageRating = null);
    // Tìm phim theo từ khóa và trả về kết quả phân trang.
    Task<PagedResult<MovieDTO>> SearchMoviesAsync(string keyword, int page, int pageSize);

    // ── Quản lý (admin CRUD) ──
    // Lấy tất cả thể loại (dạng DTO) cho dropdown form quản lý.
    Task<IEnumerable<GenreDTO>> GetGenreOptionsAsync();
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
