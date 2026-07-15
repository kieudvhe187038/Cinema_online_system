namespace Cinema_System.Application.DTOs;

/// <summary>
/// Kết quả gộp "phim bán chạy" tính trực tiếp bằng GROUP BY dưới SQL
/// (dùng cho Admin Dashboard) — tránh kéo toàn bộ bảng Tickets về bộ nhớ.
/// </summary>
public class HotMovieStatDto
{
    public string MovieTitle { get; set; } = string.Empty;
    public string? PosterUrl { get; set; }
    public int TicketsSold { get; set; }
    public decimal Revenue { get; set; }
}
