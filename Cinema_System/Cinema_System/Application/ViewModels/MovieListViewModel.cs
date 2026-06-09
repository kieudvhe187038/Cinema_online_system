using Cinema_System.Application.DTOs;

namespace Cinema_System.Application.ViewModels;

public class MovieListViewModel
{
    public IEnumerable<MovieDTO> Movies { get; set; } = new List<MovieDTO>();
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public string? Search { get; set; }
    public string? StatusFilter { get; set; }
}
