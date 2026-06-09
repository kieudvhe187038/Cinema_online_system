using Cinema_System.Application.DTOs;

namespace Cinema_System.Application.Interfaces;

public interface IMovieService
{
    Task<IEnumerable<MovieDTO>> GetAllMoviesAsync();
    Task<IEnumerable<MovieDTO>> GetNowShowingMoviesAsync();
    Task<IEnumerable<MovieDTO>> GetComingSoonMoviesAsync();
    Task<IEnumerable<MovieDTO>> GetSpecialShowtimeMoviesAsync();
    Task<MovieDTO?> GetMovieByIdAsync(Guid id);
    Task<Cinema_System.Application.ViewModels.MoviesPageViewModel> GetMoviesPageAsync(string tab, int page, int pageSize);
    Task<Cinema_System.Application.ViewModels.MoviesPageViewModel> SearchMoviesAsync(string keyword, int page, int pageSize);
}
