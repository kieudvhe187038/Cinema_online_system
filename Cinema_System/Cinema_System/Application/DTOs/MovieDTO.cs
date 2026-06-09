namespace Cinema_System.Application.DTOs;

public class MovieDTO
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Description { get; set; }
    public string? TrailerUrl { get; set; }
    public string? PosterUrl { get; set; }
    public string? BannerUrl { get; set; }
    public string? Director { get; set; }
    public string? CastMembers { get; set; }
    public string? Language { get; set; }
    public string? Subtitle { get; set; }
    public int? DurationMinutes { get; set; }
    public DateOnly? ReleaseDate { get; set; }
    public string? AgeRating { get; set; }
    public string? Status { get; set; }
    public List<string> GenreNames { get; set; } = new();
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
