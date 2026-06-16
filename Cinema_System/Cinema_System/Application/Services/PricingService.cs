using Cinema_System.Application.Interfaces;
using Cinema_System.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cinema_System.Application.Services;

public class PricingService : IPricingService
{
    private const string StatusActive = "Active";

    private readonly IUnitOfWork _unitOfWork;

    public PricingService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Tính giá cho một suất chiếu tại thời điểm <see cref="Showtime.StartTime"/>:
    /// phần giá chung (giá cơ bản + phụ thu loại phòng + phụ thu thời gian) và
    /// bảng phụ thu theo loại ghế. Chỉ xét các cấu hình Active còn hiệu lực.
    /// </summary>
    public async Task<SeatPricingResult> GetPricingAsync(Showtime showtime)
    {
        var when = showtime.StartTime;

        // Phòng + loại phòng của suất chiếu
        var room = await _unitOfWork.Rooms.FirstOrDefaultAsync(
            r => r.Id == showtime.RoomId,
            include: q => q.Include(r => r.RoomType));

        // --- Giá cơ bản theo phim (ưu tiên cấu hình theo phim, sau đó cấu hình chung) ---
        var baseConfigs = await _unitOfWork.PriceBaseConfigs.GetAllAsync(
            c => c.Status == StatusActive);
        var basePrice = baseConfigs
            .Where(c => IsEffective(c.EffectiveFrom, c.EffectiveTo, when)
                        && (c.MovieId == showtime.MovieId || c.MovieId == null))
            .OrderByDescending(c => c.MovieId == showtime.MovieId) // cấu hình riêng cho phim được ưu tiên
            .ThenByDescending(c => c.EffectiveFrom)
            .Select(c => c.BasePrice)
            .FirstOrDefault();

        // --- Phụ thu theo loại phòng ---
        decimal roomSurcharge = 0m;
        if (room is not null)
        {
            var roomConfigs = await _unitOfWork.PriceRoomTypeConfigs.GetAllAsync(
                c => c.Status == StatusActive && c.RoomTypeId == room.RoomTypeId);
            roomSurcharge = roomConfigs
                .Where(c => IsEffective(c.EffectiveFrom, c.EffectiveTo, when))
                .OrderByDescending(c => c.EffectiveFrom)
                .Select(c => c.TypeSurcharge)
                .FirstOrDefault();
        }

        // --- Phụ thu theo thời gian (best-effort theo schema, lấy quy tắc ưu tiên cao nhất khớp) ---
        var timeConfigs = await _unitOfWork.PriceTimeConfigs.GetAllAsync(
            c => c.Status == StatusActive);
        var timeSurcharge = timeConfigs
            .Where(c => IsEffective(c.EffectiveFrom, c.EffectiveTo, when) && MatchesTime(c, when))
            .OrderByDescending(c => c.Priority)
            .Select(c => c.TimeSurcharge)
            .FirstOrDefault();

        // --- Phụ thu theo loại ghế ---
        var seatConfigs = await _unitOfWork.PriceSeatConfigs.GetAllAsync(
            c => c.Status == StatusActive);
        var seatSurcharge = seatConfigs
            .Where(c => IsEffective(c.EffectiveFrom, c.EffectiveTo, when))
            .GroupBy(c => c.SeatTypeId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(c => c.EffectiveFrom).First().SeatSurcharge);

        return new SeatPricingResult
        {
            CommonAmount = basePrice + roomSurcharge + timeSurcharge,
            SeatTypeSurcharge = seatSurcharge
        };
    }

    private static bool IsEffective(DateTime from, DateTime? to, DateTime when)
        => from <= when && (to is null || to >= when);

    /// <summary>
    /// So khớp quy tắc giá theo thời gian với thời điểm suất chiếu.
    /// Hỗ trợ: ngày cụ thể, thứ trong tuần, khoảng giờ. Nếu quy tắc không khai báo
    /// điều kiện nào (config "mặc định") thì coi như khớp.
    /// </summary>
    private static bool MatchesTime(PriceTimeConfig c, DateTime when)
    {
        var hasCondition = false;

        if (c.SpecificDate is not null)
        {
            hasCondition = true;
            if (c.SpecificDate.Value == DateOnly.FromDateTime(when)) return true;
        }

        if (c.DayOfWeek is not null)
        {
            hasCondition = true;
            if (c.DayOfWeek.Value == (int)when.DayOfWeek) return true;
        }

        if (c.StartTime is not null && c.EndTime is not null)
        {
            hasCondition = true;
            var t = TimeOnly.FromDateTime(when);
            if (t >= c.StartTime.Value && t < c.EndTime.Value) return true;
        }

        return !hasCondition;
    }
}
