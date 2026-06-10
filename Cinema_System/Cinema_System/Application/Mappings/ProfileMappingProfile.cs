using AutoMapper;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.ViewModels;
using Cinema_System.Domain.Entities;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Cinema_System.Application.Mappings
{
    public class ProfileMappingProfile : Profile
    {
        public ProfileMappingProfile()
        {
            CreateMap<User, ProfileDto>()
                .ForMember(d => d.DateOfBirth,
                    o => o.MapFrom(s => s.DateOfBirth.ToDateTime(TimeOnly.MinValue)))
                .ForMember(d => d.RoleName,
                    o => o.MapFrom(s => s.Role != null ? s.Role.Name : "UNKNOWN"))
                .ForMember(d => d.RewardPoints, o => o.MapFrom(s => s.RewardPoints ?? 0))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status ?? "Active"))
                .ForMember(d => d.CreatedAt, o => o.MapFrom(s => s.CreatedAt ?? DateTime.Now));

            CreateMap<ProfileDto, ProfileViewModel>();
            CreateMap<RewardPointHistory, PointHistoryDto>();
            CreateMap<PointHistoryDto, PointHistoryViewModel>();
        }   
    }
}
