using AutoMapper;
using Cinema_System.Application.DTOs;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Mappings;

// Quy tắc map cho module đặt vé tại quầy (Staff Counter).
public class CounterBookingMappingProfile : Profile
{
    public CounterBookingMappingProfile()
    {
        CreateMap<Movie, MovieOptionDTO>();
        CreateMap<FoodBeverage, FoodOptionDTO>();
    }
}
