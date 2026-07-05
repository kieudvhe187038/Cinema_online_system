using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;
using Cinema_System.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cinema_System.Application.Services;

public class TicketCheckinService : ITicketCheckinService
{
    private const string StatusCancelled = "Cancelled";
    private const string StatusCheckedIn = "Checked-in";
    private const string PaymentPaid = "Paid";

    private readonly IUnitOfWork _unitOfWork;
    private readonly IStaffContextService _staffContext;

    public TicketCheckinService(IUnitOfWork unitOfWork, IStaffContextService staffContext)
    {
        _unitOfWork = unitOfWork;
        _staffContext = staffContext;
    }

    public async Task<TicketCheckinDTO?> LookupAsync(string? qrCode)
    {
        var ticket = await FindByQrAsync(qrCode);
        if (ticket is null) return null;

        var dto = MapToDTO(ticket);
        dto.Message = DescribeState(ticket);
        return dto;
    }

    public async Task<Result<TicketCheckinDTO>> CheckinAsync(string? qrCode, Guid staffId)
    {
        var staff = await _staffContext.GetCurrentStaffAsync(staffId);
        if (staff is null)
            return Result<TicketCheckinDTO>.Failure("Chưa có tài khoản nhân viên (Staff) để check-in.");

        var ticket = await FindByQrAsync(qrCode);
        if (ticket is null)
            return Result<TicketCheckinDTO>.Failure("Không tìm thấy vé với mã này.");

        // --- Các trạng thái không thể check-in ---
        if (string.Equals(ticket.Status, StatusCancelled, StringComparison.OrdinalIgnoreCase))
            return Result<TicketCheckinDTO>.Failure("Vé đã bị hủy, không thể check-in.");

        if (string.Equals(ticket.Status, StatusCheckedIn, StringComparison.OrdinalIgnoreCase))
            return Result<TicketCheckinDTO>.Failure(DescribeState(ticket));

        if (!string.Equals(ticket.Booking?.PaymentStatus, PaymentPaid, StringComparison.OrdinalIgnoreCase))
            return Result<TicketCheckinDTO>.Failure("Đơn chưa thanh toán, không thể check-in.");

        if (ticket.Showtime is not null && ticket.Showtime.EndTime < DateTime.Now)
            return Result<TicketCheckinDTO>.Failure("Suất chiếu đã kết thúc, không thể check-in.");

        // --- Check-in ---
        ticket.Status = StatusCheckedIn;
        ticket.ScanBy = staff.Id;
        ticket.ScannedAt = DateTime.Now;
        _unitOfWork.Tickets.Update(ticket);
        await _unitOfWork.SaveChangesAsync();

        var dto = MapToDTO(ticket);
        dto.ScanByName = staff.FullName;
        dto.Message = $"Check-in thành công lúc {ticket.ScannedAt:HH:mm dd/MM/yyyy}.";
        return Result<TicketCheckinDTO>.Success(dto);
    }

    public async Task<BookingCheckinDTO?> LookupBookingAsync(string? qrCode)
    {
        var booking = await FindBookingByQrAsync(qrCode);
        return booking is null ? null : MapBookingToDTO(booking);
    }

