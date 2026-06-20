using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Cinema_System.Controllers.Public
{
    /// <summary>
    /// Controller cho phần hiển thị phim và chi tiết phim ngoài trang chủ công khai
    /// </summary>
    public class MoviesController : Controller
    {
        private readonly IMovieService _movieService;
        private readonly IReviewService _reviewService;

        public MoviesController(IMovieService movieService, IReviewService reviewService)
        {
            _movieService = movieService;
            _reviewService = reviewService;
        }

        /// <summary>
        /// Trang danh sách phim theo tab (đang chiếu, sắp chiếu, suất chiếu đặc biệt)
        /// Hỗ trợ lọc theo thể loại và độ tuổi phân loại
        /// </summary>
        
        public async Task<IActionResult> Index(string tab = "now", int page = 1, string? genre = null, string? ageRating = null)
        {
            // Lấy danh sách phim đã được phân trang từ service
            var pagedMovies = await _movieService.GetMoviesPageAsync(tab, page, MoviePaging.DefaultPageSize, genre, ageRating);

            // Xây dựng ViewModel cho trang danh sách phim
            var moviesPageViewModel = BuildViewModel(pagedMovies, tab?.ToLower() ?? "now", searchKeyword: string.Empty);
            moviesPageViewModel.SelectedGenre = genre;
            moviesPageViewModel.SelectedAgeRating = ageRating;
            moviesPageViewModel.AvailableGenres = await _movieService.GetAllGenresAsync();
            moviesPageViewModel.AvailableAgeRatings = await _movieService.GetAllAgeRatingsAsync();

            return View(moviesPageViewModel);
        }

        private static readonly System.Text.RegularExpressions.Regex SearchQueryRegex = new("^[\\p{L}\\p{N}\\s]+$", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.CultureInvariant);

        /// <summary>
        /// Kiểm tra từ khóa tìm kiếm hợp lệ (không rỗng, tối đa 30 ký tự, chỉ chứa chữ, số và khoảng trắng)
        /// </summary>
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

        /// <summary>
        /// Xử lý tìm kiếm phim theo từ khóa
        /// </summary>
       
        public async Task<IActionResult> Search([FromQuery(Name = "find")] string? searchQuery, int page = 1)
        {
            if (!IsValidSearchQuery(searchQuery))
            {
                TempData["SearchError"] = "Từ khóa tìm kiếm chỉ được tối đa 30 ký tự và không chứa ký tự đặc biệt.";
                return RedirectToAction("Index");
            } 

            var keyword = searchQuery!.Trim();
            // Thực hiện tìm kiếm phim có chứa từ khóa
            var pagedMovies = await _movieService.SearchMoviesAsync(keyword, page, MoviePaging.DefaultPageSize);
            var moviesPageViewModel = BuildViewModel(pagedMovies, selectedTab: "search", searchKeyword: keyword);
            ViewData["SearchKeyword"] = keyword;
            return View("Index", moviesPageViewModel);
        }

        /// <summary>
        /// Trang chọn phim để đánh giá (chỉ hiển thị những phim mà người dùng hiện tại đã mua vé và đã xem)
        /// </summary>
        
        [Authorize]
        public async Task<IActionResult> SelectForReview(int page = 1)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "");
            
            // Lấy toàn bộ danh sách phim để lọc
            var allMovies = await _movieService.GetAllMoviesAsync();
            
            // Lọc ra các phim mà người dùng đã xem
            var watchedMovies = new List<MovieDTO>();
            foreach (var movie in allMovies)
            {
                if (await _reviewService.HasUserWatchedMovieAsync(userId, movie.Id))
                {
                    watchedMovies.Add(movie);
                }
            }

            // Phân trang thủ công danh sách phim đã xem
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

        /// <summary>
        /// Hàm nội bộ ánh xạ PagedResult từ tầng Service sang View Model
        /// </summary>
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

        /// <summary>
        /// Trang hiển thị chi tiết phim (hỗ trợ cả định dạng ID kiểu GUID hoặc đường dẫn thân thiện Slug)
        /// </summary>
       
        public async Task<IActionResult> Details(string id, int page = 1)
        {
            MovieDTO? movie = null;
            // Kiểm tra xem ID truyền vào có phải là GUID hợp lệ không
            if (Guid.TryParse(id, out var guidId))
            {
                movie = await _movieService.GetMovieByIdAsync(guidId);
            }

            // Nếu không phải GUID hoặc không tìm thấy bằng GUID, tìm theo Slug
            if (movie == null)
            {
                movie = await _movieService.GetMovieBySlugAsync(id);
            }

            if (movie == null)
                return NotFound();

            // Tải danh sách đánh giá được phân trang (mặc định 5 đánh giá trên một trang)
            var reviews = await _reviewService.GetMovieReviewsAsync(movie.Id, page, 5);

            var vm = new MovieDetailsViewModel
            {
                Movie = movie,
                Reviews = reviews
            };

            ViewData["Title"] = movie.Title;
            // Truyền trang hiện tại sang View để xử lý chuyển trang Ajax/URL
            ViewData["CurrentReviewPage"] = page;
            return View("Details", vm);
        }

        /// <summary>
        /// Endpoint trả về PartialView danh sách đánh giá cho yêu cầu AJAX
        /// Giúp tải/chuyển trang đánh giá mà không cần tải lại toàn bộ trang chi tiết phim
        /// </summary>
       
        [HttpGet]
        public async Task<IActionResult> LoadReviewsPartial(Guid id, int page = 1)
        {
            var reviews = await _reviewService.GetMovieReviewsAsync(id, page, 5);
            return PartialView("_ReviewList", reviews);
        }
    }
}
