using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Cinema_System.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cinema_System.Application.Services;

// Truy vấn lịch chiếu cho giao diện công khai (mọi role, kể cả khách chưa đăng nhập).
public class ShowtimeService : IShowtimeService
{
    private readonly IUnitOfWork _unitOfWork;

    // Nhận UnitOfWork qua DI để truy cập các repository (Showtimes, Seats, Tickets...).
    public ShowtimeService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // Lấy dữ liệu trang lịch chiếu: lọc theo phim/phòng/ngày + tùy chọn dropdown.
    public async Task<ShowtimePageViewModel> GetShowtimePageAsync(Guid? movieId, Guid? roomId, DateOnly? date)
    {
        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);

        // Chỉ cho xem lịch từ hiện tại đến tương lai: ép ngày đã chọn về hôm nay nếu nằm trong quá khứ.
        var selectedDate = date ?? today;
        if (selectedDate < today) selectedDate = today;

        var dayEnd = selectedDate.ToDateTime(TimeOnly.MinValue).AddDays(1);
        // Mốc bắt đầu: nếu là hôm nay thì chỉ lấy suất từ thời điểm hiện tại trở đi (bỏ suất đã qua trong ngày).
        var lowerBound = selectedDate == today ? now : selectedDate.ToDateTime(TimeOnly.MinValue);

        // Suất chiếu trong ngày đã chọn (từ hiện tại trở đi), lọc thêm theo phim/phòng nếu có.
        var showtimes = await _unitOfWork.Showtimes.GetAllAsync(
            predicate: s =>
                s.StartTime >= lowerBound && s.StartTime < dayEnd &&
                (movieId == null || s.MovieId == movieId) &&
                (roomId == null || s.RoomId == roomId) &&
                s.Status != "Cancelled",
            include: q => q
                .Include(s => s.Movie)
                .Include(s => s.Room).ThenInclude(r => r.RoomType)
                .Include(s => s.Room).ThenInclude(r => r.Cinema),
            orderBy: q => q.OrderBy(s => s.StartTime));

        var showtimeDtos = showtimes.Select(s => new ShowtimeDTO
        {
            Id = s.Id,
            MovieId = s.MovieId,
            MovieTitle = s.Movie.Title,
            MoviePosterUrl = s.Movie.PosterUrl,
            DurationMinutes = s.Movie.DurationMinutes,
            AgeRating = s.Movie.AgeRating,
            RoomId = s.RoomId,
            RoomName = s.Room.Name,
            RoomTypeName = s.Room.RoomType?.Name,
            CinemaName = s.Room.Cinema?.Name ?? string.Empty,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            Status = s.Status
        }).ToList();

        // Tùy chọn dropdown: phim & phòng (kèm tên rạp).
        var movies = await _unitOfWork.Movies.GetAllAsync(
            orderBy: q => q.OrderBy(m => m.Title));
        var rooms = await _unitOfWork.Rooms.GetAllAsync(
            include: q => q.Include(r => r.Cinema),
            orderBy: q => q.OrderBy(r => r.Name));