    public async Task<Result<BookingCheckinDTO>> CheckinBookingAsync(string? qrCode, Guid staffId)
    {
        var staff = await _staffContext.GetCurrentStaffAsync(staffId);
        if (staff is null)
            return Result<BookingCheckinDTO>.Failure("Chưa có tài khoản nhân viên (Staff) để check-in.");

        var booking = await FindBookingByQrAsync(qrCode);
        if (booking is null)
            return Result<BookingCheckinDTO>.Failure("Không tìm thấy đơn với mã này.");

        if (!string.Equals(booking.PaymentStatus, PaymentPaid, StringComparison.OrdinalIgnoreCase))
            return Result<BookingCheckinDTO>.Failure("Đơn chưa thanh toán, không thể check-in.");

        // Vé có thể check-in = chưa hủy & chưa check-in.
        var pending = booking.Tickets.Where(IsCheckinable).ToList();
        if (pending.Count == 0)
            return Result<BookingCheckinDTO>.Failure("Tất cả vé của đơn đã được check-in (hoặc không còn vé hợp lệ).");

        if (booking.Showtime is not null && booking.Showtime.EndTime < DateTime.Now)
            return Result<BookingCheckinDTO>.Failure("Suất chiếu đã kết thúc, không thể check-in.");

        var now = DateTime.Now;
        foreach (var ticket in pending)
        {
            ticket.Status = StatusCheckedIn;
            ticket.ScanBy = staff.Id;
            ticket.ScannedAt = now;
            ticket.ScanByNavigation = staff; // để hiển thị tên người quét ngay
            _unitOfWork.Tickets.Update(ticket);
        }
        await _unitOfWork.SaveChangesAsync();

        var dto = MapBookingToDTO(booking);
        dto.NewlyCheckedIn = pending.Count;
        dto.Message = $"Đã check-in {pending.Count} vé. Tổng {dto.CheckedInCount}/{dto.TotalTickets} vé đã check-in.";
        return Result<BookingCheckinDTO>.Success(dto);
    }

    private static bool IsCheckinable(Ticket t) =>
        !string.Equals(t.Status, StatusCancelled, StringComparison.OrdinalIgnoreCase)
        && !string.Equals(t.Status, StatusCheckedIn, StringComparison.OrdinalIgnoreCase);

    /// <summary>Tìm đơn theo mã QR (Booking.QrCode), kèm vé + các navigation để hiển thị.</summary>
    private async Task<Booking?> FindBookingByQrAsync(string? qrCode)
    {
        if (string.IsNullOrWhiteSpace(qrCode)) return null;
        var code = qrCode.Trim();

        return await _unitOfWork.Bookings.FirstOrDefaultAsync(
            b => b.QrCode == code,
            include: q => q
                .Include(b => b.User)
                .Include(b => b.Showtime).ThenInclude(s => s.Movie)
                .Include(b => b.Showtime).ThenInclude(s => s.Room).ThenInclude(r => r.Cinema)
                .Include(b => b.Tickets).ThenInclude(t => t.Seat).ThenInclude(s => s.SeatType)
                .Include(b => b.Tickets).ThenInclude(t => t.ScanByNavigation));
    }

    private BookingCheckinDTO MapBookingToDTO(Booking b)
    {
        // Lấy thông tin suất/phim từ đơn rồi gắn vào từng dòng vé (vé cùng đơn chung suất).
        var lines = b.Tickets
            .Where(t => !string.Equals(t.Status, StatusCancelled, StringComparison.OrdinalIgnoreCase))
            .OrderBy(t => t.Seat == null ? "" : SeatLabelHelper.Build(t.Seat.RowNumber, t.Seat.SeatNumber))
            .Select(t =>
            {
                var line = MapToDTO(t);
                line.MovieTitle = b.Showtime?.Movie?.Title ?? string.Empty;
                line.CinemaName = b.Showtime?.Room?.Cinema?.Name ?? string.Empty;
                line.RoomName = b.Showtime?.Room?.Name ?? string.Empty;
                line.ShowStartTime = b.Showtime?.StartTime ?? default;
                line.ShowEndTime = b.Showtime?.EndTime ?? default;
                line.BookingCode = b.QrCode ?? string.Empty;
                line.CustomerName = b.User?.FullName ?? "Khách lẻ";
                return line;
            })
            .ToList();

        var checkedIn = lines.Count(l => l.CheckedIn);
        var paid = string.Equals(b.PaymentStatus, PaymentPaid, StringComparison.OrdinalIgnoreCase);
        var showEnded = b.Showtime is not null && b.Showtime.EndTime < DateTime.Now;

        string message;
        if (!paid)
            message = "Đơn chưa thanh toán.";
        else if (lines.Count == 0)
            message = "Đơn không có vé hợp lệ.";
        else if (checkedIn == lines.Count)
            message = "Tất cả vé của đơn đã được check-in.";
        else if (showEnded)
            message = "Suất chiếu đã kết thúc.";
        else
            message = $"Đơn có {lines.Count} vé, {checkedIn} đã check-in, {lines.Count - checkedIn} chưa.";

        return new BookingCheckinDTO
        {
            BookingId = b.Id,
            BookingCode = b.QrCode ?? string.Empty,
            MovieTitle = b.Showtime?.Movie?.Title ?? string.Empty,
            CinemaName = b.Showtime?.Room?.Cinema?.Name ?? string.Empty,
            RoomName = b.Showtime?.Room?.Name ?? string.Empty,
            ShowStartTime = b.Showtime?.StartTime ?? default,
            ShowEndTime = b.Showtime?.EndTime ?? default,
            CustomerName = b.User?.FullName ?? "Khách lẻ",
            PaymentStatus = b.PaymentStatus ?? string.Empty,
            Tickets = lines,
            TotalTickets = lines.Count,
            CheckedInCount = checkedIn,
            Message = message
        };
    }

