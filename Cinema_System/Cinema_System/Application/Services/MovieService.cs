using System.Linq;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Services;

public class MovieService : IMovieService
{
    private const string ShowtimesIncludeProperty = "Showtimes";
    private const string StoppedMovieStatusLower = "stopped";

    private readonly IUnitOfWork _unitOfWork;
    private readonly AutoMapper.IMapper _mapper;

    public MovieService(IUnitOfWork unitOfWork, AutoMapper.IMapper mapper)   
    {
        _unitOfWork = unitOfWork; 
        _mapper = mapper;
    }

    public async Task<Cinema_System.Application.ViewModels.MoviesPageViewModel> GetMoviesPageAsync(string tab, int page, int pageSize)
    {
        IEnumerable<MovieDTO> moviesForTab = tab?.ToLower() switch
        {
            "coming" => (await GetComingSoonMoviesAsync()).ToList(),
            "special" => (await GetSpecialShowtimeMoviesAsync()).ToList(),
            _ => (await GetNowShowingMoviesAsync()).ToList(),
        };

        var totalCount = moviesForTab.Count();
        var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);
        if (page < 1) page = 1;
        if (page > totalPages) page = totalPages;

        var pagedMovies = moviesForTab.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var moviesPageViewModel = new Cinema_System.Application.ViewModels.MoviesPageViewModel
        {
            SelectedTab = tab?.ToLower() ?? "now",
            Movies = pagedMovies,
            CurrentPage = page,
            TotalPages = totalPages,
            PageSize = pageSize
        };

