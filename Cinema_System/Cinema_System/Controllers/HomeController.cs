using System.Diagnostics;
using System.Linq;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IMovieService _movieService;

        public HomeController(ILogger<HomeController> logger, IMovieService movieService)
        {
            _logger = logger;
            _movieService = movieService;
        }

        public async Task<IActionResult> Index(string? genre, string? ageRating, string? status)
        {
            var genres = await _movieService.GetAllGenresAsync();
            var ageRatings = await _movieService.GetAllAgeRatingsAsync();
            var statuses = await _movieService.GetAllMovieStatusesAsync();

            var filteredMovies = string.IsNullOrWhiteSpace(genre) && string.IsNullOrWhiteSpace(ageRating) && string.IsNullOrWhiteSpace(status)
                ? Enumerable.Empty<Cinema_System.Application.DTOs.MovieDTO>()
                : await _movieService.GetFilteredMoviesAsync(genre, ageRating, status);

            var nowShowingMovies = await _movieService.GetNowShowingMoviesAsync();
            var comingSoonMovies = await _movieService.GetComingSoonMoviesAsync();
            var specialShowtimeMovies = await _movieService.GetSpecialShowtimeMoviesAsync();

            var viewModel = new HomeViewModel
            {
                SelectedGenre = genre,
                SelectedAgeRating = ageRating,
                SelectedStatus = status,
                AvailableGenres = genres,
                AvailableAgeRatings = ageRatings,
                AvailableStatuses = statuses,
                FilteredMovies = filteredMovies,
                NowShowingMovies = nowShowingMovies,
                ComingSoonMovies = comingSoonMovies,
                SpecialShowtimeMovies = specialShowtimeMovies
            };

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