    /// <summary>Tìm vé theo mã QR (Ticket.QrCode), kèm các navigation cần để hiển thị.</summary>
    private async Task<Ticket?> FindByQrAsync(string? qrCode)
    {
        if (string.IsNullOrWhiteSpace(qrCode)) return null;
        var code = qrCode.Trim();

        return await _unitOfWork.Tickets.FirstOrDefaultAsync(
            t => t.QrCode == code,
            include: q => q
                .Include(t => t.Booking).ThenInclude(b => b.User)
                .Include(t => t.Showtime).ThenInclude(s => s.Movie)
                .Include(t => t.Showtime).ThenInclude(s => s.Room).ThenInclude(r => r.Cinema)
                .Include(t => t.Seat).ThenInclude(s => s.SeatType)
                .Include(t => t.ScanByNavigation));
    }

    private static TicketCheckinDTO MapToDTO(Ticket t) => new()
    {
        TicketId = t.Id,
        TicketCode = t.QrCode ?? string.Empty,
        BookingCode = t.Booking?.QrCode ?? string.Empty,
        MovieTitle = t.Showtime?.Movie?.Title ?? string.Empty,
        CinemaName = t.Showtime?.Room?.Cinema?.Name ?? string.Empty,
        RoomName = t.Showtime?.Room?.Name ?? string.Empty,
        ShowStartTime = t.Showtime?.StartTime ?? default,
        ShowEndTime = t.Showtime?.EndTime ?? default,
        SeatLabel = t.Seat is null ? string.Empty : SeatLabelHelper.Build(t.Seat.RowNumber, t.Seat.SeatNumber),
        SeatType = t.Seat?.SeatType?.Name ?? string.Empty,
        CustomerName = t.Booking?.User?.FullName ?? "Khách lẻ",
        Status = t.Status ?? string.Empty,
        CheckedIn = string.Equals(t.Status, StatusCheckedIn, StringComparison.OrdinalIgnoreCase),
        ScannedAt = t.ScannedAt,
        ScanByName = t.ScanByNavigation?.FullName
    };

    /// <summary>Mô tả trạng thái hiện tại của vé (dùng cho lookup / thông báo lỗi).</summary>
    private static string DescribeState(Ticket t)
    {
        if (string.Equals(t.Status, StatusCancelled, StringComparison.OrdinalIgnoreCase))
            return "Vé đã bị hủy.";

        if (string.Equals(t.Status, StatusCheckedIn, StringComparison.OrdinalIgnoreCase))
        {
            var who = t.ScanByNavigation?.FullName;
            var at = t.ScannedAt is null ? "" : $" lúc {t.ScannedAt:HH:mm dd/MM/yyyy}";
            var byTxt = string.IsNullOrEmpty(who) ? "" : $" bởi {who}";
            return $"Vé đã được check-in{at}{byTxt}.";
        }

        if (!string.Equals(t.Booking?.PaymentStatus, PaymentPaid, StringComparison.OrdinalIgnoreCase))
            return "Đơn chưa thanh toán.";

        if (t.Showtime is not null && t.Showtime.EndTime < DateTime.Now)
            return "Suất chiếu đã kết thúc.";

        return "Vé hợp lệ, sẵn sàng check-in.";
    }
}
