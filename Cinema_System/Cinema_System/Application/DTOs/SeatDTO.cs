namespace Cinema_System.Application.DTOs;

// Một ghế trong sơ đồ phòng chiếu kèm trạng thái cho 1 suất chiếu.
public class SeatDTO
{
    public Guid Id { get; set; }
    public int RowNumber { get; set; }
    public int SeatNumber { get; set; }
    public string RowLabel { get; set; } = string.Empty;   // A, B, C...
    public string SeatTypeName { get; set; } = string.Empty;

    // Giá vé của ghế này cho suất chiếu (đã gồm base + phụ thu ghế/phòng/giờ).
    public decimal Price { get; set; }

    // Available = trống (chọn được) | Booked = đã đặt | Held = đang giữ | Broken = hỏng.
    public string State { get; set; } = "Available";
    public bool Selectable => State == "Available";

    // Nhãn hiển thị ghế, vd "A1".
    public string Label => $"{RowLabel}{SeatNumber}";
}
