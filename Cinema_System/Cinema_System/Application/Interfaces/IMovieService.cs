using Cinema_System.Application.DTOs;

namespace Cinema_System.Application.Interfaces;

// Dịch vụ phim cung cấp các phép toán truy vấn phim và dữ liệu phụ trợ cho giao diện.
public interface IMovieService
{
    Task<IEnumerable<MovieDTO>> GetAllMoviesAsync();
    Task<IEnumerable<MovieDTO>> GetNowShowingMoviesAsync();
    Task<IEnumerable<MovieDTO>> GetComingSoonMoviesAsync();
    Task<IEnumerable<MovieDTO>> GetSpecialShowtimeMoviesAsync();  
    Task<IEnumerable<MovieDTO>> GetFilteredMoviesAsync(string? genre, string? ageRating, string? status);
    Task<IEnumerable<string>> GetAllGenresAsync();
    Task<IEnumerable<string>> GetAllAgeRatingsAsync();
    Task<IEnumerable<string>> GetAllMovieStatusesAsync();
    Task<MovieDTO?> GetMovieByIdAsync(Guid id);
    // Lấy dữ liệu trang phim theo tab (now/coming) và phân trang.
    Task<Cinema_System.Application.ViewModels.MoviesPageViewModel> GetMoviesPageAsync(string tab, int page, int pageSize);
    // Tìm phim theo từ khóa và trả về kết quả phân trang.
    Task<Cinema_System.Application.ViewModels.MoviesPageViewModel> SearchMoviesAsync(string keyword, int page, int pageSize);
}
