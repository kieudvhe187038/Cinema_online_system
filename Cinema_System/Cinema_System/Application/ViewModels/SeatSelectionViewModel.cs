using Cinema_System.Application.DTOs;

namespace Cinema_System.Application.ViewModels;

// Dữ liệu trang chọn ghế: thông tin suất chiếu + sơ đồ ghế theo hàng.
public class SeatSelectionViewModel
{
    public Guid ShowtimeId { get; set; }

    public string MovieTitle { get; set; } = string.Empty;
    public string? MoviePosterUrl { get; set; }
    public string? AgeRating { get; set; }

    public string RoomName { get; set; } = string.Empty;
    public string CinemaName { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    // Sơ đồ ghế gom theo hàng (đã sắp xếp).
    public List<SeatRowViewModel> Rows { get; set; } = new();
}

// Một hàng ghế trong sơ đồ.
public class SeatRowViewModel
{
    public int RowNumber { get; set; }
    public string RowLabel { get; set; } = string.Empty;
    public List<SeatDTO> Seats { get; set; } = new();
}
