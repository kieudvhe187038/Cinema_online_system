using AutoMapper;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers.Staff
{
    // Nhân viên quản lý phòng chiếu + ghế. Chỉ STAFF/MANAGER/ADMIN vào được.
    [Authorize(Roles = "STAFF,MANAGER,ADMIN")]
    public class RoomManagementController : Controller
    {
        private readonly IRoomManagementService _roomService;
        private readonly IMapper _mapper;

        public RoomManagementController(IRoomManagementService roomService, IMapper mapper)
        {
            _roomService = roomService;
            _mapper = mapper;
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

        // Đổi trạng thái ghế (mở <-> hỏng) rồi quay lại sơ đồ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSeat(Guid seatId, Guid roomId)
        {
            await _roomService.ToggleSeatStatusAsync(seatId);
            return RedirectToAction(nameof(Seats), new { roomId });
        }

    }
}
