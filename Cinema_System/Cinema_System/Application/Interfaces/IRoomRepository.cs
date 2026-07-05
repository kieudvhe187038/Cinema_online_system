using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Interfaces;

/// <summary>Repository chuyên biệt cho <see cref="Room"/>.</summary>
public interface IRoomRepository : IGenericRepository<Room>
{
    /// <summary>Lấy phòng theo Id kèm loại phòng (RoomType) — phục vụ tính giá.</summary>
    Task<Room?> GetByIdWithRoomTypeAsync(Guid roomId);
}
