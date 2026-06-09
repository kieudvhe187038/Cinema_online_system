using AutoMapper;
using Cinema_System.Application.DTOs;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Mappings;

public class MovieMappingProfile : Profile
{
    public MovieMappingProfile()
    {
        CreateMap<Movie, MovieDTO>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
            .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.Slug))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.TrailerUrl, opt => opt.MapFrom(src => src.TrailerUrl))
            .ForMember(dest => dest.PosterUrl, opt => opt.MapFrom(src => src.PosterUrl))
            .ForMember(dest => dest.BannerUrl, opt => opt.MapFrom(src => src.BannerUrl))
            .ForMember(dest => dest.Director, opt => opt.MapFrom(src => src.Director))
            .ForMember(dest => dest.CastMembers, opt => opt.MapFrom(src => src.CastMembers))
            .ForMember(dest => dest.Language, opt => opt.MapFrom(src => src.Language))
            .ForMember(dest => dest.Subtitle, opt => opt.MapFrom(src => src.Subtitle))
            .ForMember(dest => dest.DurationMinutes, opt => opt.MapFrom(src => src.DurationMinutes))
            .ForMember(dest => dest.ReleaseDate, opt => opt.MapFrom(src => src.ReleaseDate))
            .ForMember(dest => dest.AgeRating, opt => opt.MapFrom(src => src.AgeRating))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status));
    }
}
