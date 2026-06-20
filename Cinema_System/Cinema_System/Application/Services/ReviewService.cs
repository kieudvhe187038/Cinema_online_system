using AutoMapper;
using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Services;

/// <summary>
/// Dịch vụ quản lý đánh giá phim (Reviews) từ phía người dùng
/// </summary>
public class ReviewService : IReviewService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ReviewService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    /// <summary>
    /// Lấy danh sách đánh giá của một bộ phim đã được phê duyệt (status = approved), phân trang và sắp xếp giảm dần theo thời gian tạo
    /// </summary>
    public async Task<PagedResult<ReviewDTO>> GetMovieReviewsAsync(Guid movieId, int page = 1, int pageSize = 10)
    {
        var reviews = await _unitOfWork.Reviews
            .GetAllAsync(
                predicate: r => r.MovieId == movieId && r.Status == "approved",
                includeProperties: new[] { "User", "Movie" },
                orderBy: r => r.OrderByDescending(x => x.CreatedAt)
            );

        var reviewList = reviews.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var total = reviews.Count();

        return new PagedResult<ReviewDTO>
        {
            Items = _mapper.Map<List<ReviewDTO>>(reviewList),
            CurrentPage = page,
            TotalPages = total == 0 ? 1 : (int)Math.Ceiling((double)total / pageSize),
            PageSize = pageSize,
            TotalCount = total
        };
    }

    /// <summary>
    /// Kiểm tra xem người dùng đã xem phim này chưa (đã mua vé và thanh toán thành công hay chưa)
    /// </summary>
    public async Task<bool> HasUserWatchedMovieAsync(Guid userId, Guid movieId)
    {
        // Lấy tất cả booking của user đã thanh toán thành công
        var bookings = await _unitOfWork.Bookings
            .GetAllAsync(
                predicate: b => b.UserId == userId && b.PaymentStatus == "completed"
            );

        if (!bookings.Any())
            return false;

        // Kiểm tra xem booking nào có chứa vé của phim này
        foreach (var booking in bookings)
        {
            var tickets = await _unitOfWork.Tickets
                .GetAllAsync(
                    predicate: t => t.BookingId == booking.Id,
                    includeProperties: new[] { "Showtime" }
                );

            if (tickets.Any(t => t.Showtime?.MovieId == movieId))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Kiểm tra xem người dùng đã từng đánh giá bộ phim này chưa
    /// </summary>
    public async Task<bool> HasUserReviewedMovieAsync(Guid userId, Guid movieId)
    {
        return await _unitOfWork.Reviews
            .ExistsAsync(r => r.UserId == userId && r.MovieId == movieId);
    }

    /// <summary>
    /// Tạo mới một đánh giá phim ở trạng thái chờ duyệt (status = pending)
    /// </summary>
    public async Task<ReviewDTO> CreateReviewAsync(Guid userId, CreateReviewDTO reviewDTO)
    {
        var review = new Review
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            MovieId = reviewDTO.MovieId,
            Rating = reviewDTO.Rating,
            Comment = reviewDTO.Comment,
            Status = "pending", // Đánh giá mới tạo luôn ở trạng thái chờ duyệt
            CreatedAt = DateTime.Now
        };

        await _unitOfWork.Reviews.AddAsync(review);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<ReviewDTO>(review);
    }

    /// <summary>
    /// Lấy thông tin chi tiết một đánh giá qua ID
    /// </summary>
    public async Task<ReviewDTO?> GetReviewByIdAsync(Guid reviewId)
    {
        var review = await _unitOfWork.Reviews
            .FirstOrDefaultAsync(
                predicate: r => r.Id == reviewId,
                includeProperties: new[] { "User", "Movie" }
            );

        return review != null ? _mapper.Map<ReviewDTO>(review) : null;
    }

    /// <summary>
    /// Lấy danh sách các đánh giá mới nhất trên toàn hệ thống (đã duyệt) để hiển thị ngoài trang chủ
    /// </summary>
    public async Task<PagedResult<ReviewDTO>> GetRecentReviewsAsync(int page = 1, int pageSize = 10)
    {
        var reviews = await _unitOfWork.Reviews
            .GetAllAsync(
                predicate: r => r.Status == "approved",
                includeProperties: new[] { "User", "Movie" },
                orderBy: r => r.OrderByDescending(x => x.CreatedAt)
            );

        var list = reviews.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var total = reviews.Count();

        return new PagedResult<ReviewDTO>
        {
            Items = _mapper.Map<List<ReviewDTO>>(list),
            CurrentPage = page,
            TotalPages = total == 0 ? 1 : (int)Math.Ceiling((double)total / pageSize),
            PageSize = pageSize,
            TotalCount = total
        };
    }

    /// <summary>
    /// Cập nhật thông tin đánh giá (điểm số, nội dung)
    /// </summary>
    public async Task<ReviewDTO> UpdateReviewAsync(Guid reviewId, CreateReviewDTO reviewDTO)
    {
        var review = await _unitOfWork.Reviews
            .FirstOrDefaultAsync(r => r.Id == reviewId);
        
        if (review == null)
            throw new KeyNotFoundException($"Không tìm thấy đánh giá với ID: {reviewId}");

        review.Rating = reviewDTO.Rating;
        review.Comment = reviewDTO.Comment;

        _unitOfWork.Reviews.Update(review);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<ReviewDTO>(review);
    }

    /// <summary>
    /// Xóa một đánh giá khỏi hệ thống
    /// </summary>
    public async Task DeleteReviewAsync(Guid reviewId)
    {
        var review = await _unitOfWork.Reviews
            .FirstOrDefaultAsync(r => r.Id == reviewId);
        
        if (review == null)
            throw new KeyNotFoundException($"Không tìm thấy đánh giá với ID: {reviewId}");

        _unitOfWork.Reviews.Remove(review);
        await _unitOfWork.SaveChangesAsync();
    }
}
