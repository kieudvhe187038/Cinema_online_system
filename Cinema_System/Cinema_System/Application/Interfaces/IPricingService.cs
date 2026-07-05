using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Interfaces;

/// <summary>
/// Tính giá vé cho một suất chiếu dựa trên các cấu hình giá đang hiệu lực:
/// giá cơ bản theo phim + phụ thu loại phòng + phụ thu thời gian + phụ thu loại ghế.
/// (Bản tạm cho luồng bán vé tại quầy; sẽ thay bằng Pricing engine đầy đủ khi có.)
/// </summary>
public interface IPricingService
{
    Task<SeatPricingResult> GetPricingAsync(Showtime showtime);
}

/// <summary>Kết quả tính giá: phần dùng chung + phụ thu theo loại ghế.</summary>
public class SeatPricingResult
{
    /// <summary>Giá cơ bản + phụ thu phòng + phụ thu thời gian (chung cho mọi ghế).</summary>
    public decimal CommonAmount { get; init; }

    /// <summary>Phụ thu theo từng loại ghế (key = SeatTypeId).</summary>
    public IReadOnlyDictionary<Guid, decimal> SeatTypeSurcharge { get; init; }
        = new Dictionary<Guid, decimal>();

    public decimal PriceForSeatType(Guid seatTypeId)
        => CommonAmount + (SeatTypeSurcharge.TryGetValue(seatTypeId, out var s) ? s : 0m);
}
