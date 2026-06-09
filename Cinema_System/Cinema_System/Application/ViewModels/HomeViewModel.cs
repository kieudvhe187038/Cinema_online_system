using Cinema_System.Application.DTOs;

namespace Cinema_System.Application.ViewModels;

public class HomeViewModel
{
    public string? SelectedGenre { get; set; }
    public string? SelectedAgeRating { get; set; }
    public string? SelectedStatus { get; set; }

    public IEnumerable<string> AvailableGenres { get; set; } = new List<string>();
    public IEnumerable<string> AvailableAgeRatings { get; set; } = new List<string>();
    public IEnumerable<string> AvailableStatuses { get; set; } = new List<string>();

    public IEnumerable<MovieDTO> FilteredMovies { get; set; } = new List<MovieDTO>();
    public IEnumerable<MovieDTO> NowShowingMovies { get; set; } = new List<MovieDTO>();
    public IEnumerable<MovieDTO> ComingSoonMovies { get; set; } = new List<MovieDTO>();
    public IEnumerable<MovieDTO> SpecialShowtimeMovies { get; set; } = new List<MovieDTO>();
}
