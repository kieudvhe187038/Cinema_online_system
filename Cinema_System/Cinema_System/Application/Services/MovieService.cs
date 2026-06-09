using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;

namespace Cinema_System.Application.Services;

public class MovieService : IMovieService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly AutoMapper.IMapper _mapper;

    public MovieService(IUnitOfWork unitOfWork, AutoMapper.IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

        public async Task<Cinema_System.Application.ViewModels.MoviesPageViewModel> GetMoviesPageAsync(string tab, int page, int pageSize)
        {
            IEnumerable<MovieDTO> source = tab?.ToLower() switch
            {
                "coming" => (await GetComingSoonMoviesAsync()).ToList(),
                "special" => (await GetSpecialShowtimeMoviesAsync()).ToList(),
                _ => (await GetNowShowingMoviesAsync()).ToList(),
            };

            var totalCount = source.Count();
            var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var items = source.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var vm = new Cinema_System.Application.ViewModels.MoviesPageViewModel
            {
                SelectedTab = tab?.ToLower() ?? "now",
                Movies = items,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize
            };

            return vm;
        }

    public async Task<IEnumerable<MovieDTO>> GetAllMoviesAsync()
    {
        var movies = await _unitOfWork.Movies.GetAllAsync(
            includeProperties: new[] { "Showtimes" }
        );

        return _mapper.Map<IEnumerable<MovieDTO>>(movies);
    }

    public async Task<IEnumerable<MovieDTO>> GetNowShowingMoviesAsync()
    {
        var movies = await _unitOfWork.Movies.GetAllAsync(
            predicate: m => m.Status == "Now Showing",
            includeProperties: new[] { "Showtimes" }
        );

        return _mapper.Map<IEnumerable<MovieDTO>>(movies);
    }

    public async Task<IEnumerable<MovieDTO>> GetComingSoonMoviesAsync()
    {
        var movies = await _unitOfWork.Movies.GetAllAsync(
            predicate: m => m.Status == "Coming Soon",
            includeProperties: new[] { "Showtimes" }
        );

        return _mapper.Map<IEnumerable<MovieDTO>>(movies);
    }

    public async Task<MovieDTO?> GetMovieByIdAsync(Guid id)
    {
        var movie = await _unitOfWork.Movies.FirstOrDefaultAsync(
            predicate: m => m.Id == id,
            includeProperties: new[] { "Showtimes" }
        );

        return movie == null ? null : _mapper.Map<MovieDTO>(movie);
    }

    public async Task<IEnumerable<MovieDTO>> GetSpecialShowtimeMoviesAsync()
    {
        var movies = await _unitOfWork.Movies.GetAllAsync(
            predicate: m => m.Showtimes.Any(s =>
                s.Status == "Special" ||
                s.Status == "Special Screening" ||
                (s.Status != null && s.Status.Contains("Đặc"))
            ),
            includeProperties: new[] { "Showtimes" }
        );

        return _mapper.Map<IEnumerable<MovieDTO>>(movies);
    }
}
