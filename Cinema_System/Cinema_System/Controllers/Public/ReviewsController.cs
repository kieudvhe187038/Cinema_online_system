using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Cinema_System.Controllers.Public
{
    /// <summary>
    /// Tầng Presentation module Đánh giá: nhận request -> gọi Service -> trả View.
    /// Quản lý danh sách đánh giá phim, form tạo đánh giá mới, kiểm tra quyền đánh giá.
    /// </summary>
    public class ReviewsController : Controller
    {
        private readonly IReviewService _reviewService;
        private readonly IMovieService _movieService; 

        public ReviewsController(IReviewService reviewService, IMovieService movieService)
        {
            _reviewService = reviewService;
            _movieService = movieService;
        }

        /// <summary>
        /// Trang danh sách đánh giá phim (có thể lọc theo movieId hoặc xem tất cả đánh giá mới nhất).
        /// Không bắt buộc đăng nhập - khán giả có thể xem đánh giá của người khác.
        /// </summary>
        public async Task<IActionResult> Index(Guid? movieId, int page = 1)
        {
            if (movieId.HasValue)
            {
                var movie = await _movieService.GetMovieByIdAsync(movieId.Value);
                if (movie == null)
                    return NotFound();

                var reviews = await _reviewService.GetMovieReviewsAsync(movieId.Value, page, 12);
                ViewData["MovieId"] = movieId.Value;
                ViewData["MovieTitle"] = movie.Title;
                return View(reviews);
            }

            // Nếu không có movieId -> trả về danh sách đánh giá mới nhất (cho mọi phim)
            var recent = await _reviewService.GetRecentReviewsAsync(page, 12);
            ViewData["MovieId"] = null;
            ViewData["MovieTitle"] = "Đánh giá của khán giả";
            return View(recent);
        }

        /// <summary>
        /// Trang form viết đánh giá phim (GET).
        /// Bắt buộc đăng nhập + kiểm tra user đã xem phim + chưa đánh giá trước đó.
        /// </summary>
        [Authorize]
        public async Task<IActionResult> Create(Guid movieId)
        {
            try
            {
                var movie = await _movieService.GetMovieByIdAsync(movieId);
                if (movie == null)
                    return NotFound();

                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "");

                // Kiểm tra user đã xem phim này chưa
                var hasWatched = await _reviewService.HasUserWatchedMovieAsync(userId, movieId);
                if (!hasWatched)
                {
                    TempData["Error"] = "Bạn chưa xem phim này nên chưa được đánh giá. Vui lòng đặt vé xem phim trước.";
                    return RedirectToAction("Details", "Movies", new { id = movieId });
                }

                // Kiểm tra user đã đánh giá phim này chưa
                var hasReviewed = await _reviewService.HasUserReviewedMovieAsync(userId, movieId);
                if (hasReviewed)
                {
                    TempData["Error"] = "Bạn đã đánh giá phim này rồi.";
                    return RedirectToAction("Details", "Movies", new { id = movieId });
                }

                var model = new CreateReviewDTO { MovieId = movieId };
                ViewData["MovieTitle"] = movie.Title;
                ViewData["MovieSlug"] = movie.Slug;

                return View(model);
            }
            catch (Exception)
            {
                TempData["Error"] = "Có lỗi xảy ra. Vui lòng thử lại sau.";
                return RedirectToAction("Details", "Movies", new { id = movieId });
            }
        }

        /// <summary>
        /// Xử lý gửi đánh giá phim (POST).
        /// Bắt buộc đăng nhập, validate dữ liệu, lưu đánh giá, redirect về chi tiết phim.
        /// </summary>
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create(CreateReviewDTO reviewDTO)
        {
            if (!ModelState.IsValid)
            {
                var movie = await _movieService.GetMovieByIdAsync(reviewDTO.MovieId);
                if (movie != null)
                {
                    ViewData["MovieId"] = reviewDTO.MovieId;
                    ViewData["MovieTitle"] = movie.Title;
                    ViewData["MovieSlug"] = movie.Slug;
                    return View(reviewDTO);
                }

                TempData["Error"] = "Dữ liệu không hợp lệ.";
                return RedirectToAction("Index", "Movies");
            }

            var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "");

            try
            {
                await _reviewService.CreateReviewAsync(userId, reviewDTO);
                TempData["Success"] = "Cảm ơn bạn! Đánh giá của bạn đã được gửi. Chúng tôi sẽ duyệt và hiển thị trong thời gian sớm nhất.";

                var movie = await _movieService.GetMovieByIdAsync(reviewDTO.MovieId);
                return RedirectToAction("Details", "Movies", new { id = movie?.Slug ?? reviewDTO.MovieId.ToString() });
            }
            catch (Exception ex)
            {
                var movie = await _movieService.GetMovieByIdAsync(reviewDTO.MovieId);
                if (movie != null)
                {
                    ViewData["MovieId"] = reviewDTO.MovieId;
                    ViewData["MovieTitle"] = movie.Title;
                    ViewData["MovieSlug"] = movie.Slug;
                }
                
                // Log chi tiết lỗi để debugging
                System.Diagnostics.Debug.WriteLine($"[ReviewCreate] Error: {ex.GetType().Name} - {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ReviewCreate] InnerException: {ex.InnerException?.Message}");
                
                TempData["Error"] = $"Có lỗi xảy ra: {ex.Message}";
                return View(reviewDTO);
            }
        }
    }
}
