using AutoMapper;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.ViewModels;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Mappings;

public class FoodBeverageProfile : Profile
{
    public FoodBeverageProfile()
    {
        CreateMap<FoodBeverage, FoodBeverageDTO>()
            .ForMember(d => d.HasOrders, o => o.Ignore());

        CreateMap<FoodBeverage, FoodBeverageFormViewModel>();
    }
}
