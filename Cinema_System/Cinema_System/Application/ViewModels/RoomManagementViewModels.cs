namespace Cinema_System.Application.ViewModels;

// 1 phòng trong danh sách (có nhãn/màu trạng thái)
public class RoomListItemViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RoomTypeName { get; set; } = string.Empty;
    public int TotalSeats { get; set; }
    public string Status { get; set; } = string.Empty;

    public bool IsActive => Status == "Active";
    public string StatusLabel => Status == "Active" ? "Hoạt động" : "Bảo trì";
    public string StatusClass => Status == "Active" ? "bg-green-100 text-green-700" : "bg-orange-100 text-orange-700";
}

// Sơ đồ ghế 1 phòng
public class RoomSeatsViewModel
{
    public Guid RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string RoomTypeName { get; set; } = string.Empty;
    public string RoomStatus { get; set; } = string.Empty;
    public List<SeatItemViewModel> Seats { get; set; } = new();
    public int BrokenCount { get; set; }
    public int AvailableCount { get; set; }

    // Trạng thái bảo trì phòng (cho nút bật/tắt + badge)
    public bool IsUnderMaintenance => RoomStatus == "Maintenance" || RoomStatus == "Inactive";
    public string RoomStatusLabel => IsUnderMaintenance ? "Bảo trì" : "Hoạt động";
    public string RoomStatusClass => IsUnderMaintenance ? "bg-orange-100 text-orange-700" : "bg-green-100 text-green-700";

    // Danh sách tất cả phòng (cho cột trái màn quản lý ghế)
    public List<RoomListItemViewModel> AllRooms { get; set; } = new();
}

// 1 ghế (có nhãn + màu ô)
public class SeatItemViewModel
{
    public Guid Id { get; set; }
    public int RowNumber { get; set; }
    public int SeatNumber { get; set; }
    public string SeatTypeName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    public bool IsBroken => Status == "Broken";
    // Nhãn hàng: 1->A, 2->B...
    public string RowLabel => RowNumber >= 1 && RowNumber <= 26 ? ((char)('A' + RowNumber - 1)).ToString() : RowNumber.ToString();
    public string SeatLabel => RowLabel + SeatNumber;

    // Màu ô ghế: hỏng -> đỏ; còn lại theo loại ghế
    public string CellClass => IsBroken
        ? "bg-red-400 text-white"
        : SeatTypeName switch
        {
            "VIP" => "bg-blue-200 text-blue-800",
            "Couple" => "bg-purple-200 text-purple-800",
            "Premium" => "bg-amber-200 text-amber-800",
            _ => "bg-gray-100 text-gray-600"
        };
    public int UpcomingBookingCount { get; set; }                  // số vé sắp tới
    public bool HasUpcomingBooking => UpcomingBookingCount > 0;     // có khách hay không
}
