using Cinema_System.Application.DTOs;

namespace Cinema_System.Application.ViewModels;

public class HomeViewModel
{
    public IEnumerable<MovieDTO> NowShowingMovies { get; set; } = new List<MovieDTO>();
    public IEnumerable<MovieDTO> ComingSoonMovies { get; set; } = new List<MovieDTO>();
}
