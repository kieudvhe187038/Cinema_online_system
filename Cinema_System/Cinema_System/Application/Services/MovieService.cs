using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Services;

// Dịch vụ phim xử lý truy vấn dữ liệu phim và đóng gói thành DTO cho giao diện.
public class MovieService : IMovieService
{
    private const string ShowtimesIncludeProperty = "Showtimes"; // Include lịch chiếu khi truy vấn
    private const string GenresIncludeProperty = "Genres";       // Include thể loại khi truy vấn

    private readonly IUnitOfWork _unitOfWork;
    private readonly AutoMapper.IMapper _mapper;

    public MovieService(IUnitOfWork unitOfWork, AutoMapper.IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    // Lấy danh sách phim cho trang theo tab hiện tại, lọc theo thể loại/độ tuổi rồi phân trang.
    public async Task<PagedResult<MovieDTO>> GetMoviesPageAsync(string tab, int page, int pageSize, string? genre = null, string? ageRating = null)
    {
        var tabMovies = await GetTabMoviesAsync(tab?.ToLower());

        var genreLower = genre?.Trim().ToLower();
        var ageRatingLower = ageRating?.Trim().ToLower();

        // Lọc thể loại/độ tuổi ngay trong tập của tab (tập nhỏ nên lọc trong bộ nhớ);
        // thể loại cần navigation Genres nên không thể đẩy qua DTO.
        var filtered = tabMovies.Where(movie =>
            (string.IsNullOrWhiteSpace(genreLower) ||
                (movie.Genres != null && movie.Genres.Any(genreEntity => genreEntity.Name != null && genreEntity.Name.ToLower() == genreLower))) &&
            (string.IsNullOrWhiteSpace(ageRatingLower) ||
                (movie.AgeRating != null && movie.AgeRating.ToLower() == ageRatingLower)));

        var dtos = _mapper.Map<List<MovieDTO>>(filtered);
        return PagedResult<MovieDTO>.Create(dtos, page, pageSize);
    }

    // Lấy entity phim theo tab (kèm Showtimes + Genres) để phục vụ lọc trong tab.
    private async Task<IEnumerable<Movie>> GetTabMoviesAsync(string? tabKey)
    {
        var includes = new[] { ShowtimesIncludeProperty, GenresIncludeProperty };

        return tabKey switch
        {
            "coming" => await _unitOfWork.Movies.GetAllAsync(
                predicate: movie => movie.Status == MovieStatus.ComingSoon,
                includeProperties: includes),
            "special" => await _unitOfWork.Movies.GetAllAsync(
                predicate: movie => movie.Status != null && movie.Status.ToLower() != MovieStatus.StoppedLower &&
                    movie.Showtimes.Any(showtime =>
                        showtime.Status == ShowtimeStatus.Special ||
                        showtime.Status == ShowtimeStatus.SpecialScreening ||
                        (showtime.Status != null && showtime.Status.Contains(ShowtimeStatus.SpecialKeyword))),
                includeProperties: includes),
            _ => await _unitOfWork.Movies.GetAllAsync(
                predicate: movie => movie.Status == MovieStatus.NowShowing,
                includeProperties: includes),
        };
    }

    // Lấy danh sách tất cả phim, dùng khi cần dữ liệu không phân trang.
    public async Task<IEnumerable<MovieDTO>> GetAllMoviesAsync()
    {
        var allMovies = await _unitOfWork.Movies.GetAllAsync(
            includeProperties: new[] { ShowtimesIncludeProperty }
        );

        return _mapper.Map<IEnumerable<MovieDTO>>(allMovies);
    }

    // Lấy phim đang chiếu hiện tại.
    public async Task<IEnumerable<MovieDTO>> GetNowShowingMoviesAsync()
    {
        var nowShowingMovies = await _unitOfWork.Movies.GetAllAsync(
            predicate: movie => movie.Status == MovieStatus.NowShowing,
            includeProperties: new[] { ShowtimesIncludeProperty }
        );

        return _mapper.Map<IEnumerable<MovieDTO>>(nowShowingMovies);
    }

    // Lấy phim sắp chiếu
    public async Task<IEnumerable<MovieDTO>> GetComingSoonMoviesAsync()
    {
        var comingSoonMovies = await _unitOfWork.Movies.GetAllAsync(
            predicate: movie => movie.Status == MovieStatus.ComingSoon,
            includeProperties: new[] { ShowtimesIncludeProperty }
        );

        return _mapper.Map<IEnumerable<MovieDTO>>(comingSoonMovies);
    }

    // Lấy tất cả thể loại phim đã được cấu hình trong hệ thống.
    public async Task<IEnumerable<string>> GetAllGenresAsync()
    {
        var genres = await _unitOfWork.Genres.GetAllAsync();

        return genres
            .Select(genre => genre.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name)
            .ToList();
    }

    // Lấy phim còn hiển thị (chưa stopped) để dùng cho các danh sách phụ — lọc ngay ở SQL.
    private async Task<IEnumerable<Movie>> GetVisibleMoviesAsync()
    {
        return await _unitOfWork.Movies.GetAllAsync(
            predicate: movie => movie.Status != null && movie.Status.ToLower() != MovieStatus.StoppedLower
        );
    }

    // Lấy danh sách các độ tuổi (P, C13, C16, C18) tồn tại trong phim đang hiển thị.
    public async Task<IEnumerable<string>> GetAllAgeRatingsAsync()
    {
        var visibleMovies = await GetVisibleMoviesAsync();
        var ratings = visibleMovies
            .Select(movie => movie.AgeRating)
            .Where(ageRatingValue => !string.IsNullOrWhiteSpace(ageRatingValue))
            .Select(rating => rating!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var order = AgeRatingPolicy.DisplayOrder;
        return ratings
            .OrderBy(rating =>
            {
                var index = Array.FindIndex(order, x => string.Equals(x, rating, StringComparison.OrdinalIgnoreCase));
                return index >= 0 ? index : order.Length;
            })
            .ThenBy(rating => rating, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    // Lấy chi tiết phim theo Id.
    public async Task<MovieDTO?> GetMovieByIdAsync(Guid id)
    {
        var movie = await _unitOfWork.Movies.FirstOrDefaultAsync(
            predicate: movieEntity => movieEntity.Id == id,
            includeProperties: new[] { ShowtimesIncludeProperty }
        );

        return movie == null ? null : _mapper.Map<MovieDTO>(movie);
    }

    // Lấy phim có suất chiếu đặc biệt hoặc đặc sắc.
    public async Task<IEnumerable<MovieDTO>> GetSpecialShowtimeMoviesAsync()
    {
        var specialShowtimeMovies = await _unitOfWork.Movies.GetAllAsync(
            predicate: movie => movie.Status != null && movie.Status.ToLower() != MovieStatus.StoppedLower &&
                movie.Showtimes.Any(showtime =>
                    showtime.Status == ShowtimeStatus.Special ||
                    showtime.Status == ShowtimeStatus.SpecialScreening ||
                    (showtime.Status != null && showtime.Status.Contains(ShowtimeStatus.SpecialKeyword))
                ),
            includeProperties: new[] { ShowtimesIncludeProperty }
        );

        return _mapper.Map<IEnumerable<MovieDTO>>(specialShowtimeMovies);
    }

    // Tìm phim theo từ khóa (đẩy điều kiện xuống SQL) và trả về kết quả phân trang.
    public async Task<PagedResult<MovieDTO>> SearchMoviesAsync(string keyword, int page, int pageSize)
    {
        var searchTerm = keyword?.Trim().ToLowerInvariant() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return PagedResult<MovieDTO>.Create(Array.Empty<MovieDTO>(), page, pageSize);
        }

        var searchResults = await _unitOfWork.Movies.GetAllAsync(
            predicate: movie =>
                movie.Status != null && movie.Status.ToLower() != MovieStatus.StoppedLower &&
                ((movie.Title != null && movie.Title.ToLower().Contains(searchTerm)) ||
                (movie.Description != null && movie.Description.ToLower().Contains(searchTerm)) ||
                (movie.Director != null && movie.Director.ToLower().Contains(searchTerm)) ||
                (movie.CastMembers != null && movie.CastMembers.ToLower().Contains(searchTerm))),
            includeProperties: new[] { ShowtimesIncludeProperty }
        );

        var searchResultsDto = _mapper.Map<List<MovieDTO>>(searchResults);
        return PagedResult<MovieDTO>.Create(searchResultsDto, page, pageSize);
    }
}
