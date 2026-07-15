using AutoMapper;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.ViewModels;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Mappings;

public class VatProfile : Profile
{
    // Cấu hình AutoMapper giữa entity Vat và các DTO/ViewModel.
    public VatProfile()
    {
        CreateMap<Vat, VatDTO>()
            .ForMember(d => d.HasUsage, o => o.Ignore());

        CreateMap<Vat, VatFormViewModel>();
    }
}
