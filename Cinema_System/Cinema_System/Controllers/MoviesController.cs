using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers
{
    public class MoviesController : Controller
    {
        private readonly IMovieService _movieService;
        private readonly ILogger<MoviesController> _logger;

        public MoviesController(ILogger<MoviesController> logger, IMovieService movieService)
        {
            _logger = logger;
            _movieService = movieService;
        }

        public async Task<IActionResult> Index(string tab = "now", int page = 1)
        {
            var nowShowing = (await _movieService.GetNowShowingMoviesAsync()).ToList();
            var comingSoon = (await _movieService.GetComingSoonMoviesAsync()).ToList();
            var special = (await _movieService.GetSpecialShowtimeMoviesAsync()).ToList();

            var pageSize = 4;
            IEnumerable<Cinema_System.Application.DTOs.MovieDTO> source = tab?.ToLower() switch
            {
                "coming" => comingSoon,
                "special" => special,
                _ => nowShowing,
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

            return View(vm);
        }
    }
}
