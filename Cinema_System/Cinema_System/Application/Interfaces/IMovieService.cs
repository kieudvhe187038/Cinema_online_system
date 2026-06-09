using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.ViewModels;

namespace Cinema_System.Application.Interfaces;

public interface IMovieService
{
    Task<IEnumerable<MovieDTO>> GetAllMoviesAsync();
    Task<IEnumerable<MovieDTO>> GetNowShowingMoviesAsync();
    Task<IEnumerable<MovieDTO>> GetComingSoonMoviesAsync();
    Task<IEnumerable<MovieDTO>> GetSpecialShowtimeMoviesAsync();
    Task<MovieDTO?> GetMovieByIdAsync(Guid id);

    Task<IEnumerable<GenreDTO>> GetAllGenresAsync();
    Task<MovieListViewModel> GetMoviesForAdminAsync(string? search, string? status, string? genre, int page, int pageSize);
    Task<MovieFormViewModel?> GetForEditAsync(Guid id);
    Task<Result> CreateAsync(MovieFormViewModel model);
    Task<Result> UpdateAsync(MovieFormViewModel model);
    Task<Result> ToggleStatusAsync(Guid id);
}
