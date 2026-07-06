namespace Cinema_System.Application.DTOs;

/// <summary>Một dòng phòng chiếu ở màn danh sách.</summary>
public class RoomDTO
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public Guid RoomTypeId { get; set; }

    public string RoomTypeName { get; set; } = null!;

    public string? Status { get; set; }

    public int? TotalRow { get; set; }

    public int? TotalColumns { get; set; }

    /// <summary>Số đơn vị ghế đã bố trí trên sơ đồ (đếm thực tế từ bảng Seats).</summary>
    public int SeatCount { get; set; }

    /// <summary>True nếu phòng đã gắn suất chiếu → không cho xóa.</summary>
    public bool HasShowtimes { get; set; }

    /// <summary>True nếu đã có vé đặt cho phòng → khóa chỉnh sửa sơ đồ ghế.</summary>
    public bool HasBookedTickets { get; set; }
}
