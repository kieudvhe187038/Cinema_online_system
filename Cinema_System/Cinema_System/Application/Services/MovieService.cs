using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;
using Cinema_System.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Cinema_System.Application.Services;

public class MovieService : IMovieService
{
    private readonly CinemaWebDbContext _context;

    public MovieService(CinemaWebDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<MovieDTO>> GetAllMoviesAsync()
    {
        var movies = await _context.Movies
            .Select(m => MapToDTO(m))
            .ToListAsync();

        return movies;
    }

    public async Task<IEnumerable<MovieDTO>> GetNowShowingMoviesAsync()
    {
        var movies = await _context.Movies
            .Where(m => m.Status == "Now Showing")
            .Select(m => MapToDTO(m))
            .ToListAsync();

        return movies;
    }

    public async Task<IEnumerable<MovieDTO>> GetComingSoonMoviesAsync()
    {
        var movies = await _context.Movies
            .Where(m => m.Status == "Coming Soon")
            .Select(m => MapToDTO(m))
            .ToListAsync();

        return movies;
    }

    public async Task<MovieDTO> GetMovieByIdAsync(Guid id)
    {
        var movie = await _context.Movies
            .Where(m => m.Id == id)
            .Select(m => MapToDTO(m))
            .FirstOrDefaultAsync();

        return movie;
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
