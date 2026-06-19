using AutoMapper;
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

            CreateMap<BookingDetailDto, BookingDetailViewModel>();
            CreateMap<BookingSeatLineDto, BookingSeatLine>();
            CreateMap<BookingFoodLineDto, BookingFoodLine>();
        }
    }
}
