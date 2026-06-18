using Cinema_System.Application.DTOs;

namespace Cinema_System.Application.ViewModels;

/// <summary>
/// ViewModel cho trang lịch chiếu tuần
/// Chứa dữ liệu hiển thị lịch chiếu, bộ lọc, và thống kê
/// </summary>
public class ShowtimeCalendarViewModel
{
    /// <summary>Danh sách các phòng chiếu khả dụng</summary>
    public IEnumerable<ItemOptionDTO> AvailableRooms { get; set; } = new List<ItemOptionDTO>();
    
    /// <summary>ID phòng chiếu được chọn (null = tất cả)</summary>
    public Guid? SelectedRoomId { get; set; }
    
    /// <summary>Tên phòng chiếu được chọn</summary>
    public string SelectedRoomName { get; set; } = "Tất cả phòng chiếu";
    
    /// <summary>Ngày bắt đầu của tuần</summary>
    public DateTime WeekStart { get; set; }
    
    /// <summary>Danh sách các ngày trong tuần (Monday-Sunday)</summary>
    public IEnumerable<DateTime> WeekDays { get; set; } = new List<DateTime>();
    
    /// <summary>Danh sách suất chiếu của tuần</summary>
    public IEnumerable<ShowtimeDTO> Showtimes { get; set; } = new List<ShowtimeDTO>();
    
    /// <summary>Từ khóa tìm kiếm (phim hoặc phòng)</summary>
    public string? Search { get; set; }
    
    /// <summary>Bộ lọc trạng thái (Scheduled, Live, Completed, Cancelled)</summary>
    public string? StatusFilter { get; set; }
    
    /// <summary>Tổng số phim khác nhau trong tuần</summary>
    public int TotalMovies { get; set; }
    
    /// <summary>Tổng số suất chiếu trong tuần</summary>
    public int TotalShowtimes { get; set; }
    
    /// <summary>Tỷ lệ phần trăm suất chiếu đã được đặt vé</summary>
    public int ConfirmedRate { get; set; }
}
