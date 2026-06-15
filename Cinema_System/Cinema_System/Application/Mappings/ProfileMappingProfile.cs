using AutoMapper;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.ViewModels;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Mappings
{
    // Quy tắc map dữ liệu giữa các tầng (đăng ký ở Program.cs). AutoMapper tự copy field cùng tên;
    // chỉ khai báo thêm field nào map khác kiểu/khác tên.
    public class ProfileMappingProfile : Profile
    {
        public ProfileMappingProfile()
        {
            CreateMap<User, ProfileDto>()
                // DateOfBirth trong DB là DateOnly -> đổi sang DateTime
                .ForMember(d => d.DateOfBirth,
                    o => o.MapFrom(s => s.DateOfBirth.ToDateTime(TimeOnly.MinValue)))
                // Lấy tên vai trò từ bảng Role đã JOIN
                .ForMember(d => d.RoleName,
                    o => o.MapFrom(s => s.Role != null ? s.Role.Name : "UNKNOWN"))
                // Field nullable trong DB -> gán mặc định cho khỏi lỗi null ngoài giao diện
                .ForMember(d => d.RewardPoints, o => o.MapFrom(s => s.RewardPoints ?? 0))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status ?? "Active"))
                .ForMember(d => d.CreatedAt, o => o.MapFrom(s => s.CreatedAt ?? DateTime.Now));

            CreateMap<ProfileDto, ProfileViewModel>();
            CreateMap<RewardPointHistory, PointHistoryDto>();
            CreateMap<PointHistoryDto, PointHistoryViewModel>();
        }
    }
}
