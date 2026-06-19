using Cinema_System.Application.DTOs;

namespace Cinema_System.Application.Interfaces
{
    // Nghiệp vụ quản lý phòng + ghế (nhân viên)
    public interface IRoomManagementService
    {
        Task<List<RoomListItemDto>> GetAllRoomsAsync();           // xem toàn bộ phòng
        Task<RoomSeatsDto?> GetRoomSeatsAsync(Guid roomId);       // sơ đồ ghế 1 phòng
        Task<bool> ToggleSeatStatusAsync(Guid seatId);            // đổi ghế mở <-> hỏng
    }
}
