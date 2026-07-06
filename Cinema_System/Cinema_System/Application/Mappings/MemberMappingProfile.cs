using AutoMapper;
using Cinema_System.Application.DTOs;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Mappings;

// AutoMapper cho tra cứu thành viên tại quầy (Inter2 - Staff).
public class MemberMappingProfile : Profile
{
    public MemberMappingProfile()
    {
        // MaskedPhone là computed property (chỉ có getter) nên AutoMapper tự bỏ qua.
        CreateMap<User, MemberDTO>()
            .ForMember(d => d.RewardPoints, o => o.MapFrom(s => s.RewardPoints ?? 0));
    }
}
