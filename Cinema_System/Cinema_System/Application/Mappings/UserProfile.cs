using AutoMapper;
using Cinema_System.Application.DTOs;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Mappings;

/// <summary>
/// Cấu hình AutoMapper: ánh xạ giữa Entity User và UserDto.
/// </summary>
public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.RoleName,
                opt => opt.MapFrom(src => src.Role.Name))
            .ForMember(dest => dest.RewardPoints,
                opt => opt.MapFrom(src => src.RewardPoints ?? 0));
    }
}
