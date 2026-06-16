using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers.Public
{
    public class MoviesController : Controller
    {
        private readonly IMovieService _movieService;

        public MoviesController(IMovieService movieService)
        {
            _movieService = movieService;
        }

        // Trang danh sách phim theo tab (now, coming, special), lọc theo thể loại/độ tuổi.
        public async Task<IActionResult> Index(string tab = "now", int page = 1, string? genre = null, string? ageRating = null)
        {
            var pagedMovies = await _movieService.GetMoviesPageAsync(tab, page, MoviePaging.DefaultPageSize, genre, ageRating);

            var moviesPageViewModel = BuildViewModel(pagedMovies, tab?.ToLower() ?? "now", searchKeyword: string.Empty);
            moviesPageViewModel.SelectedGenre = genre;
            moviesPageViewModel.SelectedAgeRating = ageRating;
            moviesPageViewModel.AvailableGenres = await _movieService.GetAllGenresAsync();
            moviesPageViewModel.AvailableAgeRatings = await _movieService.GetAllAgeRatingsAsync();

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

            var keyword = searchQuery.Trim();
            var pagedMovies = await _movieService.SearchMoviesAsync(keyword, page, MoviePaging.DefaultPageSize);
            var moviesPageViewModel = BuildViewModel(pagedMovies, selectedTab: "search", searchKeyword: keyword);
            ViewData["SearchKeyword"] = keyword;
            return View("Index", moviesPageViewModel);
        }

        // Map kết quả phân trang (tầng Application) sang ViewModel của giao diện.
        private static MoviesPageViewModel BuildViewModel(PagedResult<MovieDTO> pagedMovies, string selectedTab, string searchKeyword)
        {
            return new MoviesPageViewModel
            {
                SelectedTab = selectedTab,
                SearchKeyword = searchKeyword,
                Movies = pagedMovies.Items,
                CurrentPage = pagedMovies.CurrentPage,
                TotalPages = pagedMovies.TotalPages,
                PageSize = pagedMovies.PageSize
            };
        }
    }
}
