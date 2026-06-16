using AutoMapper;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.ViewModels;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Mappings;

// AutoMapper cho module quản trị của taido (User, Role, SeatType, RoomType).
// AutoMapper tự copy field cùng tên; chỉ khai báo thêm field map khác tên/khác nguồn,
// hoặc Ignore các field do service tính toán (SeatCount/RoomCount/InUse, Roles).
public class AdminMappingProfile : Profile
{
    public AdminMappingProfile()
    {
        // User -> UserDTO (trang quản trị): lấy tên vai trò từ Role đã JOIN.
        CreateMap<User, UserDTO>()
            .ForMember(d => d.RoleName, o => o.MapFrom(s => s.Role != null ? s.Role.Name : string.Empty));

        // User -> form sửa: danh sách Roles do service nạp riêng.
        CreateMap<User, UserEditViewModel>()
            .ForMember(d => d.Roles, o => o.Ignore());

        CreateMap<Role, RoleDTO>();

        // SeatType -> DTO: SeatCount/InUse cần truy vấn thêm nên service tự gán sau khi map.
        CreateMap<SeatType, SeatTypeDTO>()
            .ForMember(d => d.SeatCount, o => o.Ignore())
            .ForMember(d => d.InUse, o => o.Ignore());
        CreateMap<SeatType, SeatTypeFormViewModel>();

        // RoomType -> DTO: RoomCount/InUse cần truy vấn thêm nên service tự gán sau khi map.
        CreateMap<RoomType, RoomTypeDTO>()
            .ForMember(d => d.RoomCount, o => o.Ignore())
            .ForMember(d => d.InUse, o => o.Ignore());
        CreateMap<RoomType, RoomTypeFormViewModel>();
    }
}
