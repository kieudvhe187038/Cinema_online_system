namespace Cinema_System.Application.DTOs;

/// <summary>
/// Thông tin một vé hiển thị tại trang Check-in (sau khi quét/nhập mã QR),
/// kèm trạng thái cho biết vé có thể check-in hay không.
/// </summary>
public class TicketCheckinDTO
{
    public Guid TicketId { get; set; }
    public string TicketCode { get; set; } = string.Empty;
    public string BookingCode { get; set; } = string.Empty;

    public string MovieTitle { get; set; } = string.Empty;
    public string CinemaName { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public DateTime ShowStartTime { get; set; }
    public DateTime ShowEndTime { get; set; }

    public string SeatLabel { get; set; } = string.Empty;
    public string SeatType { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    /// <summary>Trạng thái vé hiện tại: Booked / Printed / Checked-in / Cancelled.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>True nếu vé vừa được check-in thành công hoặc đã ở trạng thái Checked-in.</summary>
    public bool CheckedIn { get; set; }

    /// <summary>Thời điểm và người đã check-in (nếu có).</summary>
    public DateTime? ScannedAt { get; set; }
    public string? ScanByName { get; set; }

    /// <summary>Thông báo trạng thái thân thiện để hiển thị cho nhân viên.</summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Thông tin một ĐƠN đặt vé tại trang Check-in (khi quét mã đơn <c>BK...</c>),
/// kèm danh sách vé để check-in hàng loạt.
/// </summary>
public class BookingCheckinDTO
{
    public Guid BookingId { get; set; }
    public string BookingCode { get; set; } = string.Empty;

    public string MovieTitle { get; set; } = string.Empty;
    public string CinemaName { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public DateTime ShowStartTime { get; set; }
    public DateTime ShowEndTime { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    /// <summary>Trạng thái thanh toán của đơn: Pending / Paid / Cancelled.</summary>
    public string PaymentStatus { get; set; } = string.Empty;

    /// <summary>Các vé thuộc đơn (kèm trạng thái từng vé).</summary>
    public List<TicketCheckinDTO> Tickets { get; set; } = new();

    /// <summary>Tổng số vé hợp lệ của đơn (không tính vé đã hủy).</summary>
    public int TotalTickets { get; set; }

    /// <summary>Số vé đang ở trạng thái đã check-in (sau thao tác).</summary>
    public int CheckedInCount { get; set; }

    /// <summary>Số vé vừa được check-in trong thao tác này.</summary>
    public int NewlyCheckedIn { get; set; }

    /// <summary>Thông báo trạng thái thân thiện để hiển thị cho nhân viên.</summary>
    public string Message { get; set; } = string.Empty;
}
