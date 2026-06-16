using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Cinema_System.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cinema_System.Application.Services;

public class BookingService : IBookingService
{
    private const string GuestName = "Khách lẻ";

    private readonly IUnitOfWork _unitOfWork;

    public BookingService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BookingListViewModel> GetBookingsAsync(
        string? search, string? bookingType, string? paymentStatus, int page, int pageSize)
    {
        var bookings = (await _unitOfWork.Bookings.GetAllAsync(
            include: q => q
                .Include(b => b.Showtime).ThenInclude(s => s.Movie)
                .Include(b => b.Showtime).ThenInclude(s => s.Room)
                .Include(b => b.User)
                .Include(b => b.Staff)
                .Include(b => b.Tickets),
            orderBy: q => q.OrderByDescending(b => b.CreatedAt))).ToList();

        var keyword = search?.Trim();
        var filtered = bookings.Where(b =>
            (string.IsNullOrEmpty(keyword)
                || (b.QrCode != null && b.QrCode.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                || (b.User != null && b.User.FullName.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                || (b.User != null && b.User.Phone != null && b.User.Phone.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            && (string.IsNullOrEmpty(bookingType) || b.BookingType == bookingType)
            && (string.IsNullOrEmpty(paymentStatus) || b.PaymentStatus == paymentStatus))
            .ToList();

        var totalCount = filtered.Count;
        var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);
        if (page < 1) page = 1;
        if (page > totalPages) page = totalPages;

        var items = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BookingListItemDTO
            {
                Id = b.Id,
                BookingCode = b.QrCode ?? string.Empty,
                MovieTitle = b.Showtime?.Movie?.Title ?? string.Empty,
                RoomName = b.Showtime?.Room?.Name ?? string.Empty,
                ShowStartTime = b.Showtime?.StartTime ?? default,
                CustomerName = b.User?.FullName ?? GuestName,
                StaffName = b.Staff?.FullName,
                SeatCount = b.Tickets.Count,
                FinalAmount = b.FinalAmount,
                BookingType = b.BookingType ?? string.Empty,
                PaymentStatus = b.PaymentStatus ?? string.Empty,
                CreatedAt = b.CreatedAt
            })
            .ToList();

        return new BookingListViewModel
        {
            Bookings = items,
            Search = search,
            BookingType = bookingType,
            PaymentStatus = paymentStatus,
            CurrentPage = page,
            TotalPages = totalPages,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<BookingDetailDTO?> GetBookingDetailAsync(Guid id)
    {
        var b = await LoadFullBookingAsync(id);
        if (b is null) return null;

        return new BookingDetailDTO
        {
            Id = b.Id,
            BookingCode = b.QrCode ?? string.Empty,
            BookingType = b.BookingType ?? string.Empty,
            PaymentStatus = b.PaymentStatus ?? string.Empty,
            CreatedAt = b.CreatedAt,
            MovieTitle = b.Showtime?.Movie?.Title ?? string.Empty,
            CinemaName = b.Showtime?.Room?.Cinema?.Name ?? string.Empty,
            RoomName = b.Showtime?.Room?.Name ?? string.Empty,
            ShowStartTime = b.Showtime?.StartTime ?? default,
            ShowEndTime = b.Showtime?.EndTime ?? default,
            CustomerName = b.User?.FullName ?? GuestName,
            CustomerPhone = b.User?.Phone,
            StaffName = b.Staff?.FullName,
            TotalAmount = b.TotalAmount,
            DiscountAmount = b.DiscountAmount ?? 0m,
            VatAmount = b.VatAmount ?? 0m,
            FinalAmount = b.FinalAmount,
            Seats = b.Tickets
                .OrderBy(t => t.Seat.RowNumber).ThenBy(t => t.Seat.SeatNumber)
                .Select(t => new BookingSeatLineDTO
                {
                    SeatLabel = SeatLabelHelper.Build(t.Seat.RowNumber, t.Seat.SeatNumber),
                    SeatType = t.Seat.SeatType?.Name ?? string.Empty,
                    Price = t.PriceAtBooking,
                    Status = t.Status ?? string.Empty
                }).ToList(),
            Foods = b.BookingFoods.Select(f => new BookingFoodLineDTO
            {
                Name = f.Fb?.Name ?? string.Empty,
                Quantity = f.Quantity,
                UnitPrice = f.PriceAtBooking
            }).ToList(),
            Payments = b.Payments.Select(p => new BookingPaymentLineDTO
            {
                Method = p.PaymentMethod,
                Source = p.PaymentSource,
                Amount = p.Amount,
                CashReceived = p.CashReceived,
                ChangeAmount = p.ChangeAmount,
                Status = p.Status ?? string.Empty,
                PaidAt = p.PaidAt
            }).ToList()
        };
    }

    public async Task<TicketPrintViewModel?> GetTicketPrintAsync(Guid bookingId)
    {
        var b = await LoadFullBookingAsync(bookingId);
        if (b is null) return null;

        var payment = b.Payments.OrderByDescending(p => p.PaidAt).FirstOrDefault();

        return new TicketPrintViewModel
        {
            BookingId = b.Id,
            BookingCode = b.QrCode ?? string.Empty,
            MovieTitle = b.Showtime?.Movie?.Title ?? string.Empty,
            CinemaName = b.Showtime?.Room?.Cinema?.Name ?? string.Empty,
            RoomName = b.Showtime?.Room?.Name ?? string.Empty,
            ShowStartTime = b.Showtime?.StartTime ?? default,
            ShowEndTime = b.Showtime?.EndTime ?? default,
            CustomerName = b.User?.FullName ?? GuestName,
            CustomerPhone = b.User?.Phone,
            StaffName = b.Staff?.FullName,
            CreatedAt = b.CreatedAt,
            TotalAmount = b.TotalAmount,
            DiscountAmount = b.DiscountAmount ?? 0m,
            VatAmount = b.VatAmount ?? 0m,
            FinalAmount = b.FinalAmount,
            Tickets = b.Tickets
                .OrderBy(t => t.Seat.RowNumber).ThenBy(t => t.Seat.SeatNumber)
                .Select(t => new TicketPrintLine
                {
                    SeatLabel = SeatLabelHelper.Build(t.Seat.RowNumber, t.Seat.SeatNumber),
                    SeatType = t.Seat.SeatType?.Name ?? string.Empty,
                    Price = t.PriceAtBooking,
                    TicketCode = t.QrCode ?? string.Empty
                }).ToList(),
            Foods = b.BookingFoods.Select(f => new BookingFoodLineDTO
            {
                Name = f.Fb?.Name ?? string.Empty,
                Quantity = f.Quantity,
                UnitPrice = f.PriceAtBooking
            }).ToList(),
            Payment = payment is null ? null : new BookingPaymentLineDTO
            {
                Method = payment.PaymentMethod,
                Source = payment.PaymentSource,
                Amount = payment.Amount,
                CashReceived = payment.CashReceived,
                ChangeAmount = payment.ChangeAmount,
                Status = payment.Status ?? string.Empty,
                PaidAt = payment.PaidAt
            }
        };
    }

    private async Task<Booking?> LoadFullBookingAsync(Guid id)
    {
        return await _unitOfWork.Bookings.FirstOrDefaultAsync(
            b => b.Id == id,
            include: q => q
                .Include(b => b.Showtime).ThenInclude(s => s.Movie)
                .Include(b => b.Showtime).ThenInclude(s => s.Room).ThenInclude(r => r.Cinema)
                .Include(b => b.User)
                .Include(b => b.Staff)
                .Include(b => b.Tickets).ThenInclude(t => t.Seat).ThenInclude(s => s.SeatType)
                .Include(b => b.BookingFoods).ThenInclude(f => f.Fb)
                .Include(b => b.Payments));
    }
}
