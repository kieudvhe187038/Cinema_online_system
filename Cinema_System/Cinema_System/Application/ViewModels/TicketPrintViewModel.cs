using Cinema_System.Application.DTOs;

namespace Cinema_System.Application.ViewModels;

/// <summary>Dữ liệu in vé tại quầy (Ticket Printing - giả lập PDF qua print).</summary>
public class TicketPrintViewModel
{
    public Guid BookingId { get; set; }

    public string BookingCode { get; set; } = string.Empty;

    public string MovieTitle { get; set; } = string.Empty;

    public string CinemaName { get; set; } = string.Empty;

    public string RoomName { get; set; } = string.Empty;

    public DateTime ShowStartTime { get; set; }

    public DateTime ShowEndTime { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string? CustomerPhone { get; set; }

    public string? StaffName { get; set; }

    public DateTime? CreatedAt { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal VatAmount { get; set; }

    public decimal FinalAmount { get; set; }

    public List<TicketPrintLine> Tickets { get; set; } = new();

    public List<BookingManagementFoodLineDTO> Foods { get; set; } = new();

    public BookingManagementPaymentLineDTO? Payment { get; set; }
}

public class TicketPrintLine
{
    public string SeatLabel { get; set; } = string.Empty;

    public string SeatType { get; set; } = string.Empty;

    public decimal Price { get; set; }

    /// <summary>Mã vé (Ticket.QrCode) để in mã/QR.</summary>
    public string TicketCode { get; set; } = string.Empty;
}
