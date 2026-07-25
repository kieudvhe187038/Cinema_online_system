using Cinema_System.Application.DTOs;
using Cinema_System.Application.Common;

namespace Cinema_System.Application.Interfaces
{
    // Nghiệp vụ quản lý phòng + ghế (nhân viên)
    public interface IRoomManagementService
    {
        Task<List<RoomListItemDto>> GetAllRoomsAsync();           // xem toàn bộ phòng
        Task<RoomSeatsDto?> GetRoomSeatsAsync(Guid roomId);       // sơ đồ ghế 1 phòng
        Task<Result<string>> ToggleRoomStatusAsync(Guid roomId); // Đổi trạng thái phòng Maintenance <-> Active.
    }
}
