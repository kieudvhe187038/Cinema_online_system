namespace Cinema_System.Application.DTOs;

/// <summary>
/// DTO chứa thông tin suất chiếu phim dùng cho màn quản lý lịch chiếu (Manager).
/// </summary>
public class ShowtimeScheduleDTO
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

    /// <summary>Số vé đã bán cho suất chiếu này</summary>
    public int SeatsSold { get; set; }

    /// <summary>Tổng số ghế của phòng chiếu (sức chứa)</summary>
    public int Capacity { get; set; }
}
