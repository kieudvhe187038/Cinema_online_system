using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using AutoMapper;
using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Cinema_System.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cinema_System.Application.Services;

// Dịch vụ phim: truy vấn dữ liệu cho giao diện công khai + nghiệp vụ quản lý (CRUD).
public class MovieService : IMovieService
{
    private const string ShowtimesIncludeProperty = "Showtimes"; // Include lịch chiếu khi truy vấn
    private const string GenresIncludeProperty = "Genres";       // Include thể loại khi truy vấn

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public MovieService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    // ─────────────────────────────────────────────────────────────
    // Public (xem phim)
    // ─────────────────────────────────────────────────────────────

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
            includeProperties: new[] { ShowtimesIncludeProperty, GenresIncludeProperty }
        );

        return _mapper.Map<IEnumerable<MovieDTO>>(allMovies);
    }

    // Lấy phim đang chiếu hiện tại.
    public async Task<IEnumerable<MovieDTO>> GetNowShowingMoviesAsync()
    {
        var nowShowingMovies = await _unitOfWork.Movies.GetAllAsync(
            predicate: movie => movie.Status == MovieStatus.NowShowing,
            includeProperties: new[] { ShowtimesIncludeProperty, GenresIncludeProperty }
        );

        return _mapper.Map<IEnumerable<MovieDTO>>(nowShowingMovies);
    }

    // Lấy phim sắp chiếu
    public async Task<IEnumerable<MovieDTO>> GetComingSoonMoviesAsync()
    {
        var comingSoonMovies = await _unitOfWork.Movies.GetAllAsync(
            predicate: movie => movie.Status == MovieStatus.ComingSoon,
            includeProperties: new[] { ShowtimesIncludeProperty, GenresIncludeProperty }
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
            includeProperties: new[] { ShowtimesIncludeProperty, GenresIncludeProperty }
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
            includeProperties: new[] { ShowtimesIncludeProperty, GenresIncludeProperty }
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
            includeProperties: new[] { ShowtimesIncludeProperty, GenresIncludeProperty }
        );

        var searchResultsDto = _mapper.Map<List<MovieDTO>>(searchResults);
        return PagedResult<MovieDTO>.Create(searchResultsDto, page, pageSize);
    }

    // ─────────────────────────────────────────────────────────────
    // Quản lý (admin CRUD)
    // ─────────────────────────────────────────────────────────────

    // Lấy tất cả thể loại (dạng DTO) để đổ vào dropdown form quản lý, sắp xếp theo tên.
    public async Task<IEnumerable<GenreDTO>> GetGenreOptionsAsync()
    {
        var genres = await _unitOfWork.Genres.GetAllAsync(
            orderBy: q => q.OrderBy(g => g.Name));
        return _mapper.Map<IEnumerable<GenreDTO>>(genres);
    }

    // Lấy danh sách phim cho trang quản lý: lọc theo tên/trạng thái/thể loại + phân trang.
    public async Task<MovieListViewModel> GetMoviesForManagerAsync(
        string? search, string? status, string? genre, int page, int pageSize)
    {
        var movies = await _unitOfWork.Movies.GetAllAsync(
            predicate: m =>
                (string.IsNullOrEmpty(search) || m.Title.Contains(search)) &&
                (string.IsNullOrEmpty(status) || m.Status == status) &&
                (string.IsNullOrEmpty(genre) || m.Genres.Any(g => g.Name == genre)),
            include: q => q.Include(m => m.Genres),
            orderBy: q => q.OrderByDescending(m => m.CreatedAt));

        var list = movies.ToList();
        var total = list.Count;
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
        page = Math.Clamp(page, 1, totalPages);

        var paged = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var allGenres = await GetGenreOptionsAsync();

        return new MovieListViewModel
        {
            Movies = _mapper.Map<List<MovieDTO>>(paged),
            CurrentPage = page,
            TotalPages = totalPages,
            PageSize = pageSize,
            TotalCount = total,
            Search = search,
            StatusFilter = status,
            GenreFilter = genre,
            AvailableGenres = allGenres.ToList()
        };
    }

    // Lấy dữ liệu 1 phim để đổ vào form sửa.
    public async Task<MovieFormViewModel?> GetForEditAsync(Guid id)
    {
        var movie = await _unitOfWork.Movies.FirstOrDefaultAsync(
            m => m.Id == id,
            include: q => q.Include(m => m.Genres));

        if (movie is null) return null;

        var vm = _mapper.Map<MovieFormViewModel>(movie);
        vm.AvailableGenres = (await GetGenreOptionsAsync()).ToList();
        return vm;
    }

    // Tạo phim mới (tự sinh slug nếu trống, gán thể loại đã chọn).
    public async Task<Result> CreateAsync(MovieFormViewModel model)
    {
        var slug = string.IsNullOrWhiteSpace(model.Slug)
            ? GenerateSlug(model.Title)
            : model.Slug.Trim().ToLower();

        if (await _unitOfWork.Movies.ExistsAsync(m => m.Slug == slug))
            slug = $"{slug}-{DateTime.UtcNow.Ticks}";

        var movie = new Movie
        {
            Id = Guid.NewGuid(),
            Title = model.Title.Trim(),
            Slug = slug,
            Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
            TrailerUrl = string.IsNullOrWhiteSpace(model.TrailerUrl) ? null : model.TrailerUrl.Trim(),
            PosterUrl = string.IsNullOrWhiteSpace(model.PosterUrl) ? null : model.PosterUrl.Trim(),
            BannerUrl = string.IsNullOrWhiteSpace(model.BannerUrl) ? null : model.BannerUrl.Trim(),
            Director = string.IsNullOrWhiteSpace(model.Director) ? null : model.Director.Trim(),
            CastMembers = string.IsNullOrWhiteSpace(model.CastMembers) ? null : model.CastMembers.Trim(),
            Language = string.IsNullOrWhiteSpace(model.Language) ? null : model.Language.Trim(),
            Subtitle = string.IsNullOrWhiteSpace(model.Subtitle) ? null : model.Subtitle.Trim(),
            DurationMinutes = model.DurationMinutes,
            ReleaseDate = model.ReleaseDate,
            AgeRating = model.AgeRating,
            Status = model.Status,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        foreach (var genreId in model.SelectedGenreIds)
        {
            var genre = await _unitOfWork.Genres.GetByIdAsync(genreId);
            if (genre != null) movie.Genres.Add(genre);
        }

        await _unitOfWork.Movies.AddAsync(movie);
        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    // Cập nhật thông tin phim và danh sách thể loại.
    public async Task<Result> UpdateAsync(MovieFormViewModel model)
    {
        var movie = await _unitOfWork.Movies.FirstOrDefaultAsync(
            m => m.Id == model.Id,
            include: q => q.Include(m => m.Genres));

        if (movie is null)
            return Result.Failure("Không tìm thấy phim.");

        movie.Title = model.Title.Trim();
        movie.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        movie.TrailerUrl = string.IsNullOrWhiteSpace(model.TrailerUrl) ? null : model.TrailerUrl.Trim();
        movie.PosterUrl = string.IsNullOrWhiteSpace(model.PosterUrl) ? null : model.PosterUrl.Trim();
        movie.BannerUrl = string.IsNullOrWhiteSpace(model.BannerUrl) ? null : model.BannerUrl.Trim();
        movie.Director = string.IsNullOrWhiteSpace(model.Director) ? null : model.Director.Trim();
        movie.CastMembers = string.IsNullOrWhiteSpace(model.CastMembers) ? null : model.CastMembers.Trim();
        movie.Language = string.IsNullOrWhiteSpace(model.Language) ? null : model.Language.Trim();
        movie.Subtitle = string.IsNullOrWhiteSpace(model.Subtitle) ? null : model.Subtitle.Trim();
        movie.DurationMinutes = model.DurationMinutes;
        movie.ReleaseDate = model.ReleaseDate;
        movie.AgeRating = model.AgeRating;
        movie.Status = model.Status;
        movie.UpdatedAt = DateTime.Now;

        movie.Genres.Clear();
        foreach (var genreId in model.SelectedGenreIds)
        {
            var genre = await _unitOfWork.Genres.GetByIdAsync(genreId);
            if (genre != null) movie.Genres.Add(genre);
        }

        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    // Đổi trạng thái chiếu: chuyển qua lại giữa "Now Showing" và "Stopped".
    public async Task<Result> ToggleStatusAsync(Guid id)
    {
        var movie = await _unitOfWork.Movies.GetByIdAsync(id);
        if (movie is null)
            return Result.Failure("Không tìm thấy phim.");

        movie.Status = movie.Status == "Stopped" ? MovieStatus.NowShowing : "Stopped";
        movie.UpdatedAt = DateTime.Now;
        _unitOfWork.Movies.Update(movie);
        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    // Sinh slug từ tên phim: bỏ dấu tiếng Việt, thay khoảng trắng bằng "-".
    private static string GenerateSlug(string title)
    {
        var normalized = title.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        var result = sb.ToString().Normalize(NormalizationForm.FormC).ToLower();
        result = Regex.Replace(result, @"[^a-z0-9\s-]", "");
        result = Regex.Replace(result, @"\s+", "-");
        result = Regex.Replace(result, @"-+", "-");
        return result.Trim('-');
    }
}
