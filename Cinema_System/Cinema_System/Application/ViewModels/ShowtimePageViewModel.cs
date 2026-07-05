using Cinema_System.Application.DTOs;

namespace Cinema_System.Application.ViewModels;

// Dữ liệu cho trang Lịch chiếu công khai: bộ lọc (phim/thể loại/độ tuổi/loại phòng) + danh sách suất chiếu.
public class ShowtimePageViewModel
{
    // ── Giá trị bộ lọc đang chọn ──
    public Guid? MovieId { get; set; }
    public Guid? GenreId { get; set; }
    public string? AgeRating { get; set; }
    public Guid? RoomTypeId { get; set; }
    public DateOnly SelectedDate { get; set; }

    // ── Tùy chọn cho dropdown / thanh ngày ──
    public List<ShowtimeFilterOption> Movies { get; set; } = new();
    public List<ShowtimeFilterOption> Genres { get; set; } = new();
    public List<string> AgeRatings { get; set; } = new();
    public List<ShowtimeFilterOption> RoomTypes { get; set; } = new();
    public List<DateOnly> AvailableDates { get; set; } = new();

    // ── Kết quả ──
    public List<ShowtimeDTO> Showtimes { get; set; } = new();
}

// Một mục chọn (Id + nhãn hiển thị) cho dropdown phim/phòng.
public class ShowtimeFilterOption
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
