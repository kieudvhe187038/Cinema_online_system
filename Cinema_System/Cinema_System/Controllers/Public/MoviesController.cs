using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Cinema_System.Controllers.Public
{
    public class MoviesController : Controller
    {
        private readonly IMovieService _movieService;
        private readonly IReviewService _reviewService;

        public MoviesController(IMovieService movieService, IReviewService reviewService)
        {
            _movieService = movieService;
            _reviewService = reviewService;
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

        private static readonly System.Text.RegularExpressions.Regex SearchQueryRegex = new("^[\\p{L}\\p{N}\\s]+$", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.CultureInvariant);

        // Kiểm tra query tìm kiếm hợp lệ: không rỗng, tối đa 30 ký tự, chỉ chữ/số/khoảng trắng, hỗ trợ tiếng Việt.
        private static bool IsValidSearchQuery(string? searchQuery)
        {
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                return false;
            }

            var trimmed = searchQuery.Trim();
            if (trimmed.Length > 30)
            {
                return false;
            }

            return SearchQueryRegex.IsMatch(trimmed);
        }

        // Xử lý tìm kiếm phim theo tham số query string
        public async Task<IActionResult> Search([FromQuery(Name = "find")] string? searchQuery, int page = 1)
        {
            if (!IsValidSearchQuery(searchQuery))
            {
                TempData["SearchError"] = "Từ khóa tìm kiếm chỉ được tối đa 30 ký tự và không chứa ký tự đặc biệt.";
                return RedirectToAction("Index");
            } 

            var keyword = searchQuery!.Trim();
            var pagedMovies = await _movieService.SearchMoviesAsync(keyword, page, MoviePaging.DefaultPageSize);
            var moviesPageViewModel = BuildViewModel(pagedMovies, selectedTab: "search", searchKeyword: keyword);
            ViewData["SearchKeyword"] = keyword;
            return View("Index", moviesPageViewModel);
        }

        // Trang chọn phim để đánh giá (chỉ hiển thị phim user đã xem)
        [Authorize]
        public async Task<IActionResult> SelectForReview(int page = 1)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "");
            
            // Lấy tất cả phim mà user đã xem
            var allMovies = await _movieService.GetAllMoviesAsync();
            
            // Kiểm tra từng phim xem user đã xem chưa
            var watchedMovies = new List<MovieDTO>();
            foreach (var movie in allMovies)
            {
                if (await _reviewService.HasUserWatchedMovieAsync(userId, movie.Id))
                {
                    watchedMovies.Add(movie);
                }
            }

            // Phân trang
            int pageSize = MoviePaging.DefaultPageSize;
            var pagedMovies = new PagedResult<MovieDTO>
            {
                Items = watchedMovies.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)watchedMovies.Count / pageSize),
                PageSize = pageSize
            };

            var viewModel = BuildViewModel(pagedMovies, selectedTab: "myWatched", searchKeyword: string.Empty);
            ViewData["Title"] = "Chọn phim để đánh giá";
            
            return View("SelectForReview", viewModel);
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

        // Trang chi tiết phim (hỗ trợ slug và guid cũ).
        public async Task<IActionResult> Details(string id, int page = 1)
        {
            MovieDTO? movie = null;
            if (Guid.TryParse(id, out var guidId))
            {
                movie = await _movieService.GetMovieByIdAsync(guidId);
            }

            if (movie == null)
            {
                movie = await _movieService.GetMovieBySlugAsync(id);
            }

            if (movie == null)
                return NotFound();

            var reviews = await _reviewService.GetMovieReviewsAsync(movie.Id, page, 5);

            var vm = new MovieDetailsViewModel
            {
                Movie = movie,
                Reviews = reviews
            };

            ViewData["Title"] = movie.Title;
            return View("Details", vm);
        }
    }
}
