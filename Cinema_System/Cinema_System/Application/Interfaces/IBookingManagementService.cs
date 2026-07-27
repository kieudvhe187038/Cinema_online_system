using Cinema_System.Application.DTOs;
using Cinema_System.Application.ViewModels;

namespace Cinema_System.Application.Interfaces;

/// <summary>Quản lý đơn đặt vé (Booking Management) + dữ liệu chi tiết / in vé.</summary>
public interface IBookingManagementService
{
    Task<BookingListViewModel> GetBookingsAsync(
        string? search, string? bookingType, string? paymentStatus, int page, int pageSize);

    Task<BookingManagementDetailDTO?> GetBookingDetailAsync(Guid id);

    Task<TicketPrintViewModel?> GetTicketPrintAsync(Guid bookingId);

    /// <summary>
    /// Tìm BookingId theo mã QR (mã đơn <c>BK...</c> hoặc mã vé <c>TK...</c> — tự nhận
    /// diện, thử mã đơn trước). Trả về null nếu không khớp mã nào.
    /// </summary>
    Task<Guid?> FindBookingIdByQrAsync(string? qr);
}
