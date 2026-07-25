namespace Cinema_System.Application.Common;

/// <summary>
/// Chính sách điểm thưởng dùng CHUNG cho toàn hệ thống (đặt vé online, bán vé tại quầy, hoàn điểm
/// do sự cố). Lấy từ SystemConfig qua <see cref="Interfaces.IPointConfigService"/> — không hardcode.
/// </summary>
/// <param name="EarnRate">Số điểm cộng cho mỗi 1₫ chi tiêu (vd 0.0001 = 10.000₫ được 1 điểm).</param>
/// <param name="PointValueVnd">Giá trị quy đổi của 1 điểm khi dùng để giảm giá (₫).</param>
public readonly record struct PointPolicy(decimal EarnRate, int PointValueVnd)
{
    /// <summary>Số điểm được cộng khi thanh toán <paramref name="amount"/> ₫ (làm tròn xuống).</summary>
    public int PointsEarnedFor(decimal amount)
        => EarnRate <= 0 ? 0 : (int)Math.Floor(amount * EarnRate);

    /// <summary>Số tiền (₫) tương ứng với <paramref name="points"/> điểm khi đem đi giảm giá.</summary>
    public decimal DiscountFor(int points) => (decimal)points * PointValueVnd;

    /// <summary>Số điểm tối đa dùng được để không vượt quá số tiền còn phải trả.</summary>
    public int MaxUsablePoints(int availablePoints, decimal amountDue)
        => PointValueVnd <= 0 ? 0 : Math.Max(0, Math.Min(availablePoints, (int)(amountDue / PointValueVnd)));
}
