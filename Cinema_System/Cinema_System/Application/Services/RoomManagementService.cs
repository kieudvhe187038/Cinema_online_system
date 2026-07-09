using System.Globalization;
using AutoMapper;
using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Services
{
    // Truy cập DB qua IUnitOfWork (repo Rooms/Seats/Tickets...). Không đụng DbContext.
    public class RoomManagementService : IRoomManagementService
    {
        private const string RewardRateKey = "reward_point_rate";

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

        // Đổi trạng thái ghế Broken <-> Available.
        // Khi KHÓA ghế có khách Paid cho suất sắp tới: đổi sang ghế trống cùng phòng trước;
        // hết ghế trống thì hoàn điểm bồi thường cho khách.
        public async Task<Result<string>> ToggleSeatStatusAsync(Guid seatId)
        {
            var seat = await _unitOfWork.Seats.FirstOrDefaultAsync(
                s => s.Id == seatId,
                includeProperties: new[] { "Tickets.Showtime", "Tickets.Booking" });
            if (seat == null) return Result<string>.Failure("Không tìm thấy ghế.");

            var lockingNow = seat.Status != "Broken";        // sắp chuyển sang Broken?
            seat.Status = lockingNow ? "Broken" : "Available";
            _unitOfWork.Seats.Update(seat);

            // Mở lại ghế: không xử lý gì thêm
            if (!lockingNow)
            {
                await _unitOfWork.SaveChangesAsync();
                return Result<string>.Success("Đã mở lại ghế cho sử dụng bình thường.");
            }

            // Vé bị ảnh hưởng = suất chưa diễn ra + đơn đã thanh toán
            var now = DateTime.Now;
            var affected = seat.Tickets
                .Where(t => t.Showtime != null && t.Showtime.StartTime > now &&
                            t.Booking != null && t.Booking.PaymentStatus == "Paid")
                .ToList();

            if (affected.Count == 0)
            {
                await _unitOfWork.SaveChangesAsync();
                return Result<string>.Success("Đã khóa ghế (không có khách nào bị ảnh hưởng).");
            }

            // Toàn bộ ghế trong phòng để tìm chỗ trống thay thế
            var roomSeats = (await _unitOfWork.Seats.GetAllAsync(s => s.RoomId == seat.RoomId)).ToList();
            var rewardRate = ParseDecimal(
                (await _unitOfWork.SystemConfigs.FirstOrDefaultAsync(c => c.ConfigKey == RewardRateKey))?.ConfigValue);

            int reassigned = 0, compensated = 0;
            // Ghế vừa gán trong thao tác này (tránh gán trùng cho cùng 1 suất)
            var assignedPerShowtime = new Dictionary<Guid, HashSet<Guid>>();

            foreach (var ticket in affected)
            {
                var showtimeId = ticket.ShowtimeId;

                // Ghế đã có người cho suất này (đã bán vé)
                var occupied = (await _unitOfWork.Tickets.GetAllAsync(x => x.ShowtimeId == showtimeId))
                    .Select(x => x.SeatId).ToHashSet();
                occupied.Add(seat.Id);
                if (assignedPerShowtime.TryGetValue(showtimeId, out var just))
                    foreach (var id in just) occupied.Add(id);

                // Né cả ghế đang bị GIỮ TẠM (khách khác đang đặt dở) để không đổi khách vào ghế đó
                var heldSeatIds = (await _unitOfWork.SeatHolds.GetAllAsync(
                    h => h.ShowtimeId == showtimeId && h.Status == "Holding" && h.ExpiresAt > now))
                    .Select(h => h.SeatId);
                foreach (var id in heldSeatIds) occupied.Add(id);

                // Ghế trống khả dụng: ưu tiên cùng loại, rồi theo vị trí
                var chosen = roomSeats
                    .Where(s => s.Id != seat.Id && s.Status != "Broken" && !occupied.Contains(s.Id))
                    .OrderByDescending(s => s.SeatTypeId == seat.SeatTypeId)
                    .ThenBy(s => s.RowNumber).ThenBy(s => s.SeatNumber)
                    .FirstOrDefault();

                if (chosen != null)
                {
                    ticket.SeatId = chosen.Id;                  // đổi khách sang ghế mới
                    _unitOfWork.Tickets.Update(ticket);
                    if (!assignedPerShowtime.ContainsKey(showtimeId))
                        assignedPerShowtime[showtimeId] = new HashSet<Guid>();
                    assignedPerShowtime[showtimeId].Add(chosen.Id);
                    reassigned++;
                }
                else if (ticket.Booking?.UserId != null)
                {
                    // Hết ghế trống -> hoàn điểm bồi thường
                    var points = (int)Math.Round(ticket.PriceAtBooking * rewardRate, MidpointRounding.AwayFromZero);
                    if (points < 1) points = 1;

                    await _unitOfWork.RewardPointHistories.AddAsync(new RewardPointHistory
                    {
                        Id = Guid.NewGuid(),
                        UserId = ticket.Booking.UserId.Value,
                        BookingId = ticket.BookingId,
                        PointsChanged = points,
                        ActionType = "Refund_Rollback",
                        Description = "Hoàn điểm do ghế hỏng, không còn ghế trống thay thế",
                        CreatedAt = now
                    });

                    var user = await _unitOfWork.Users.GetByIdAsync(ticket.Booking.UserId.Value);
                    if (user != null)
                    {
                        user.RewardPoints = (user.RewardPoints ?? 0) + points;
                        _unitOfWork.Users.Update(user);
                    }
                    compensated++;
                }
            }

            await _unitOfWork.SaveChangesAsync();

            var msg = $"Đã khóa ghế. Đổi chỗ cho {reassigned} khách";
            msg += compensated > 0 ? $", hoàn điểm cho {compensated} khách (hết ghế trống)." : ".";
            return Result<string>.Success(msg);
        }

        private static decimal ParseDecimal(string? value)
            => decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m;
    }
}
