namespace Cinema_System.Application.DTOs;

// Thông tin một suất chiếu để hiển thị lịch chiếu công khai.
public class ShowtimeDTO
{
    public Guid Id { get; set; }

    public Guid MovieId { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public string? MoviePosterUrl { get; set; }
    public int? DurationMinutes { get; set; }
    public string? AgeRating { get; set; }

    public Guid RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string? RoomTypeName { get; set; }
    public string CinemaName { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Status { get; set; }
}
