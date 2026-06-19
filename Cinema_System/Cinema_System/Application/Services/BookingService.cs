using AutoMapper;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;
using Cinema_System.Domain.Entities;

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

            return new BookingDetailDto
            {
                Id = booking.Id,
                MovieTitle = booking.Showtime.Movie.Title,
                PosterUrl = booking.Showtime.Movie.PosterUrl,
                AgeRating = booking.Showtime.Movie.AgeRating,
                DurationMinutes = booking.Showtime.Movie.DurationMinutes,
                StartTime = booking.Showtime.StartTime,
                EndTime = booking.Showtime.EndTime,
                RoomName = booking.Showtime.Room.Name,
                Seats = booking.Tickets
                    .OrderBy(t => t.Seat.RowNumber).ThenBy(t => t.Seat.SeatNumber)
                    .Select(t => new BookingSeatLineDto
                    {
                        SeatLabel = SeatLabel(t.Seat.RowNumber, t.Seat.SeatNumber),
                        SeatTypeName = t.Seat.SeatType.Name,
                        Price = t.PriceAtBooking
                    }).ToList(),
                Foods = booking.BookingFoods
                    .Select(f => new BookingFoodLineDto
                    {
                        Name = f.Fb.Name,
                        Quantity = f.Quantity,
                        Price = f.PriceAtBooking
                    }).ToList(),
                TotalAmount = booking.TotalAmount,
                DiscountAmount = booking.DiscountAmount,
                VatAmount = booking.VatAmount,
                FinalAmount = booking.FinalAmount,
                PaymentStatus = booking.PaymentStatus,
                BookingType = booking.BookingType,
                CreatedAt = booking.CreatedAt,
                QrCode = booking.QrCode
            };
        }

        // Đổi (hàng, số ghế) thành nhãn kiểu "A5": hàng 1->A, 2->B...
        private static string SeatLabel(int row, int num)
            => (row >= 1 && row <= 26 ? ((char)('A' + row - 1)).ToString() : row.ToString()) + num;
    }
}
