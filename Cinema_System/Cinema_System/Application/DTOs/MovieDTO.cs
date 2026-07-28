namespace Cinema_System.Application.DTOs;

// DTO phim truyền dữ liệu phim từ service lên view.
public class MovieDTO
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

    // Phim còn suất chiếu sắp tới (chưa hủy) -> cho phép đặt vé, kể cả phim sắp chiếu/chiếu sớm.
    // Chỉ đúng khi truy vấn có Include "Showtimes".
    public bool HasUpcomingShowtimes { get; set; }

    // Điểm đánh giá trung bình (thang 1-5) tính trên các review ĐÃ DUYỆT; null khi chưa có review nào.
    // AutoMapper KHÔNG tự điền 2 trường này — service phải gán tay (xem MovieService.GetBannerMoviesAsync).
    public double? AverageRating { get; set; }

    // Số lượt đánh giá đã duyệt.
    public int ReviewCount { get; set; }

    public List<string> GenreNames { get; set; } = new();
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
