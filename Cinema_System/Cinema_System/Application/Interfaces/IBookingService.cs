using Cinema_System.Application.DTOs;
using Cinema_System.Application.ViewModels;

namespace Cinema_System.Application.Interfaces;

/// <summary>Quản lý đơn đặt vé (Booking Management) + dữ liệu chi tiết / in vé.</summary>
public interface IBookingService
{
    Task<BookingListViewModel> GetBookingsAsync(
        string? search, string? bookingType, string? paymentStatus, int page, int pageSize);

    Task<BookingDetailDTO?> GetBookingDetailAsync(Guid id);

    Task<TicketPrintViewModel?> GetTicketPrintAsync(Guid bookingId);
}
