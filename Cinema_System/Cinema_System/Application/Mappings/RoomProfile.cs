using AutoMapper;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.ViewModels;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Mappings;

public class RoomProfile : Profile
{
    public RoomProfile()
    {
        // Chiều ĐỌC: Entity → DTO/VM. Các trường đếm (SeatCount/HasShowtimes...)
        // do Service set thủ công sau khi map.
        CreateMap<Room, RoomDTO>()
            .ForMember(d => d.RoomTypeName, o => o.MapFrom(s => s.RoomType.Name))
            .ForMember(d => d.SeatCount, o => o.Ignore())
            .ForMember(d => d.HasShowtimes, o => o.Ignore())
            .ForMember(d => d.HasBookedTickets, o => o.Ignore());

        CreateMap<Room, RoomFormViewModel>()
            .ForMember(d => d.RoomTypeOptions, o => o.Ignore())
            .ForMember(d => d.SeatTypes, o => o.Ignore())
            .ForMember(d => d.SeatsJson, o => o.Ignore())
            .ForMember(d => d.Locked, o => o.Ignore())
            .ForMember(d => d.TotalRow, o => o.MapFrom(s => s.TotalRow ?? 1))
            .ForMember(d => d.TotalColumns, o => o.MapFrom(s => s.TotalColumns ?? 1));

        CreateMap<SeatType, SeatTypeOptionDTO>();
    }
}
