using AutoMapper;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.ViewModels;
using Cinema_System.Domain.Entities;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Cinema_System.Application.Mappings
{
    // Khai báo "luật chuyển đổi". Profile = 1 nhóm luật map của AutoMapper.
    public class ProfileMappingProfile : Profile
    {
        public ProfileMappingProfile()
        {
            // User (Entity) -> ProfileDto
            CreateMap<User, ProfileDto>()
                // DateOnly -> DateTime? (AutoMapper không tự đổi được 2 kiểu này)
                .ForMember(d => d.DateOfBirth,
                    o => o.MapFrom(s => s.DateOfBirth.ToDateTime(TimeOnly.MinValue)))
                // Lấy tên Role từ bảng liên kết
                .ForMember(d => d.RoleName,
                    o => o.MapFrom(s => s.Role != null ? s.Role.Name : "UNKNOWN"))
                // Các field nullable -> giá trị mặc định
                .ForMember(d => d.RewardPoints, o => o.MapFrom(s => s.RewardPoints ?? 0))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status ?? "Active"))
                .ForMember(d => d.CreatedAt, o => o.MapFrom(s => s.CreatedAt ?? DateTime.Now));

            // ProfileDto -> ProfileViewModel (các field trùng tên nên tự map hết;
            // Age không có setter nên AutoMapper tự bỏ qua)
            CreateMap<ProfileDto, ProfileViewModel>();
        }
    }
}
