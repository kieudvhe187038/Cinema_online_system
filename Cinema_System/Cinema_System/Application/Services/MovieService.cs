using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cinema_System.Application.Services;

public class MovieService : IMovieService
{
    private readonly IUnitOfWork _unitOfWork;

    public MovieService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<MovieDTO>> GetAllMoviesAsync()
    {
        var movies = await _unitOfWork.Movies.GetAllAsync(
            include: q => q.Include(m => m.Showtimes)
        );

        return movies.Select(MapToDTO);
    }

    public async Task<IEnumerable<MovieDTO>> GetNowShowingMoviesAsync()
    {
        var movies = await _unitOfWork.Movies.GetAllAsync(
            predicate: m => m.Status == "Now Showing",
            include: q => q.Include(m => m.Showtimes)
        );

        return movies.Select(MapToDTO);
    }

    public async Task<IEnumerable<MovieDTO>> GetComingSoonMoviesAsync()
    {
        var movies = await _unitOfWork.Movies.GetAllAsync(
            predicate: m => m.Status == "Coming Soon",
            include: q => q.Include(m => m.Showtimes)
        );

        return movies.Select(MapToDTO);
    }

    public async Task<MovieDTO> GetMovieByIdAsync(Guid id)
    {
        var movie = await _unitOfWork.Movies.FirstOrDefaultAsync(
            predicate: m => m.Id == id,
            include: q => q.Include(m => m.Showtimes)
        );

        return movie == null ? null : MapToDTO(movie);
    }

    public async Task<IEnumerable<MovieDTO>> GetSpecialShowtimeMoviesAsync()
    {
        var movies = await _unitOfWork.Movies.GetAllAsync(
            include: q => q.Include(m => m.Showtimes)
        );

        var specialMovies = movies.Where(m =>
            m.Showtimes.Any(s =>
                s.Status == "Special" ||
                s.Status == "Special Screening" ||
                (s.Status != null && s.Status.Contains("Đặc"))
            )
        );

        return specialMovies.Select(MapToDTO);
    }

    private static MovieDTO MapToDTO(Domain.Entities.Movie m)
    {
        return new MovieDTO
        {
            Id = m.Id,
            Title = m.Title,
            Slug = m.Slug,
            Description = m.Description,
            TrailerUrl = m.TrailerUrl,
            PosterUrl = m.PosterUrl,
            BannerUrl = m.BannerUrl,
            Director = m.Director,
            CastMembers = m.CastMembers,
            Language = m.Language,
            Subtitle = m.Subtitle,
            DurationMinutes = m.DurationMinutes,
            ReleaseDate = m.ReleaseDate,
            AgeRating = m.AgeRating,
            Status = m.Status
        };
    }
}
