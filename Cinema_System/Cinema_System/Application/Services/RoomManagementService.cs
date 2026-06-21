using AutoMapper;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;

namespace Cinema_System.Application.Services
{
    // Truy cập DB qua IUnitOfWork (repo Rooms/Seats có sẵn). Không đụng DbContext.
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

        // Sơ đồ ghế 1 phòng + đếm hỏng/khả dụng
        public async Task<RoomSeatsDto?> GetRoomSeatsAsync(Guid roomId)
        {
            var room = await _unitOfWork.Rooms.FirstOrDefaultAsync(
                r => r.Id == roomId, includeProperties: new[] { "RoomType" });
            if (room == null) return null;

            var seats = await _unitOfWork.Seats.GetAllAsync(
                predicate: s => s.RoomId == roomId,
                includeProperties: new[] { "SeatType", "Tickets.Showtime", "Tickets.Booking" }, // + Booking để lọc đã thanh toán
                orderBy: q => q.OrderBy(s => s.RowNumber).ThenBy(s => s.SeatNumber));

            var now = DateTime.Now;
            var seatDtos = seats.Select(s =>
            {
                var dto = _mapper.Map<SeatItemDto>(s);
                // "Có khách" = số vé cho suất CHƯA diễn ra VÀ đơn ĐÃ THANH TOÁN
                dto.UpcomingBookingCount = s.Tickets.Count(t =>
                    t.Showtime != null && t.Showtime.StartTime > now &&
                    t.Booking != null && t.Booking.PaymentStatus == "Paid");
                return dto;
            }).ToList();


            return new RoomSeatsDto
            {
                RoomId = room.Id,
                RoomName = room.Name,
                RoomTypeName = room.RoomType?.Name ?? "",
                Seats = seatDtos,
                BrokenCount = seatDtos.Count(s => s.Status == "Broken"),
                AvailableCount = seatDtos.Count(s => s.Status != "Broken")
            };
        }

        // Đổi trạng thái ghế: Broken <-> Available
        public async Task<bool> ToggleSeatStatusAsync(Guid seatId)
        {
            var seat = await _unitOfWork.Seats.GetByIdAsync(seatId);
            if (seat == null) return false;

            seat.Status = seat.Status == "Broken" ? "Available" : "Broken";
            _unitOfWork.Seats.Update(seat);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}
