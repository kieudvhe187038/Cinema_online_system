namespace Cinema_System.Application.DTOs;

// Một dòng gợi ý của ô tìm kiếm phim ở header — chỉ mang đủ dữ liệu để vẽ dropdown.
public class MovieSuggestionDTO
{
    public string Title { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? PosterUrl { get; set; }
    public string? Status { get; set; }
    public int? ReleaseYear { get; set; }
}
