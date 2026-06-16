using Cinema_System.Application.DTOs;
namespace Cinema_System.Application.ViewModels;

// ViewModel dùng cho trang danh sách phim và tìm kiếm phim.
public class MoviesPageViewModel
{
    public string SelectedTab { get; set; } = "now";
    // Từ khóa đang tìm kiếm, dùng để giữ giá trị trong form và phân trang.
    public string SearchKeyword { get; set; } = string.Empty;
    public IEnumerable<MovieDTO> Movies { get; set; } = new List<MovieDTO>();
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int PageSize { get; set; } = 8;

    // Bộ lọc trong tab đang hiển thị.
    public string? SelectedGenre { get; set; }
    public string? SelectedAgeRating { get; set; }
    public IEnumerable<string> AvailableGenres { get; set; } = new List<string>();
    public IEnumerable<string> AvailableAgeRatings { get; set; } = new List<string>();
}
