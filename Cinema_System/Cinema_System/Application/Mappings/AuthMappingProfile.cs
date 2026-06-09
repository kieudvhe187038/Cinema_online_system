using AutoMapper;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.ViewModels;

namespace Cinema_System.Application.Mappings;

/// <summary>
/// Ánh xạ giữa ViewModel (Presentation) và DTO (Application) cho nghiệp vụ tài khoản.
/// </summary>
public class AuthMappingProfile : Profile
{
    public AuthMappingProfile()
    {
        CreateMap<LoginViewModel, LoginDto>();
        CreateMap<RegisterViewModel, RegisterDto>();
    }
}
