using AutoMapper;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers.Staff
{
    // Quản lý phòng chiếu + ghế: CHỈ STAFF (chủ sở hữu nghiệp vụ). Manager dùng trang Sự cố;
    // Inter 2 & Inter 3 liên hệ qua trạng thái dùng chung (room status / showtime status), không cross-role.
    [Authorize(Roles = "STAFF")]
    public class RoomManagementController : Controller
    {
        private readonly IRoomManagementService _roomService;
        private readonly IMapper _mapper;
        private readonly IAuditLogWriter _audit;

        public RoomManagementController(IRoomManagementService roomService, IMapper mapper, IAuditLogWriter audit)
        {
            _roomService = roomService;
            _mapper = mapper;
            _audit = audit;
        }

        // Xem toàn bộ phòng chiếu
        public async Task<IActionResult> Index()
        {
            var rooms = await _roomService.GetAllRoomsAsync();
            var vm = rooms.Select(r => _mapper.Map<RoomListItemViewModel>(r)).ToList();
            return View(vm);
        }

        // Sơ đồ ghế 1 phòng
        public async Task<IActionResult> Seats(Guid roomId)
        {
            var data = await _roomService.GetRoomSeatsAsync(roomId);

            // Không tìm thấy phòng -> trả về view báo lỗi (vẫn nằm trong layout Staff),
            if (data == null)
            {
                var allRooms = await _roomService.GetAllRoomsAsync();
                var notFoundVm = new RoomSeatsViewModel
                {
                    AllRooms = allRooms.Select(r => _mapper.Map<RoomListItemViewModel>(r)).ToList()
                };
                Response.StatusCode = StatusCodes.Status404NotFound; // vẫn đúng chuẩn 404
                return View("RoomNotFound", notFoundVm);
            }

            var vm = _mapper.Map<RoomSeatsViewModel>(data);

            // Danh sách tất cả phòng cho cột trái
            var rooms = await _roomService.GetAllRoomsAsync();
            vm.AllRooms = rooms.Select(r => _mapper.Map<RoomListItemViewModel>(r)).ToList();

            return View(vm);
        }

        // Đổi trạng thái phòng (Bảo trì <-> Hoạt động) rồi quay lại sơ đồ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleRoomStatus(Guid roomId)
        {
            var result = await _roomService.ToggleRoomStatusAsync(roomId);
            if (result.Succeeded)
                await _audit.LogAsync("TOGGLE_ROOM_STATUS", "Rooms", roomId);

            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? result.Data : result.Error;
            return RedirectToAction(nameof(Seats), new { roomId });
        }
    }
}
