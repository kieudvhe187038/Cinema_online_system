using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Interfaces;

/// <summary>
/// Repository chuyên biệt cho <see cref="Booking"/> — chứa các truy vấn kèm
/// nhiều quan hệ (suất chiếu, vé, đồ ăn, thanh toán) và phân trang tại DB.
/// </summary>
public interface IBookingRepository : IGenericRepository<Booking>
{
    /// <summary>
    /// Một trang đơn đặt vé (kèm phim/phòng/khách/nhân viên/vé) đã lọc theo loại đơn,
    /// trạng thái thanh toán và từ khóa (mã đơn / tên / SĐT). Lọc &amp; phân trang tại SQL.
    /// </summary>
    Task<(IReadOnlyList<Booking> Items, int TotalCount)> GetPagedListAsync(
        string? bookingType, string? paymentStatus, string? keyword, int page, int pageSize);

    /// <summary>Lấy đầy đủ một đơn (suất chiếu, vé+ghế+loại ghế, đồ ăn, thanh toán).</summary>
    Task<Booking?> GetDetailByIdAsync(Guid id);
}
