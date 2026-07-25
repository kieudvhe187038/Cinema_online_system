using AutoMapper;
using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.ViewModels;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Mappings;

// Quy tắc map cho module quản lý đơn đặt vé (Manager/Staff).
public class BookingManagementMappingProfile : Profile
{
    private const string GuestName = "Khách lẻ";

    public BookingManagementMappingProfile()
    {
        CreateMap<Booking, BookingListItemDTO>()
            .ForMember(d => d.BookingCode, o => o.MapFrom(s => s.QrCode ?? string.Empty))
            .ForMember(d => d.MovieTitle, o => o.MapFrom(s => s.Showtime.Movie.Title))
            .ForMember(d => d.RoomName, o => o.MapFrom(s => s.Showtime.Room.Name))
            .ForMember(d => d.ShowStartTime, o => o.MapFrom(s => s.Showtime.StartTime))
            .ForMember(d => d.CustomerName, o => o.MapFrom(s => s.User != null ? s.User.FullName : GuestName))
            .ForMember(d => d.StaffName, o => o.MapFrom(s => s.Staff != null ? s.Staff.FullName : null))
            .ForMember(d => d.SeatCount, o => o.MapFrom(s => s.Tickets.Count))
            .ForMember(d => d.BookingType, o => o.MapFrom(s => s.BookingType ?? string.Empty))
            .ForMember(d => d.PaymentStatus, o => o.MapFrom(s => s.PaymentStatus ?? string.Empty));

        CreateMap<Booking, BookingManagementDetailDTO>()
            .ForMember(d => d.BookingCode, o => o.MapFrom(s => s.QrCode ?? string.Empty))
            .ForMember(d => d.BookingType, o => o.MapFrom(s => s.BookingType ?? string.Empty))
            .ForMember(d => d.PaymentStatus, o => o.MapFrom(s => s.PaymentStatus ?? string.Empty))
            .ForMember(d => d.MovieTitle, o => o.MapFrom(s => s.Showtime.Movie.Title))
            .ForMember(d => d.CinemaName, o => o.MapFrom(s => s.Showtime.Room.Cinema != null ? s.Showtime.Room.Cinema.Name : string.Empty))
            .ForMember(d => d.RoomName, o => o.MapFrom(s => s.Showtime.Room.Name))
            .ForMember(d => d.ShowStartTime, o => o.MapFrom(s => s.Showtime.StartTime))
            .ForMember(d => d.ShowEndTime, o => o.MapFrom(s => s.Showtime.EndTime))
            .ForMember(d => d.CustomerName, o => o.MapFrom(s => s.User != null ? s.User.FullName : GuestName))
            .ForMember(d => d.CustomerPhone, o => o.MapFrom(s => s.User != null ? s.User.Phone : null))
            .ForMember(d => d.StaffName, o => o.MapFrom(s => s.Staff != null ? s.Staff.FullName : null))
            .ForMember(d => d.DiscountAmount, o => o.MapFrom(s => s.DiscountAmount ?? 0m))
            .ForMember(d => d.VatAmount, o => o.MapFrom(s => s.VatAmount ?? 0m))
            .ForMember(d => d.Seats, o => o.MapFrom(s => s.Tickets.OrderBy(t => t.Seat.RowNumber).ThenBy(t => t.Seat.SeatNumber)))
            .ForMember(d => d.Foods, o => o.MapFrom(s => s.BookingFoods))
            .ForMember(d => d.Payments, o => o.MapFrom(s => s.Payments));

        CreateMap<Booking, TicketPrintViewModel>()
            .ForMember(d => d.BookingId, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.BookingCode, o => o.MapFrom(s => s.QrCode ?? string.Empty))
            .ForMember(d => d.MovieTitle, o => o.MapFrom(s => s.Showtime.Movie.Title))
            .ForMember(d => d.CinemaName, o => o.MapFrom(s => s.Showtime.Room.Cinema != null ? s.Showtime.Room.Cinema.Name : string.Empty))
            .ForMember(d => d.RoomName, o => o.MapFrom(s => s.Showtime.Room.Name))
            .ForMember(d => d.ShowStartTime, o => o.MapFrom(s => s.Showtime.StartTime))
            .ForMember(d => d.ShowEndTime, o => o.MapFrom(s => s.Showtime.EndTime))
            .ForMember(d => d.CustomerName, o => o.MapFrom(s => s.User != null ? s.User.FullName : GuestName))
            .ForMember(d => d.CustomerPhone, o => o.MapFrom(s => s.User != null ? s.User.Phone : null))
            .ForMember(d => d.StaffName, o => o.MapFrom(s => s.Staff != null ? s.Staff.FullName : null))
            .ForMember(d => d.DiscountAmount, o => o.MapFrom(s => s.DiscountAmount ?? 0m))
            .ForMember(d => d.VatAmount, o => o.MapFrom(s => s.VatAmount ?? 0m))
            .ForMember(d => d.Tickets, o => o.MapFrom(s => s.Tickets.OrderBy(t => t.Seat.RowNumber).ThenBy(t => t.Seat.SeatNumber)))
            .ForMember(d => d.Foods, o => o.MapFrom(s => s.BookingFoods))
            .ForMember(d => d.Payment, o => o.MapFrom(s => s.Payments.OrderByDescending(p => p.PaidAt).FirstOrDefault()));

        CreateMap<Ticket, BookingManagementSeatLineDTO>()
            .ForMember(d => d.SeatLabel, o => o.MapFrom(s => SeatLabelHelper.Build(s.Seat.RowNumber, s.Seat.SeatNumber)))
            .ForMember(d => d.SeatType, o => o.MapFrom(s => s.Seat.SeatType != null ? s.Seat.SeatType.Name : string.Empty))
            .ForMember(d => d.Price, o => o.MapFrom(s => s.PriceAtBooking))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status ?? string.Empty));

        CreateMap<Ticket, TicketPrintLine>()
            .ForMember(d => d.SeatLabel, o => o.MapFrom(s => SeatLabelHelper.Build(s.Seat.RowNumber, s.Seat.SeatNumber)))
            .ForMember(d => d.SeatType, o => o.MapFrom(s => s.Seat.SeatType != null ? s.Seat.SeatType.Name : string.Empty))
            .ForMember(d => d.Price, o => o.MapFrom(s => s.PriceAtBooking))
            .ForMember(d => d.TicketCode, o => o.MapFrom(s => s.QrCode ?? string.Empty));

        CreateMap<BookingFood, BookingManagementFoodLineDTO>()
            .ForMember(d => d.Name, o => o.MapFrom(s => s.Fb != null ? s.Fb.Name : string.Empty))
            .ForMember(d => d.UnitPrice, o => o.MapFrom(s => s.PriceAtBooking));

        CreateMap<Payment, BookingManagementPaymentLineDTO>()
            .ForMember(d => d.Method, o => o.MapFrom(s => s.PaymentMethod))
            .ForMember(d => d.Source, o => o.MapFrom(s => s.PaymentSource))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status ?? string.Empty));
    }
}
