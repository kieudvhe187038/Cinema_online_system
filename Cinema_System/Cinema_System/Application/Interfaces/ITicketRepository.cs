using Cinema_System.Application.DTOs;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Interfaces;

/// <summary>Repository chuyên biệt cho <see cref="Ticket"/>.</summary>
public interface ITicketRepository : IGenericRepository<Ticket>
{
    /// <summary>
    /// Check-in nguyên tử: chỉ cập nhật nếu vé đang ở trạng thái có thể check-in
    /// (chưa hủy, chưa check-in) tại thời điểm ghi xuống DB. Trả về false nếu vé đã
    /// bị người khác check-in/hủy trong lúc request này đang xử lý (race condition
    /// giữa hai lần quét cùng mã QR gần như đồng thời).
    /// </summary>
    Task<bool> TryCheckinAsync(Guid ticketId, Guid staffId, DateTime scannedAt);

    /// <summary>
    /// Đánh dấu 1 vé đã in nguyên tử: chỉ chuyển Booked -> Printed. Không hạ cấp vé đã
    /// Checked-in/Cancelled/đã Printed (WHERE status = Booked nên không khớp -> trả về
    /// false, không phải lỗi — vé đó vẫn in được, chỉ là không cần cập nhật cờ nữa).
    /// </summary>
    Task<bool> TryMarkPrintedAsync(Guid ticketId);

    /// <summary>
    /// Đánh dấu tất cả vé Booked của một đơn thành Printed (bỏ qua vé đã Cancelled/
    /// Checked-in/đã Printed). Trả về số vé vừa được cập nhật.
    /// </summary>
    Task<int> MarkBookingPrintedAsync(Guid bookingId);

    /// <summary>
    /// Top phim bán chạy (đếm vé + doanh thu) — gộp bằng GROUP BY dưới SQL và chỉ trả
    /// về <paramref name="top"/> dòng, không kéo cả bảng Tickets về bộ nhớ.
    /// </summary>
    Task<IReadOnlyList<HotMovieStatDto>> GetTopMoviesByTicketsAsync(int top);
}
