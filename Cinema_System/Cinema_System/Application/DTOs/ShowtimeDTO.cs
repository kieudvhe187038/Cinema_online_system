namespace Cinema_System.Application.DTOs;

/// <summary>
/// DTO chứa thông tin suất chiếu phim
/// Được sử dụng để truyền dữ liệu suất chiếu giữa các lớp
/// </summary>
public class ShowtimeDTO
{
    /// <summary>ID duy nhất của suất chiếu</summary>
    public Guid Id { get; set; }
    
    /// <summary>Tên bộ phim</summary>
    public string MovieTitle { get; set; } = null!;
    
    /// <summary>Tên phòng chiếu</summary>
    public string RoomName { get; set; } = null!;
    
    /// <summary>Thời gian bắt đầu suất chiếu</summary>
    public DateTime StartTime { get; set; }
    
    /// <summary>Thời gian kết thúc suất chiếu</summary>
    public DateTime EndTime { get; set; }
    
    /// <summary>Trạng thái suất chiếu (Scheduled, Live, Completed, Cancelled)</summary>
    public string Status { get; set; } = null!;
    
    /// <summary>Có vé/đặt chỗ cho suất chiếu này hay không</summary>
    public bool HasBookings { get; set; }
}
