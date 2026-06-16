using Cinema_System.Application.DTOs;

namespace Cinema_System.Application.ViewModels;

public class FoodBeverageListViewModel
{
    public IEnumerable<FoodBeverageDTO> Items { get; set; } = new List<FoodBeverageDTO>();
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public string? Search { get; set; }
    public string? StatusFilter { get; set; }
}
