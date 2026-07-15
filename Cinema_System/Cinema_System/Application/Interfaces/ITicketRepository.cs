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
}
