using System.Diagnostics;
using System.Linq;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers
{
    // Controller chính cho trang chủ và các bộ lọc phim.
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IMovieService _movieService;

        public HomeController(ILogger<HomeController> logger, IMovieService movieService)
        {
            _logger = logger;
            _movieService = movieService;
        }

        // Hiển thị trang chủ, load dữ liệu bộ chọn và kết quả lọc nếu người dùng chọn bộ lọc.
        public async Task<IActionResult> Index(string? genre, string? ageRating, string? status)
        {
            var availableGenres = await _movieService.GetAllGenresAsync();
            var availableAgeRatings = await _movieService.GetAllAgeRatingsAsync();
            var availableStatuses = await _movieService.GetAllMovieStatusesAsync();

            var filteredMovies = string.IsNullOrWhiteSpace(genre) && string.IsNullOrWhiteSpace(ageRating) && string.IsNullOrWhiteSpace(status)
                ? Enumerable.Empty<Cinema_System.Application.DTOs.MovieDTO>()
                : await _movieService.GetFilteredMoviesAsync(genre, ageRating, status);

            var nowShowingMovies = await _movieService.GetNowShowingMoviesAsync();
            var comingSoonMovies = await _movieService.GetComingSoonMoviesAsync();
            var specialShowtimeMovies = await _movieService.GetSpecialShowtimeMoviesAsync();

            var homeViewModel = new HomeViewModel
            {
                SelectedGenre = genre,
                SelectedAgeRating = ageRating,
                SelectedStatus = status,
                AvailableGenres = availableGenres,
                AvailableAgeRatings = availableAgeRatings,
                AvailableStatuses = availableStatuses,  
                FilteredMovies = filteredMovies,
                NowShowingMovies = nowShowingMovies,
                ComingSoonMovies = comingSoonMovies, 
                SpecialShowtimeMovies = specialShowtimeMovies
            };

            _logger.LogInformation("Home page loaded with filters: Genre={Genre}, AgeRating={AgeRating}, Status={Status}", genre, ageRating, status);
            return View(homeViewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // Trang lỗi dùng để hiển thị thông tin request khi có ngoại lệ.
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)] 
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier }); 
        }
    }
}
 