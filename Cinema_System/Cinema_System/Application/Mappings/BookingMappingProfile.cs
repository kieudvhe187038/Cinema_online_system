using AutoMapper;
using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.ViewModels;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Mappings
{
    // Quy tắc map cho module đặt vé.
    public class BookingMappingProfile : Profile
    {
        public BookingMappingProfile() {
            CreateMap<Booking, BookingHistoryDto>()
                .ForMember(d => d.MovieTitle, o => o.MapFrom(s => s.Showtime.Movie.Title))
                .ForMember(d => d.PosterUrl, o => o.MapFrom(s => s.Showtime.Movie.PosterUrl))
                .ForMember(d => d.StartTime, o => o.MapFrom(s => s.Showtime.StartTime))
                .ForMember(d => d.SeatCount, o => o.MapFrom(s => s.Tickets.Count));
            CreateMap<BookingHistoryDto, BookingHistoryViewModel>();

            CreateMap<Booking, BookingDetailDto>()
                .ForMember(d => d.MovieTitle, o => o.MapFrom(s => s.Showtime.Movie.Title))
                .ForMember(d => d.PosterUrl, o => o.MapFrom(s => s.Showtime.Movie.PosterUrl))
                .ForMember(d => d.AgeRating, o => o.MapFrom(s => s.Showtime.Movie.AgeRating))
                .ForMember(d => d.DurationMinutes, o => o.MapFrom(s => s.Showtime.Movie.DurationMinutes))
                .ForMember(d => d.StartTime, o => o.MapFrom(s => s.Showtime.StartTime))
                .ForMember(d => d.EndTime, o => o.MapFrom(s => s.Showtime.EndTime))
                .ForMember(d => d.RoomName, o => o.MapFrom(s => s.Showtime.Room.Name))
                .ForMember(d => d.Seats, o => o.MapFrom(s => s.Tickets.OrderBy(t => t.Seat.RowNumber).ThenBy(t => t.Seat.SeatNumber)))
                .ForMember(d => d.Foods, o => o.MapFrom(s => s.BookingFoods));
            CreateMap<BookingDetailDto, BookingDetailViewModel>();

            CreateMap<Ticket, BookingSeatLineDto>()
                .ForMember(d => d.SeatLabel, o => o.MapFrom(s => SeatLabelHelper.Build(s.Seat.RowNumber, s.Seat.SeatNumber)))
                .ForMember(d => d.SeatTypeName, o => o.MapFrom(s => s.Seat.SeatType.Name))
                .ForMember(d => d.Price, o => o.MapFrom(s => s.PriceAtBooking));
            CreateMap<BookingSeatLineDto, BookingSeatLine>();

            CreateMap<BookingFood, BookingFoodLineDto>()
                .ForMember(d => d.Name, o => o.MapFrom(s => s.Fb.Name))
                .ForMember(d => d.Price, o => o.MapFrom(s => s.PriceAtBooking));
            CreateMap<BookingFoodLineDto, BookingFoodLine>();
        }
    }
}
