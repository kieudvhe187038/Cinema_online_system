using AutoMapper;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.ViewModels;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Mappings;

// Quy tắc map cho module quản lý lịch chiếu (Manager).
public class ShowtimeScheduleMappingProfile : Profile
{
    public ShowtimeScheduleMappingProfile()
    {
        CreateMap<Room, ItemOptionDTO>();
        CreateMap<Movie, ItemOptionDTO>()
            .ForMember(d => d.Name, o => o.MapFrom(s => s.Title));

        CreateMap<Showtime, ShowtimeScheduleDTO>()
            .ForMember(d => d.MovieTitle, o => o.MapFrom(s => s.Movie != null ? s.Movie.Title : "—"))
            .ForMember(d => d.RoomName, o => o.MapFrom(s => s.Room != null ? s.Room.Name : "—"))
            .ForMember(d => d.Status, o => o.MapFrom(s => string.IsNullOrEmpty(s.Status) ? "Scheduled" : s.Status))
            // Cần truy vấn Bookings/Tickets riêng -> gán sau khi map.
            .ForMember(d => d.HasBookings, o => o.Ignore());

        CreateMap<Showtime, ShowtimeFormViewModel>()
            .ForMember(d => d.Status, o => o.MapFrom(s => string.IsNullOrEmpty(s.Status) ? "Scheduled" : s.Status))
            // Gán sau khi map: cần truy vấn riêng (HasBookings) hoặc nạp cho dropdown (Available*).
            .ForMember(d => d.HasBookings, o => o.Ignore())
            .ForMember(d => d.AvailableMovies, o => o.Ignore())
            .ForMember(d => d.AvailableRooms, o => o.Ignore());
    }
}
