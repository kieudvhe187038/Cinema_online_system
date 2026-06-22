using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;

namespace Cinema_System.Application.Interfaces;

public interface IReviewService
{
    /// <summary>
    /// Lấy danh sách đánh giá của một phim với phân trang
    /// </summary>
    Task<PagedResult<ReviewDTO>> GetMovieReviewsAsync(Guid movieId, int page = 1, int pageSize = 10);

    /// <summary>
    /// Kiểm tra xem user đã xem phim này chưa (có booking thành công)
    /// </summary>
    Task<bool> HasUserWatchedMovieAsync(Guid userId, Guid movieId); 

    /// <summary>
    /// Kiểm tra xem user đã đánh giá phim này chưa
    /// </summary>
    Task<bool> HasUserReviewedMovieAsync(Guid userId, Guid movieId);

    /// <summary>
    /// Tạo đánh giá mới
    /// </summary>
    Task<ReviewDTO> CreateReviewAsync(Guid userId, CreateReviewDTO reviewDTO);

    /// <summary>
    /// Lấy đánh giá theo ID
    /// </summary>
    Task<ReviewDTO?> GetReviewByIdAsync(Guid reviewId);

    /// <summary>
    /// Cập nhật đánh giá
    /// </summary>
    Task<ReviewDTO> UpdateReviewAsync(Guid reviewId, CreateReviewDTO reviewDTO);

    /// <summary>
    /// Xóa đánh giá
    /// </summary>
    Task DeleteReviewAsync(Guid reviewId);

    /// <summary>
    /// Lấy các đánh giá mới nhất (cho trang tổng quan reviews) với phân trang
    /// </summary>
    Task<PagedResult<ReviewDTO>> GetRecentReviewsAsync(int page = 1, int pageSize = 10);
}
