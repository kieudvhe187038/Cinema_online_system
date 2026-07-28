using AutoMapper;
using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;
using Cinema_System.Domain.Entities;
using Microsoft.EntityFrameworkCore;

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
        var (items, totalCount) = await _unitOfWork.Reviews.GetPagedAsync(
            page,
            pageSize,
            predicate: r => r.MovieId == movieId && r.Status == ReviewStatus.Approved,
            include: q => q.Include(r => r.User).Include(r => r.Movie),
            orderBy: q => q.OrderByDescending(x => x.CreatedAt));

        return new PagedResult<ReviewDTO>
        {
            Items = _mapper.Map<List<ReviewDTO>>(items),
            CurrentPage = page,
            TotalPages = totalCount == 0 ? 1 : (int)Math.Ceiling((double)totalCount / pageSize),
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    /// <summary>
    /// Kiểm tra xem người dùng đã xem phim này chưa (đã mua vé và thanh toán thành công hay chưa)
    /// </summary>
    public async Task<bool> HasUserWatchedMovieAsync(Guid userId, Guid movieId)
    {
        return await _unitOfWork.Tickets.ExistsAsync(t =>
            t.Booking.UserId == userId &&
            t.Booking.PaymentStatus == "Paid" &&
            t.Showtime.MovieId == movieId);
    }

    /// <summary>
    /// Lấy tập ID các phim mà người dùng đã xem, trong 1 truy vấn duy nhất (thay vì kiểm tra từng phim một).
    /// </summary>
    public async Task<HashSet<Guid>> GetWatchedMovieIdsAsync(Guid userId)
    {
        var tickets = await _unitOfWork.Tickets.GetAllAsync(
            predicate: t => t.Booking.UserId == userId && t.Booking.PaymentStatus == "Paid",
            include: q => q.Include(t => t.Showtime));

        return tickets.Select(t => t.Showtime.MovieId).ToHashSet();
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
    /// Tạo mới một đánh giá phim ở trạng thái được phê duyệt ngay lập tức (status = approved)
    /// </summary>
    public async Task<ReviewDTO> CreateReviewAsync(Guid userId, CreateReviewDTO reviewDTO)
    {
        // Kiểm tra lại lần cuối để tránh race condition / submit 2 lần
        var alreadyReviewed = await _unitOfWork.Reviews
            .ExistsAsync(r => r.UserId == userId && r.MovieId == reviewDTO.MovieId);

        if (alreadyReviewed)
            throw new InvalidOperationException("Bạn đã đánh giá phim này rồi.");

        var review = new Review
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            MovieId = reviewDTO.MovieId,
            Rating = reviewDTO.Rating,
            Comment = reviewDTO.Comment,
            Status = ReviewStatus.Approved, // Khớp với HasDefaultValue("Approved") trong DbContext
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
        var (items, totalCount) = await _unitOfWork.Reviews.GetPagedAsync(
            page,
            pageSize,
            predicate: r => r.Status == ReviewStatus.Approved,
            include: q => q.Include(r => r.User).Include(r => r.Movie),
            orderBy: q => q.OrderByDescending(x => x.CreatedAt));

        return new PagedResult<ReviewDTO>
        {
            Items = _mapper.Map<List<ReviewDTO>>(items),
            CurrentPage = page,
            TotalPages = totalCount == 0 ? 1 : (int)Math.Ceiling((double)totalCount / pageSize),
            PageSize = pageSize,
            TotalCount = totalCount
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
