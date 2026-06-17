using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Cinema_System.Controllers.Public;

[Route("reviews")]
public class ReviewsController : Controller
{
    private readonly IReviewService _reviewService;
    private readonly IMovieService _movieService;

    public ReviewsController(IReviewService reviewService, IMovieService movieService)
    {
        _reviewService = reviewService;
        _movieService = movieService;
    }

    // Trang danh sách đánh giá của một phim
    [HttpGet("")]
    public async Task<IActionResult> Index(Guid? movieId, int page = 1)
    {
        if (movieId.HasValue)
        {
            var movie = await _movieService.GetMovieByIdAsync(movieId.Value);
            if (movie == null)
                return NotFound();

            var reviews = await _reviewService.GetMovieReviewsAsync(movieId.Value, page, 10);
            ViewData["MovieId"] = movieId.Value;
            ViewData["MovieTitle"] = movie.Title;
            return View(reviews);
        }

        // Nếu không có movieId -> trả về danh sách đánh giá mới nhất (cho mọi phim)
        var recent = await _reviewService.GetRecentReviewsAsync(page, 10);
        ViewData["MovieId"] = null;
        ViewData["MovieTitle"] = "Đánh giá của khán giả";
        return View(recent);
    }

    // Trang form đánh giá phim (GET)
    [Authorize]
    [HttpGet("create/{movieId}")]
    public async Task<IActionResult> Create(Guid movieId)
    {
        var movie = await _movieService.GetMovieByIdAsync(movieId);
        if (movie == null)
            return NotFound();

        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "");
        
        // Kiểm tra user đã xem phim này chưa
        var hasWatched = await _reviewService.HasUserWatchedMovieAsync(userId, movieId);
        if (!hasWatched)
        {
            TempData["Error"] = "Bạn chỉ có thể đánh giá những phim mà bạn đã xem.";
            return RedirectToAction("Details", "Movies", new { id = movieId });
        }

        // Kiểm tra user đã đánh giá phim này chưa
        var hasReviewed = await _reviewService.HasUserReviewedMovieAsync(userId, movieId);
        if (hasReviewed)
        {
            TempData["Error"] = "Bạn đã đánh giá phim này rồi.";
            return RedirectToAction("Details", "Movies", new { id = movieId });
        }

        ViewData["MovieId"] = movieId;
        ViewData["MovieTitle"] = movie.Title;
        ViewData["MovieSlug"] = movie.Slug;

        return View();
    }

    // Xử lý gửi đánh giá phim (POST)
    [Authorize]
    [HttpPost("create")]
    public async Task<IActionResult> Create(CreateReviewDTO reviewDTO)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Dữ liệu không hợp lệ.";
            return RedirectToAction("Create", new { movieId = reviewDTO.MovieId });
        }

        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "");

        try
        {
            await _reviewService.CreateReviewAsync(userId, reviewDTO);
            TempData["Success"] = "Cảm ơn bạn! Đánh giá của bạn đã được gửi. Chúng tôi sẽ duyệt và hiển thị trong thời gian sớm nhất.";

            var movie = await _movieService.GetMovieByIdAsync(reviewDTO.MovieId);
            return RedirectToAction("Details", "Movies", new { id = movie?.Slug ?? reviewDTO.MovieId.ToString() });
        }
        catch (Exception)
        {
            TempData["Error"] = "Có lỗi xảy ra. Vui lòng thử lại sau.";
            return RedirectToAction("Create", new { movieId = reviewDTO.MovieId });
        }
    }
}
