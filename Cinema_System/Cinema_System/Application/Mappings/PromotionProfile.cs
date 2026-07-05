using AutoMapper;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.ViewModels;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Mappings;

public class PromotionProfile : Profile
{
    // Cấu hình AutoMapper giữa entity Promotion và các DTO/ViewModel.
    public PromotionProfile()
    {
        CreateMap<Promotion, PromotionDTO>()
            .ForMember(d => d.HasUsage, o => o.Ignore());

        CreateMap<Promotion, PromotionFormViewModel>();
    }
}
