using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;

namespace Cinema_System.Application.Interfaces;

// Dịch vụ phim cung cấp các phép toán truy vấn phim và dữ liệu phụ trợ cho giao diện.
public interface IMovieService
{
    Task<IEnumerable<MovieDTO>> GetAllMoviesAsync();
    Task<IEnumerable<MovieDTO>> GetNowShowingMoviesAsync();
    Task<IEnumerable<MovieDTO>> GetComingSoonMoviesAsync();
    Task<IEnumerable<MovieDTO>> GetSpecialShowtimeMoviesAsync();
    Task<IEnumerable<string>> GetAllGenresAsync();
    Task<IEnumerable<string>> GetAllAgeRatingsAsync();
    Task<MovieDTO?> GetMovieByIdAsync(Guid id);
    // Lấy dữ liệu trang phim theo tab (now/coming/special), lọc thể loại/độ tuổi và phân trang.
    Task<PagedResult<MovieDTO>> GetMoviesPageAsync(string tab, int page, int pageSize, string? genre = null, string? ageRating = null);
    // Tìm phim theo từ khóa và trả về kết quả phân trang.
    Task<PagedResult<MovieDTO>> SearchMoviesAsync(string keyword, int page, int pageSize);
}
