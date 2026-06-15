using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers
{
    public class MoviesController : Controller
    {
        private readonly IMovieService _movieService;

        public MoviesController(IMovieService movieService) 
        {
            _movieService = movieService;
        }

        // Trang danh sách phim theo tab (now, coming, special)
        public async Task<IActionResult> Index(string tab = "now", int page = 1) 
        {
            var pageSize = 3;
            var moviesPageViewModel = await _movieService.GetMoviesPageAsync(tab, page, pageSize);
            return View(moviesPageViewModel);
        }

        // Kiểm tra query tìm kiếm hợp lệ: không rỗng, tối đa 30 ký tự, chỉ chữ/số/khoảng trắng
        private bool IsValidSearchQuery(string searchQuery)
        {
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                return false;
            }

            if (searchQuery.Trim().Length > 30)
            {
                return false;
            }

            return System.Text.RegularExpressions.Regex.IsMatch(searchQuery.Trim(), @"^[\p{L}\p{N}\s]+$"); 
        }

        // Xử lý tìm kiếm phim theo tham số query string
        public async Task<IActionResult> Search([FromQuery(Name = "find")] string searchQuery, int page = 1) 
        {
            if (!IsValidSearchQuery(searchQuery))
            {
                return RedirectToAction("Index");
            }

            var pageSize = 3;
            var moviesPageViewModel = await _movieService.SearchMoviesAsync(searchQuery.Trim(), page, pageSize);
            ViewData["SearchKeyword"] = searchQuery.Trim();
            return View("Index", moviesPageViewModel);
        }
    }
}
