using System;
using System.Collections.Generic;

namespace Cinema_System.Domain.Entities;

public partial class Movie
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

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<PriceBaseConfig> PriceBaseConfigs { get; set; } = new List<PriceBaseConfig>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();

    public virtual ICollection<Genre> Genres { get; set; } = new List<Genre>();
}
