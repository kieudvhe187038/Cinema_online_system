using AutoMapper;
using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;

namespace Cinema_System.Application.Services
{
    // Truy cập DB qua IUnitOfWork (repo Rooms/Seats...). Không đụng DbContext.
    public class RoomManagementService : IRoomManagementService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public RoomManagementService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        // Danh sách phòng (kèm loại phòng), sắp theo tên
        public async Task<List<RoomListItemDto>> GetAllRoomsAsync()
        {
            var rooms = await _unitOfWork.Rooms.GetAllAsync(
                includeProperties: new[] { "RoomType" },
                orderBy: q => q.OrderBy(r => r.Name));
            return rooms.Select(r => _mapper.Map<RoomListItemDto>(r)).ToList();
        }

        // Sơ đồ ghế 1 phòng + đếm hỏng/khả dụng (chỉ để XEM)
        public async Task<RoomSeatsDto?> GetRoomSeatsAsync(Guid roomId)
        {
            var room = await _unitOfWork.Rooms.FirstOrDefaultAsync(
                r => r.Id == roomId, includeProperties: new[] { "RoomType" });
            if (room == null) return null;

            var seats = await _unitOfWork.Seats.GetAllAsync(
                predicate: s => s.RoomId == roomId,
                includeProperties: new[] { "SeatType", "Tickets.Showtime", "Tickets.Booking" },
                orderBy: q => q.OrderBy(s => s.RowNumber).ThenBy(s => s.SeatNumber));

            var now = DateTime.Now;
            var seatDtos = seats.Select(s =>
            {
                var dto = _mapper.Map<SeatItemDto>(s);
                // "Có khách" = vé cho suất CHƯA diễn, CHƯA bị hủy, đơn ĐÃ THANH TOÁN
                dto.UpcomingBookingCount = s.Tickets.Count(t =>
                    t.Showtime != null && t.Showtime.StartTime > now && t.Showtime.Status != "Cancelled" &&
                    t.Booking != null && t.Booking.PaymentStatus == "Paid");
                return dto;
            }).ToList();

            return new RoomSeatsDto
            {
                RoomId = room.Id,
                RoomName = room.Name,
                RoomTypeName = room.RoomType?.Name ?? "",
                RoomStatus = room.Status ?? "Active",
                Seats = seatDtos,
                BrokenCount = seatDtos.Count(s => s.Status == "Broken"),
                AvailableCount = seatDtos.Count(s => s.Status != "Broken")
            };
        }

        // Đổi trạng thái phòng: Maintenance <-> Active (badge "Bảo trì" / "Hoạt động"), có hoàn tác.
        public async Task<Result<string>> ToggleRoomStatusAsync(Guid roomId)
        {
            var room = await _unitOfWork.Rooms.GetByIdAsync(roomId);
            if (room == null) return Result<string>.Failure("Không tìm thấy phòng.");

            if (room.Status == "Maintenance" || room.Status == "Inactive")
            {
                room.Status = "Active";
                _unitOfWork.Rooms.Update(room);
                await _unitOfWork.SaveChangesAsync();
                return Result<string>.Success($"Đã mở lại phòng {room.Name} (Hoạt động).");
            }

            room.Status = "Maintenance";
            _unitOfWork.Rooms.Update(room);
            await _unitOfWork.SaveChangesAsync();
            return Result<string>.Success($"Đã đặt phòng {room.Name} sang Bảo trì.");
        }
    }
}
