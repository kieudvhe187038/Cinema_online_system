using Cinema_System.Application.DTOs;

namespace Cinema_System.Application.ViewModels;

// Dữ liệu cho popup "đặt vé nhanh": ngày + suất chiếu của MỘT phim (dùng trong modal ở trang chủ / chi tiết phim).
public class MovieShowtimesViewModel
{
    public Guid MovieId { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public string? MoviePosterUrl { get; set; }

    // Các ngày (từ hôm nay tới 14 ngày) có suất chiếu, mỗi ngày kèm danh sách suất.
    public List<MovieShowtimeDay> Days { get; set; } = new();
}

// Một ngày chiếu và các suất trong ngày đó.
public class MovieShowtimeDay
{
    public DateOnly Date { get; set; }
    public List<ShowtimeDTO> Showtimes { get; set; } = new();
}
