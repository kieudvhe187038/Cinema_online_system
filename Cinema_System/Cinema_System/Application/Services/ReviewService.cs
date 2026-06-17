using AutoMapper;
using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Services;

public class ReviewService : IReviewService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ReviewService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

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
            TotalPages = (int)Math.Ceiling((double)total / pageSize),
            PageSize = pageSize
        };
    }

    public async Task<bool> HasUserWatchedMovieAsync(Guid userId, Guid movieId)
    {
        // Lấy tất cả booking của user đã thanh toán
        var bookings = await _unitOfWork.Bookings
            .GetAllAsync(
                predicate: b => b.UserId == userId && b.PaymentStatus == "completed"
            );

        if (!bookings.Any())
            return false;

        // Kiểm tra xem booking nào có ticket cho phim này
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

    public async Task<bool> HasUserReviewedMovieAsync(Guid userId, Guid movieId)
    {
        return await _unitOfWork.Reviews
            .ExistsAsync(r => r.UserId == userId && r.MovieId == movieId);
    }

    public async Task<ReviewDTO> CreateReviewAsync(Guid userId, CreateReviewDTO reviewDTO)
    {
        var review = new Review
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            MovieId = reviewDTO.MovieId,
            Rating = reviewDTO.Rating,
            Comment = reviewDTO.Comment,
            Status = "pending",
            CreatedAt = DateTime.Now
        };

        await _unitOfWork.Reviews.AddAsync(review);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<ReviewDTO>(review);
    }

    public async Task<ReviewDTO?> GetReviewByIdAsync(Guid reviewId)
    {
        var review = await _unitOfWork.Reviews
            .FirstOrDefaultAsync(
                predicate: r => r.Id == reviewId,
                includeProperties: new[] { "User", "Movie" }
            );

        return review != null ? _mapper.Map<ReviewDTO>(review) : null;
    }

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
            TotalPages = (int)Math.Ceiling((double)total / pageSize),
            PageSize = pageSize
        };
    }

    public async Task<ReviewDTO> UpdateReviewAsync(Guid reviewId, CreateReviewDTO reviewDTO)
    {
        var review = await _unitOfWork.Reviews
            .FirstOrDefaultAsync(r => r.Id == reviewId);
        
        if (review == null)
            throw new KeyNotFoundException($"Review {reviewId} not found");

        review.Rating = reviewDTO.Rating;
        review.Comment = reviewDTO.Comment;

        _unitOfWork.Reviews.Update(review);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<ReviewDTO>(review);
    }

    public async Task DeleteReviewAsync(Guid reviewId)
    {
        var review = await _unitOfWork.Reviews
            .FirstOrDefaultAsync(r => r.Id == reviewId);
        
        if (review == null)
            throw new KeyNotFoundException($"Review {reviewId} not found");

        _unitOfWork.Reviews.Remove(review);
        await _unitOfWork.SaveChangesAsync();
    }
}
