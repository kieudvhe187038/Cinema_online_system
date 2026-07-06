using AutoMapper;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;

namespace Cinema_System.Application.Services
{
    // Tầng Application: xử lý nghiệp vụ đặt vé, truy cập DB qua IUnitOfWork.
    public class BookingService : IBookingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public BookingService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        // Lấy lịch sử đặt vé của user, kèm Phim (qua Showtime) + danh sách vé (để đếm ghế)
        public async Task<List<BookingHistoryDto>> GetBookingHistoryAsync(Guid userId)
        {
            var bookings = await _unitOfWork.Bookings.GetAllAsync(
                predicate: b => b.UserId == userId,
                includeProperties: new[] { "Showtime.Movie", "Tickets" }, // JOIN nạp sẵn
                orderBy: q => q.OrderByDescending(b => b.CreatedAt));     // mới nhất lên đầu

            return bookings.Select(b => _mapper.Map<BookingHistoryDto>(b)).ToList();
        }

        // Chi tiết đơn đặt vé. Lọc theo cả userId để KHÔNG cho xem đơn của người khác.
        public async Task<BookingDetailDto?> GetBookingDetailAsync(Guid bookingId, Guid userId)
        {
            var booking = await _unitOfWork.Bookings.FirstOrDefaultAsync(
                b => b.Id == bookingId && b.UserId == userId,
                includeProperties: new[]
                {
                "Showtime.Movie", "Showtime.Room",
                "Tickets.Seat.SeatType", "BookingFoods.Fb"
                });
            if (booking == null) return null;

            return _mapper.Map<BookingDetailDto>(booking);
        }
    }
}