        return moviesPageViewModel;
    }

    public async Task<IEnumerable<MovieDTO>> GetAllMoviesAsync()
    {
        var allMovies = await _unitOfWork.Movies.GetAllAsync(
            includeProperties: new[] { ShowtimesIncludeProperty }
        );

        return _mapper.Map<IEnumerable<MovieDTO>>(allMovies);
    }

    public async Task<IEnumerable<MovieDTO>> GetNowShowingMoviesAsync()
    {
        var nowShowingMovies = await _unitOfWork.Movies.GetAllAsync(
            predicate: movie => movie.Status == "Now Showing",
            includeProperties: new[] { ShowtimesIncludeProperty }
        );

        return _mapper.Map<IEnumerable<MovieDTO>>(nowShowingMovies);
    }

    public async Task<IEnumerable<MovieDTO>> GetComingSoonMoviesAsync()
    {
        var comingSoonMovies = await _unitOfWork.Movies.GetAllAsync(
            predicate: movie => movie.Status == "Coming Soon",
            includeProperties: new[] { ShowtimesIncludeProperty }
        );

        return _mapper.Map<IEnumerable<MovieDTO>>(comingSoonMovies);
    }

    public async Task<IEnumerable<MovieDTO>> GetFilteredMoviesAsync(string? genre, string? ageRating, string? status)
    {
        var allMovies = await _unitOfWork.Movies.GetAllAsync(
            includeProperties: new[] { ShowtimesIncludeProperty, "Genres" }
        );

        var filteredMovies = allMovies.Where(movie =>
            movie.Status != null && movie.Status.ToLower() != StoppedMovieStatusLower &&
            (string.IsNullOrWhiteSpace(genre) || (movie.Genres != null && movie.Genres.Any(genreEntity => genreEntity.Name != null && genreEntity.Name.ToLower() == genre.ToLower()))) &&
            (string.IsNullOrWhiteSpace(ageRating) || (movie.AgeRating != null && movie.AgeRating.ToLower() == ageRating.ToLower())) &&
            (string.IsNullOrWhiteSpace(status) || (movie.Status != null && movie.Status.ToLower() == status.ToLower()))
        );

        return _mapper.Map<IEnumerable<MovieDTO>>(filteredMovies);
    }

    public async Task<IEnumerable<string>> GetAllGenresAsync()
    {
        var genres = await _unitOfWork.Genres.GetAllAsync(
            orderBy: genresQuery => genresQuery.OrderBy(genre => genre.Name)
        );

        return genres
            .Select(genre => genre.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name)
            .ToList();
    }

    private async Task<IEnumerable<Domain.Entities.Movie>> GetVisibleMoviesAsync()
    {
        return (await _unitOfWork.Movies.GetAllAsync())
            .Where(movie => movie.Status != null && movie.Status.ToLower() != StoppedMovieStatusLower);
    }

    public async Task<IEnumerable<string>> GetAllAgeRatingsAsync()
    {
        var visibleMovies = await GetVisibleMoviesAsync();
        var ratings = visibleMovies
            .Select(movie => movie.AgeRating)
            .Where(ageRatingValue => !string.IsNullOrWhiteSpace(ageRatingValue))
            .Select(rating => rating!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var order = new List<string> { "P", "C13", "C16", "C18" };
        return ratings
            .OrderBy(rating =>
            {
                var index = order.FindIndex(x => string.Equals(x, rating, StringComparison.OrdinalIgnoreCase));
                return index >= 0 ? index : order.Count;
            })
            .ThenBy(rating => rating, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<IEnumerable<string>> GetAllMovieStatusesAsync()
    {
        var visibleMovies = await GetVisibleMoviesAsync();
        return visibleMovies
            .Select(movie => movie.Status)
            .Where(statusValue => !string.IsNullOrWhiteSpace(statusValue))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(status => status)
            .ToList()!;
    }

    public async Task<MovieDTO?> GetMovieByIdAsync(Guid id)
    {
        var movie = await _unitOfWork.Movies.FirstOrDefaultAsync(
            predicate: movieEntity => movieEntity.Id == id,
            includeProperties: new[] { ShowtimesIncludeProperty }
        );

        return movie == null ? null : _mapper.Map<MovieDTO>(movie);
    }

    public async Task<IEnumerable<MovieDTO>> GetSpecialShowtimeMoviesAsync()
    {
        var specialShowtimeMovies = await _unitOfWork.Movies.GetAllAsync(
            predicate: movie => movie.Status != null && movie.Status.ToLower() != StoppedMovieStatusLower &&
                movie.Showtimes.Any(showtime =>
                    showtime.Status == "Special" ||
                    showtime.Status == "Special Screening" ||
                    (showtime.Status != null && showtime.Status.Contains("Đặc"))
                ),
            includeProperties: new[] { ShowtimesIncludeProperty }
        );

        return _mapper.Map<IEnumerable<MovieDTO>>(specialShowtimeMovies);
    }

    public async Task<Cinema_System.Application.ViewModels.MoviesPageViewModel> SearchMoviesAsync(string keyword, int page, int pageSize)
    {
        var searchTerm = keyword?.Trim().ToLowerInvariant() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return new Cinema_System.Application.ViewModels.MoviesPageViewModel
            {
                SelectedTab = "search",
                SearchKeyword = string.Empty,
                Movies = new List<MovieDTO>(),
                CurrentPage = page,
                TotalPages = 1,
                PageSize = pageSize
            };
        }

        var searchResults = await _unitOfWork.Movies.GetAllAsync(
            predicate: movie =>
                movie.Status != null && movie.Status.ToLower() != StoppedMovieStatusLower &&
                ((movie.Title != null && movie.Title.ToLower().Contains(searchTerm)) ||
                (movie.Description != null && movie.Description.ToLower().Contains(searchTerm)) ||
                (movie.Director != null && movie.Director.ToLower().Contains(searchTerm)) ||
                (movie.CastMembers != null && movie.CastMembers.ToLower().Contains(searchTerm))),
            includeProperties: new[] { ShowtimesIncludeProperty }
        );

        var searchResultsDto = _mapper.Map<List<MovieDTO>>(searchResults);
        var totalCount = searchResultsDto.Count;
        var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);
        if (page < 1) page = 1;
        if (page > totalPages) page = totalPages;

        var pagedSearchResults = searchResultsDto.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new Cinema_System.Application.ViewModels.MoviesPageViewModel
        {
            SelectedTab = "search",
            SearchKeyword = searchTerm,
            Movies = pagedSearchResults,
            CurrentPage = page,
            TotalPages = totalPages,
            PageSize = pageSize
        };
    }
}
