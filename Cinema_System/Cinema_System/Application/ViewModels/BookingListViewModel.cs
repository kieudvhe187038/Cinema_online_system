using Cinema_System.Application.DTOs;

namespace Cinema_System.Application.ViewModels;

/// <summary>Trang Quản lý đơn hàng (Booking Management) - có lọc & phân trang.</summary>
public class BookingListViewModel
{
    public IEnumerable<BookingListItemDTO> Bookings { get; set; } = new List<BookingListItemDTO>();

    public string? Search { get; set; }

    /// <summary>Lọc theo loại đơn: Online / Offline / null = tất cả.</summary>
    public string? BookingType { get; set; }

    /// <summary>Lọc theo trạng thái thanh toán.</summary>
    public string? PaymentStatus { get; set; }

    public int CurrentPage { get; set; } = 1;

    public int TotalPages { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public int TotalCount { get; set; }
}
