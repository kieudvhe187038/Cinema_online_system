using AutoMapper;
using Cinema_System.Application.DTOs;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Mappings;

public class MovieMappingProfile : Profile
{
    public MovieMappingProfile()
    {
        // Mọi thuộc tính Movie -> MovieDTO trùng tên nên AutoMapper tự map theo convention.
        CreateMap<Movie, MovieDTO>();
    }
}
