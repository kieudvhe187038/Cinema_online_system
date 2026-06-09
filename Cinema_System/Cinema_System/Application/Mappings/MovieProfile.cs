using AutoMapper;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.ViewModels;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Mappings;

public class MovieProfile : Profile
{
    public MovieProfile()
    {
        CreateMap<Movie, MovieDTO>()
            .ForMember(d => d.GenreNames, o => o.MapFrom(s => s.Genres.Select(g => g.Name).ToList()));

        CreateMap<Movie, MovieFormViewModel>()
            .ForMember(d => d.SelectedGenreIds, o => o.MapFrom(s => s.Genres.Select(g => g.Id).ToList()))
            .ForMember(d => d.AvailableGenres, o => o.Ignore());

        CreateMap<Genre, GenreDTO>();
    }
}
