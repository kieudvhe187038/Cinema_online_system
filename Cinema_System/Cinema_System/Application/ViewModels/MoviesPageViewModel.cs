using Cinema_System.Application.DTOs;
namespace Cinema_System.Application.ViewModels;

public class MoviesPageViewModel
{
    public string SelectedTab { get; set; } = "now";
    public IEnumerable<MovieDTO> Movies { get; set; } = new List<MovieDTO>();
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int PageSize { get; set; } = 4;
}
