using System.Diagnostics;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers.Public
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

        // Hiển thị trang chủ với các slider phim đang chiếu / sắp chiếu / suất chiếu đặc biệt.
        public async Task<IActionResult> Index()
        {
            var homeViewModel = new HomeViewModel
            {
                NowShowingMovies = await _movieService.GetNowShowingMoviesAsync(),
                ComingSoonMovies = await _movieService.GetComingSoonMoviesAsync(),
                SpecialMovies = await _movieService.GetSpecialMoviesAsync() 
            };

            return View(homeViewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Info()
        {
            ViewData["Title"] = "Thông tin rạp";
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
 