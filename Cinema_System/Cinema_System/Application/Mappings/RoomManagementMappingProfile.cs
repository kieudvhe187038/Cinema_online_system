using AutoMapper;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.ViewModels;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Mappings
{
    public class RoomManagementMappingProfile : Profile
    {
        public RoomManagementMappingProfile()
        {
            CreateMap<Room, RoomListItemDto>()
                .ForMember(d => d.RoomTypeName, o => o.MapFrom(s => s.RoomType != null ? s.RoomType.Name : ""))
                .ForMember(d => d.TotalSeats, o => o.MapFrom(s => s.TotalSeats ?? 0))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status ?? ""));

            CreateMap<Seat, SeatItemDto>()
                .ForMember(d => d.SeatTypeName, o => o.MapFrom(s => s.SeatType != null ? s.SeatType.Name : ""))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status ?? ""));

            CreateMap<RoomListItemDto, RoomListItemViewModel>();
            CreateMap<RoomSeatsDto, RoomSeatsViewModel>();
            CreateMap<SeatItemDto, SeatItemViewModel>();
        }
    }
}
