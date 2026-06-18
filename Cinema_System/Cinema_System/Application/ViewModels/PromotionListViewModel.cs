using Cinema_System.Application.DTOs;

namespace Cinema_System.Application.ViewModels;

public class PromotionListViewModel
{
    public IEnumerable<PromotionDTO> Items { get; set; } = new List<PromotionDTO>();
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public string? Search { get; set; }
    public string? StatusFilter { get; set; }
}
