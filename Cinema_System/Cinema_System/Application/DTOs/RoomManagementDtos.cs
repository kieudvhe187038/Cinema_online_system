namespace Cinema_System.Application.DTOs;

// 1 phòng trong danh sách
public class RoomListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RoomTypeName { get; set; } = string.Empty;
    public int TotalSeats { get; set; }
    public string Status { get; set; } = string.Empty; // Active / Maintenance
}

// Sơ đồ ghế của 1 phòng
public class RoomSeatsDto
{
    public Guid RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string RoomTypeName { get; set; } = string.Empty;
    public string RoomStatus { get; set; } = string.Empty; // Active / Maintenance / Inactive
    public List<SeatItemDto> Seats { get; set; } = new();
    public int BrokenCount { get; set; }
    public int AvailableCount { get; set; }
}

// 1 ghế
public class SeatItemDto
{
    public Guid Id { get; set; }
    public int RowNumber { get; set; }
    public int SeatNumber { get; set; }
    public string SeatTypeName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Available / Active / Broken
    public int UpcomingBookingCount { get; set; } // số vé ĐÃ THANH TOÁN cho suất sắp tới
}
