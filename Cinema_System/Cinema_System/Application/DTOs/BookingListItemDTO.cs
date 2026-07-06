namespace Cinema_System.Application.DTOs;

/// <summary>Một dòng trong danh sách quản lý đơn đặt vé (Booking Management).</summary>
public class BookingListItemDTO
{
    public Guid Id { get; set; }

    /// <summary>Mã đặt vé hiển thị cho khách (lưu ở Booking.QrCode).</summary>
    public string BookingCode { get; set; } = string.Empty;

    public string MovieTitle { get; set; } = string.Empty;

    public string RoomName { get; set; } = string.Empty;

    public DateTime ShowStartTime { get; set; }

    /// <summary>Tên khách (tài khoản đặt) hoặc "Khách lẻ" nếu offline không gắn tài khoản.</summary>
    public string CustomerName { get; set; } = string.Empty;

    public string? StaffName { get; set; }

    public int SeatCount { get; set; }

    public decimal FinalAmount { get; set; }

    /// <summary>Online / Offline.</summary>
    public string BookingType { get; set; } = string.Empty;

    /// <summary>Pending / Paid / Cancelled ...</summary>
    public string PaymentStatus { get; set; } = string.Empty;

    public DateTime? CreatedAt { get; set; }
}
