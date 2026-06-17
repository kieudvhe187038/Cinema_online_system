using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Interfaces;

/// <summary>Repository chuyên biệt cho <see cref="Seat"/>.</summary>
public interface ISeatRepository : IGenericRepository<Seat>
{
    /// <summary>Tất cả ghế của một phòng kèm loại ghế, sắp theo hàng rồi cột.</summary>
    Task<IReadOnlyList<Seat>> GetByRoomWithTypeAsync(Guid roomId);

    /// <summary>Các ghế theo danh sách Id kèm loại ghế — để kiểm tra &amp; tính giá khi tạo đơn.</summary>
    Task<IReadOnlyList<Seat>> GetByIdsWithTypeAsync(IEnumerable<Guid> seatIds);
}
