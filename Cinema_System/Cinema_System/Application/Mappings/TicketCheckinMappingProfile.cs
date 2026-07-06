using AutoMapper;
using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Mappings;

// Quy tắc map cho module check-in vé tại quầy (Staff).
public class TicketCheckinMappingProfile : Profile
{
    private const string GuestName = "Khách lẻ";

    public TicketCheckinMappingProfile()
    {
        CreateMap<Ticket, TicketCheckinDTO>()
            .ForMember(d => d.TicketId, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.TicketCode, o => o.MapFrom(s => s.QrCode ?? string.Empty))
            .ForMember(d => d.BookingCode, o => o.MapFrom(s => s.Booking != null ? s.Booking.QrCode ?? string.Empty : string.Empty))
            .ForMember(d => d.MovieTitle, o => o.MapFrom(s => s.Showtime != null && s.Showtime.Movie != null ? s.Showtime.Movie.Title : string.Empty))
            .ForMember(d => d.CinemaName, o => o.MapFrom(s => s.Showtime != null && s.Showtime.Room != null && s.Showtime.Room.Cinema != null ? s.Showtime.Room.Cinema.Name : string.Empty))
            .ForMember(d => d.RoomName, o => o.MapFrom(s => s.Showtime != null && s.Showtime.Room != null ? s.Showtime.Room.Name : string.Empty))
            .ForMember(d => d.ShowStartTime, o => o.MapFrom(s => s.Showtime != null ? s.Showtime.StartTime : default))
            .ForMember(d => d.ShowEndTime, o => o.MapFrom(s => s.Showtime != null ? s.Showtime.EndTime : default))
            .ForMember(d => d.SeatLabel, o => o.MapFrom(s => s.Seat == null ? string.Empty : SeatLabelHelper.Build(s.Seat.RowNumber, s.Seat.SeatNumber)))
            .ForMember(d => d.SeatType, o => o.MapFrom(s => s.Seat != null && s.Seat.SeatType != null ? s.Seat.SeatType.Name : string.Empty))
            .ForMember(d => d.CustomerName, o => o.MapFrom(s => s.Booking != null && s.Booking.User != null ? s.Booking.User.FullName : GuestName))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status ?? string.Empty))
            .ForMember(d => d.ScanByName, o => o.MapFrom(s => s.ScanByNavigation != null ? s.ScanByNavigation.FullName : null))
            // Tính theo hằng số trạng thái riêng của Service (StatusCheckedIn) -> gán sau khi map.
            .ForMember(d => d.CheckedIn, o => o.Ignore())
            // Gán sau khi map: phụ thuộc ngữ cảnh gọi (lookup / check-in vừa xong).
            .ForMember(d => d.Message, o => o.Ignore());

        CreateMap<Booking, BookingCheckinDTO>()
            .ForMember(d => d.BookingId, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.BookingCode, o => o.MapFrom(s => s.QrCode ?? string.Empty))
            .ForMember(d => d.MovieTitle, o => o.MapFrom(s => s.Showtime != null && s.Showtime.Movie != null ? s.Showtime.Movie.Title : string.Empty))
            .ForMember(d => d.CinemaName, o => o.MapFrom(s => s.Showtime != null && s.Showtime.Room != null && s.Showtime.Room.Cinema != null ? s.Showtime.Room.Cinema.Name : string.Empty))
            .ForMember(d => d.RoomName, o => o.MapFrom(s => s.Showtime != null && s.Showtime.Room != null ? s.Showtime.Room.Name : string.Empty))
            .ForMember(d => d.ShowStartTime, o => o.MapFrom(s => s.Showtime != null ? s.Showtime.StartTime : default))
            .ForMember(d => d.ShowEndTime, o => o.MapFrom(s => s.Showtime != null ? s.Showtime.EndTime : default))
            .ForMember(d => d.CustomerName, o => o.MapFrom(s => s.User != null ? s.User.FullName : GuestName))
            .ForMember(d => d.PaymentStatus, o => o.MapFrom(s => s.PaymentStatus ?? string.Empty))
            // Danh sách vé + số liệu tổng hợp + thông báo: dựng riêng trong Service (lọc/sắp xếp/đếm theo ngữ cảnh).
            .ForMember(d => d.Tickets, o => o.Ignore())
            .ForMember(d => d.TotalTickets, o => o.Ignore())
            .ForMember(d => d.CheckedInCount, o => o.Ignore())
            .ForMember(d => d.NewlyCheckedIn, o => o.Ignore())
            .ForMember(d => d.Message, o => o.Ignore());
    }
}
