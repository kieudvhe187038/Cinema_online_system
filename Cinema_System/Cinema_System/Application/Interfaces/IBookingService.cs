using Cinema_System.Application.DTOs;

namespace Cinema_System.Application.Interfaces
{
    // Nghiệp vụ đặt vé (phía khách hàng)
    public interface IBookingService
    {
        // Lấy lịch sử đặt vé của 1 user (mới nhất lên đầu)
        Task<List<BookingHistoryDto>> GetBookingHistoryAsync(Guid userId);

        // Lấy chi tiết 1 đơn (chỉ chủ đơn mới xem được)
        Task<BookingDetailDto?> GetBookingDetailAsync(Guid bookingId, Guid userId);
    }
}