        return new ShowtimePageViewModel
        {
            MovieId = movieId,
            RoomId = roomId,
            SelectedDate = selectedDate,
            Movies = movies.Select(m => new ShowtimeFilterOption { Id = m.Id, Name = m.Title }).ToList(),
            Rooms = rooms.Select(r => new ShowtimeFilterOption
            {
                Id = r.Id,
                Name = r.Cinema != null ? $"{r.Name} — {r.Cinema.Name}" : r.Name
            }).ToList(),
            // Thanh chọn ngày: 14 ngày kể từ hôm nay (không có ngày quá khứ).
            AvailableDates = Enumerable.Range(0, 14)
                .Select(i => today.AddDays(i))
                .ToList(),
            Showtimes = showtimeDtos
        };
    }

    // Lấy sơ đồ ghế cho 1 suất chiếu, đánh dấu trạng thái từng ghế.
    public async Task<SeatSelectionViewModel?> GetSeatSelectionAsync(Guid showtimeId, Guid? currentUserId = null)
    {
        // Thông tin suất chiếu kèm phim & phòng/rạp.
        var showtimeList = await _unitOfWork.Showtimes.GetAllAsync(
            predicate: s => s.Id == showtimeId,
            include: q => q
                .Include(s => s.Movie)
                .Include(s => s.Room).ThenInclude(r => r.Cinema));
        var showtime = showtimeList.FirstOrDefault();
        if (showtime is null)
            return null;

        // Ghế đã đặt (có vé chưa hủy) và ghế đang được giữ (chưa hết hạn) của suất này.
        var bookedTickets = await _unitOfWork.Tickets.GetAllAsync(
            predicate: t => t.ShowtimeId == showtimeId && t.Status != "Cancelled");
        var bookedSeatIds = bookedTickets.Select(t => t.SeatId).ToHashSet();

        // Ghế đang được giữ bởi NGƯỜI KHÁC (hold còn hiệu lực). Hold của chính user hiện tại không tính.
        var now = DateTime.Now;
        var activeHolds = await _unitOfWork.SeatHolds.GetAllAsync(
            predicate: h => h.ShowtimeId == showtimeId && h.ExpiresAt > now
                && h.Status == "Holding" && h.UserId != currentUserId);
        var heldSeatIds = activeHolds.Select(h => h.SeatId).ToHashSet();

        // Toàn bộ ghế của phòng, sắp theo hàng rồi số ghế.
        var seats = await _unitOfWork.Seats.GetAllAsync(
            predicate: s => s.RoomId == showtime.RoomId,
            include: q => q.Include(s => s.SeatType),
            orderBy: q => q.OrderBy(s => s.RowNumber).ThenBy(s => s.SeatNumber));

        // ── Tính giá vé cho suất chiếu này ──
        var st = showtime.StartTime;
        bool Active(string? status) => status == "Active";
        bool Effective(DateTime from, DateTime? to) => from <= st && (to == null || to >= st);

        // Giá gốc: ưu tiên cấu hình theo phim, không có thì lấy cấu hình chung (movie_id = null).
        var baseConfigs = (await _unitOfWork.PriceBaseConfigs.GetAllAsync(
            predicate: p => p.Status == "Active" && p.EffectiveFrom <= st && (p.EffectiveTo == null || p.EffectiveTo >= st)))
            .ToList();
        var basePrice = baseConfigs.Where(p => p.MovieId == showtime.MovieId).OrderByDescending(p => p.EffectiveFrom)
                            .Select(p => (decimal?)p.BasePrice).FirstOrDefault()
                        ?? baseConfigs.Where(p => p.MovieId == null).OrderByDescending(p => p.EffectiveFrom)
                            .Select(p => (decimal?)p.BasePrice).FirstOrDefault()
                        ?? 0m;

        // Phụ thu loại phòng.
        var roomTypeId = showtime.Room.RoomTypeId;
        var roomSurcharge = (await _unitOfWork.PriceRoomTypeConfigs.GetAllAsync(
            predicate: p => p.RoomTypeId == roomTypeId))
            .Where(p => Active(p.Status) && Effective(p.EffectiveFrom, p.EffectiveTo))
            .OrderByDescending(p => p.EffectiveFrom).Select(p => p.TypeSurcharge).FirstOrDefault();

        // Phụ thu khung giờ: cộng mọi rule active khớp ngày trong tuần và/hoặc khung giờ.
        var sqlDow = (int)st.DayOfWeek + 1;          // .NET CN=0 -> SQL 1 ... T7=6 -> 7
        var timeOfDay = TimeOnly.FromDateTime(st);
        var timeSurcharge = (await _unitOfWork.PriceTimeConfigs.GetAllAsync(
            predicate: p => p.Status == "Active" && p.EffectiveFrom <= st && (p.EffectiveTo == null || p.EffectiveTo >= st)))
            .Where(p => (p.DayOfWeek == null || p.DayOfWeek == sqlDow)
                     && ((p.StartTime == null && p.EndTime == null)
                         || (p.StartTime != null && p.EndTime != null && timeOfDay >= p.StartTime && timeOfDay <= p.EndTime)))
            .Sum(p => p.TimeSurcharge);

        // Phụ thu theo loại ghế (map seatTypeId -> surcharge).
        var seatSurcharges = (await _unitOfWork.PriceSeatConfigs.GetAllAsync(
            predicate: p => p.Status == "Active" && p.EffectiveFrom <= st && (p.EffectiveTo == null || p.EffectiveTo >= st)))
            .GroupBy(p => p.SeatTypeId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.EffectiveFrom).First().SeatSurcharge);

        // Giá 1 ghế = base + phụ thu phòng + phụ thu giờ + phụ thu loại ghế.
        decimal SeatPrice(Guid seatTypeId) =>
            basePrice + roomSurcharge + timeSurcharge
            + (seatSurcharges.TryGetValue(seatTypeId, out var s2) ? s2 : 0m);

        var rows = seats
            .GroupBy(s => s.RowNumber)
            .OrderBy(g => g.Key)
            .Select(g => new SeatRowViewModel
            {
                RowNumber = g.Key,
                RowLabel = RowLabel(g.Key),
                Seats = g.OrderBy(s => s.SeatNumber).Select(s => new SeatDTO
                {
                    Id = s.Id,
                    RowNumber = s.RowNumber,
                    SeatNumber = s.SeatNumber,
                    RowLabel = RowLabel(s.RowNumber),
                    SeatTypeName = s.SeatType?.Name ?? string.Empty,
                    Price = SeatPrice(s.SeatTypeId),
                    State = s.Status == "Broken" ? "Broken"
                          : bookedSeatIds.Contains(s.Id) ? "Booked"
                          : heldSeatIds.Contains(s.Id) ? "Held"
                          : "Available"
                }).ToList()
            })
            .ToList();

        return new SeatSelectionViewModel
        {
            ShowtimeId = showtime.Id,
            MovieTitle = showtime.Movie.Title,
            MoviePosterUrl = showtime.Movie.PosterUrl,
            AgeRating = showtime.Movie.AgeRating,
            RoomName = showtime.Room.Name,
            CinemaName = showtime.Room.Cinema?.Name ?? string.Empty,
            StartTime = showtime.StartTime,
            EndTime = showtime.EndTime,
            Rows = rows
        };
    }

    // Giữ 1 ghế cho user trong holdMinutes phút (tạo mới hoặc gia hạn nếu đã giữ).
    public async Task<Result> HoldSeatAsync(Guid showtimeId, Guid seatId, Guid userId, int holdMinutes)
    {
        var now = DateTime.Now;

        // Ghế đã có vé (chưa hủy) -> không giữ được.
        var booked = await _unitOfWork.Tickets.ExistsAsync(
            t => t.ShowtimeId == showtimeId && t.SeatId == seatId && t.Status != "Cancelled");
        if (booked) return Result.Failure("Ghế đã được đặt.");

        // Các hold còn hiệu lực của ghế này.
        var holds = (await _unitOfWork.SeatHolds.GetAllAsync(
            predicate: h => h.ShowtimeId == showtimeId && h.SeatId == seatId
                && h.Status == "Holding" && h.ExpiresAt > now)).ToList();

        // Người khác đang giữ -> không giữ được.
        if (holds.Any(h => h.UserId != userId))
            return Result.Failure("Ghế đang được người khác giữ.");

        var mine = holds.FirstOrDefault(h => h.UserId == userId);
        if (mine != null)
        {
            mine.ExpiresAt = now.AddMinutes(holdMinutes);   // gia hạn
            _unitOfWork.SeatHolds.Update(mine);
        }
        else
        {
            await _unitOfWork.SeatHolds.AddAsync(new SeatHold
            {
                Id = Guid.NewGuid(),
                ShowtimeId = showtimeId,
                SeatId = seatId,
                UserId = userId,
                HeldAt = now,
                ExpiresAt = now.AddMinutes(holdMinutes),
                Status = "Holding"
            });
        }

        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    // Bỏ giữ 1 ghế của user.
    public async Task ReleaseSeatAsync(Guid showtimeId, Guid seatId, Guid userId)
    {
        var holds = await _unitOfWork.SeatHolds.GetAllAsync(
            predicate: h => h.ShowtimeId == showtimeId && h.SeatId == seatId
                && h.UserId == userId && h.Status == "Holding");
        foreach (var h in holds)
        {
            h.Status = "Released";
            _unitOfWork.SeatHolds.Update(h);
        }
        await _unitOfWork.SaveChangesAsync();
    }

    // Bỏ giữ toàn bộ ghế user đang giữ trong suất (gọi khi rời trang).
    public async Task ReleaseAllAsync(Guid showtimeId, Guid userId)
    {
        var holds = await _unitOfWork.SeatHolds.GetAllAsync(
            predicate: h => h.ShowtimeId == showtimeId && h.UserId == userId && h.Status == "Holding");
        foreach (var h in holds)
        {
            h.Status = "Released";
            _unitOfWork.SeatHolds.Update(h);
        }
        await _unitOfWork.SaveChangesAsync();
    }

    // Gia hạn thời gian giữ cho toàn bộ ghế user đang giữ (heartbeat khi còn ở trang).
    public async Task ExtendHoldsAsync(Guid showtimeId, Guid userId, int holdMinutes)
    {
        var now = DateTime.Now;
        var holds = await _unitOfWork.SeatHolds.GetAllAsync(
            predicate: h => h.ShowtimeId == showtimeId && h.UserId == userId
                && h.Status == "Holding" && h.ExpiresAt > now);
        foreach (var h in holds)
        {
            h.ExpiresAt = now.AddMinutes(holdMinutes);
            _unitOfWork.SeatHolds.Update(h);
        }
        await _unitOfWork.SaveChangesAsync();
    }

    // Đổi số hàng (1,2,3...) thành nhãn chữ (A,B,C...).
    private static string RowLabel(int rowNumber)
    {
        if (rowNumber < 1) return rowNumber.ToString();
        return ((char)('A' + (rowNumber - 1) % 26)).ToString();
    }
}
