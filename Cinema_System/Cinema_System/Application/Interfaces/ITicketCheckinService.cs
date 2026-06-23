using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;

namespace Cinema_System.Application.Interfaces;

/// <summary>Check-in vé tại quầy: quét/nhập mã QR để xác thực và check-in (#Inter3).</summary>
public interface ITicketCheckinService
{
    /// <summary>
    /// Tra cứu vé theo mã QR (không thay đổi dữ liệu) để xem trước thông tin
    /// và trạng thái check-in. Trả về null nếu không tìm thấy vé.
    /// </summary>
    Task<TicketCheckinDTO?> LookupAsync(string? qrCode);

    /// <summary>
    /// Xác thực và check-in vé theo mã QR. Thành công khi vé hợp lệ và
    /// chưa check-in; thất bại kèm thông báo nếu vé đã hủy / đã check-in /
    /// suất chiếu đã kết thúc / chưa thanh toán.
    /// </summary>
    Task<Result<TicketCheckinDTO>> CheckinAsync(string? qrCode);

    /// <summary>
    /// Tra cứu một ĐƠN theo mã QR đơn (<c>BK...</c>) để xem trước danh sách vé
    /// và trạng thái check-in của từng vé. Trả về null nếu không tìm thấy đơn.
    /// </summary>
    Task<BookingCheckinDTO?> LookupBookingAsync(string? qrCode);

    /// <summary>
    /// Check-in hàng loạt tất cả vé hợp lệ của một đơn theo mã QR đơn (<c>BK...</c>).
    /// Bỏ qua vé đã hủy / đã check-in; thất bại nếu đơn chưa thanh toán hoặc
    /// suất chiếu đã kết thúc.
    /// </summary>
    Task<Result<BookingCheckinDTO>> CheckinBookingAsync(string? qrCode);
}
