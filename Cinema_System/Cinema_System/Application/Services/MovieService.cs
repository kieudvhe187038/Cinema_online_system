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

public class MovieService : IMovieService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public MovieService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IEnumerable<MovieDTO>> GetAllMoviesAsync()
    {
        var movies = await _unitOfWork.Movies.GetAllAsync(
            include: q => q.Include(m => m.Genres));
        return _mapper.Map<IEnumerable<MovieDTO>>(movies);
    }

    public async Task<IEnumerable<MovieDTO>> GetNowShowingMoviesAsync()
    {
        var movies = await _unitOfWork.Movies.GetAllAsync(
            predicate: m => m.Status == "Now Showing",
            include: q => q.Include(m => m.Genres));
        return _mapper.Map<IEnumerable<MovieDTO>>(movies);
    }

    public async Task<IEnumerable<MovieDTO>> GetComingSoonMoviesAsync()
    {
        var movies = await _unitOfWork.Movies.GetAllAsync(
            predicate: m => m.Status == "Coming Soon",
            include: q => q.Include(m => m.Genres));
        return _mapper.Map<IEnumerable<MovieDTO>>(movies);
    }

    public async Task<IEnumerable<MovieDTO>> GetSpecialShowtimeMoviesAsync()
    {
        var movies = await _unitOfWork.Movies.GetAllAsync(
            predicate: m => m.Showtimes.Any(s =>
                s.Status == "Special" ||
                s.Status == "Special Screening" ||
                (s.Status != null && s.Status.Contains("Đặc"))),
            include: q => q.Include(m => m.Genres));
        return _mapper.Map<IEnumerable<MovieDTO>>(movies);
    }

    public async Task<MovieDTO?> GetMovieByIdAsync(Guid id)
    {
        var movie = await _unitOfWork.Movies.FirstOrDefaultAsync(
            m => m.Id == id,
            include: q => q.Include(m => m.Genres));
        return movie is null ? null : _mapper.Map<MovieDTO>(movie);
    }

    public async Task<IEnumerable<GenreDTO>> GetAllGenresAsync()
    {
        var genres = await _unitOfWork.Genres.GetAllAsync(
            orderBy: q => q.OrderBy(g => g.Name));
        return _mapper.Map<IEnumerable<GenreDTO>>(genres);
    }

    public async Task<MovieListViewModel> GetMoviesForAdminAsync(
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
        var allGenres = await GetAllGenresAsync();

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

    public async Task<MovieFormViewModel?> GetForEditAsync(Guid id)
    {
        var movie = await _unitOfWork.Movies.FirstOrDefaultAsync(
            m => m.Id == id,
            include: q => q.Include(m => m.Genres));

        if (movie is null) return null;

        var vm = _mapper.Map<MovieFormViewModel>(movie);
        vm.AvailableGenres = (await GetAllGenresAsync()).ToList();
        return vm;
    }

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
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
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
        movie.UpdatedAt = DateTime.UtcNow;

        movie.Genres.Clear();
        foreach (var genreId in model.SelectedGenreIds)
        {
            var genre = await _unitOfWork.Genres.GetByIdAsync(genreId);
            if (genre != null) movie.Genres.Add(genre);
        }

        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> ToggleStatusAsync(Guid id)
    {
        var movie = await _unitOfWork.Movies.GetByIdAsync(id);
        if (movie is null)
            return Result.Failure("Không tìm thấy phim.");

        movie.Status = movie.Status == "Inactive" ? "Now Showing" : "Inactive";
        movie.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Movies.Update(movie);
        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

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
