using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Services;

public class CounterBookingService : ICounterBookingService
{
    private const string StatusActive = "Active";
    private const string SeatAvailable = "Available";
    private const string HoldHolding = "Holding";
    private const string TicketCancelled = "Cancelled";
    private const string OutOfStock = "Out of Stock";
    private const string ShowtimeScheduled = "Scheduled";

    // Phương thức thanh toán hợp lệ tại quầy (khớp với UI: Tiền mặt / Chuyển khoản).
    // Chặn giá trị tùy ý / quá dài tràn vào cột Payments.payment_method NVARCHAR(100).
    private static readonly HashSet<string> AllowedPaymentMethods =
        new(StringComparer.OrdinalIgnoreCase) { "Cash", "Transfer" };

    private readonly IUnitOfWork _unitOfWork;
    private readonly IPricingService _pricing;
    private readonly IStaffContextService _staffContext;
    private readonly IPointConfigService _pointConfig;

    public CounterBookingService(
        IUnitOfWork unitOfWork,
        IPricingService pricing,
        IStaffContextService staffContext,
        IPointConfigService pointConfig)
    {
        _unitOfWork = unitOfWork;
        _pricing = pricing;
        _staffContext = staffContext;
        _pointConfig = pointConfig;
    }

    public async Task<CounterBookingViewModel> GetCounterDataAsync()
    {
        var staff = await _staffContext.GetCurrentStaffAsync();
        var now = DateTime.Now;

        var showtimes = await _unitOfWork.Showtimes.GetUpcomingWithMovieAsync(now);

        var movies = showtimes
            .Where(s => s.Movie != null)
            .Select(s => s.Movie)
            .DistinctBy(m => m.Id)
            .OrderBy(m => m.Title)
            .Select(m => new MovieOptionDTO
            {
                Id = m.Id,
                Title = m.Title,
                DurationMinutes = m.DurationMinutes,
                AgeRating = m.AgeRating
            })
            .ToList();

        var foods = (await _unitOfWork.FoodBeverages.GetAllAsync(
            f => f.StockStatus != OutOfStock,
            orderBy: q => q.OrderBy(f => f.Name)))
            .Select(f => new FoodOptionDTO
            {
                Id = f.Id,
                Name = f.Name,
                Price = f.Price,
                ImageUrl = f.ImageUrl
            })
            .ToList();

        var vat = (await _unitOfWork.Vats.GetAllAsync(v => v.Status == StatusActive))
            .OrderByDescending(v => v.CreatedAt)
            .FirstOrDefault();

        return new CounterBookingViewModel
        {
            Movies = movies,
            Foods = foods,
            ActingStaffName = staff?.FullName ?? "(chưa có nhân viên)",
            HasStaff = staff is not null,
            VatRate = vat?.VatRate ?? 0m
        };
    }

    public async Task<IEnumerable<ShowtimeOptionDTO>> GetShowtimesAsync(Guid movieId)
    {
        var now = DateTime.Now;
        var showtimes = await _unitOfWork.Showtimes.GetUpcomingByMovieAsync(movieId, now);

        var result = new List<ShowtimeOptionDTO>();
        foreach (var s in showtimes)
        {
            var totalSeats = s.Room?.TotalSeats
                ?? await _unitOfWork.Seats.CountAsync(seat => seat.RoomId == s.RoomId);
            var occupied = await GetOccupiedSeatIdsAsync(s.Id);

            result.Add(new ShowtimeOptionDTO
            {
                Id = s.Id,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                RoomName = s.Room?.Name ?? string.Empty,
                CinemaName = s.Room?.Cinema?.Name ?? string.Empty,
                RoomType = s.Room?.RoomType?.Name ?? string.Empty,
                AvailableSeats = Math.Max(0, totalSeats - occupied.Count)
            });
        }

        return result;
    }

    public async Task<SeatMapDTO?> GetSeatMapAsync(Guid showtimeId)
    {
        var showtime = await _unitOfWork.Showtimes.GetWithRoomAndMovieAsync(showtimeId);
        if (showtime is null) return null;

        var seats = await _unitOfWork.Seats.GetByRoomWithTypeAsync(showtime.RoomId);

        var occupied = await GetOccupiedSeatIdsAsync(showtimeId);
        var pricing = await _pricing.GetPricingAsync(showtime);

        var cells = seats.Select(s =>
        {
            var available = string.Equals(s.Status, SeatAvailable, StringComparison.OrdinalIgnoreCase);
            return new SeatCellDTO
            {
                Id = s.Id,
                RowNumber = s.RowNumber,
                SeatNumber = s.SeatNumber,
                Label = SeatLabelHelper.Build(s.RowNumber, s.SeatNumber),
                SeatTypeName = s.SeatType?.Name ?? string.Empty,
                ColumnSpan = s.SeatType?.ColumnSpan ?? 1,
                Price = pricing.PriceForSeatType(s.SeatTypeId),
                Status = s.Status ?? string.Empty,
                IsOccupied = occupied.Contains(s.Id) || !available
            };
        }).ToList();

        return new SeatMapDTO
        {
            ShowtimeId = showtimeId,
            MovieTitle = showtime.Movie?.Title ?? string.Empty,
            RoomName = showtime.Room?.Name ?? string.Empty,
            StartTime = showtime.StartTime,
            TotalRows = showtime.Room?.TotalRow ?? (seats.Count > 0 ? seats.Max(x => x.RowNumber) : 0),
            TotalColumns = showtime.Room?.TotalColumns ?? (seats.Count > 0 ? seats.Max(x => x.SeatNumber) : 0),
            Seats = cells
        };
    }

    public async Task<Result<Guid>> CreateAsync(CounterBookingRequest request)
    {
        var staff = await _staffContext.GetCurrentStaffAsync();
        if (staff is null)
            return Result<Guid>.Failure("Chưa có tài khoản nhân viên (Staff) để ghi nhận đơn.");

        if (request.SeatIds is null || request.SeatIds.Count == 0)
            return Result<Guid>.Failure("Vui lòng chọn ít nhất một ghế.");

        var showtime = await _unitOfWork.Showtimes.GetWithRoomAndMovieAsync(request.ShowtimeId);
        if (showtime is null)
            return Result<Guid>.Failure("Không tìm thấy suất chiếu.");
        // Chỉ cho đặt suất còn lịch chiếu và chưa bắt đầu (khớp filter ở danh sách suất chiếu).
        if (!string.Equals(showtime.Status, ShowtimeScheduled, StringComparison.OrdinalIgnoreCase))
            return Result<Guid>.Failure("Suất chiếu không còn nhận đặt vé.");
        if (showtime.StartTime <= DateTime.Now)
            return Result<Guid>.Failure("Suất chiếu đã bắt đầu, không thể đặt vé.");

        var seatIds = request.SeatIds.Distinct().ToList();
        var seats = await _unitOfWork.Seats.GetByIdsWithTypeAsync(seatIds);

        if (seats.Count != seatIds.Count)
            return Result<Guid>.Failure("Một số ghế không hợp lệ.");
        if (seats.Any(s => s.RoomId != showtime.RoomId))
            return Result<Guid>.Failure("Ghế không thuộc phòng của suất chiếu.");
        if (seats.Any(s => !string.Equals(s.Status, SeatAvailable, StringComparison.OrdinalIgnoreCase)))
            return Result<Guid>.Failure("Một số ghế đang bảo trì/không sử dụng.");

        var occupied = await GetOccupiedSeatIdsAsync(showtime.Id);
        if (seats.Any(s => occupied.Contains(s.Id)))
            return Result<Guid>.Failure("Một số ghế đã được đặt hoặc đang được giữ. Vui lòng chọn lại.");

        // --- Tính giá vé ---
        var pricing = await _pricing.GetPricingAsync(showtime);
        var bookingId = Guid.NewGuid();
        decimal ticketsTotal = 0m;
        var tickets = new List<Ticket>();
        foreach (var seat in seats)
        {
            var price = pricing.PriceForSeatType(seat.SeatTypeId);
            ticketsTotal += price;
            tickets.Add(new Ticket
            {
                Id = Guid.NewGuid(),
                BookingId = bookingId,
                ShowtimeId = showtime.Id,
                SeatId = seat.Id,
                PriceAtBooking = price,
                QrCode = NewCode("TK"),
                Status = "Booked"
            });
        }

        // --- Đồ ăn / thức uống kèm theo ---
        decimal foodsTotal = 0m;
        var bookingFoods = new List<BookingFood>();
        var mergedFoods = (request.Foods ?? new())
            .Where(f => f.Quantity > 0)
            .GroupBy(f => f.FbId)
            .Select(g => new { FbId = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToList();

        if (mergedFoods.Count > 0)
        {
            var fbIds = mergedFoods.Select(f => f.FbId).ToList();
            var fbs = (await _unitOfWork.FoodBeverages.GetAllAsync(f => fbIds.Contains(f.Id)))
                .ToDictionary(f => f.Id);

            foreach (var item in mergedFoods)
            {
                if (!fbs.TryGetValue(item.FbId, out var fb))
                    return Result<Guid>.Failure("Món ăn/thức uống không hợp lệ.");

                foodsTotal += fb.Price * item.Quantity;
                bookingFoods.Add(new BookingFood
                {
                    Id = Guid.NewGuid(),
                    BookingId = bookingId,
                    FbId = fb.Id,
                    Quantity = item.Quantity,
                    PriceAtBooking = fb.Price
                });
            }
        }

        // --- VAT & tổng tiền ---
        var vatEntity = (await _unitOfWork.Vats.GetAllAsync(v => v.Status == StatusActive))
            .OrderByDescending(v => v.CreatedAt)
            .FirstOrDefault();
        var totalAmount = ticketsTotal + foodsTotal;
        var discountAmount = 0m;
        var vatAmount = vatEntity is null
            ? 0m
            : Math.Round(totalAmount * vatEntity.VatRate / 100m, 2);
        var finalAmount = totalAmount - discountAmount + vatAmount;

        // --- Khách hàng (thành viên hoặc khách lẻ) ---
        User? customer = null;
        if (request.CustomerId is Guid customerId)
            customer = await _unitOfWork.Users.GetByIdAsync(customerId);
        else if (!string.IsNullOrWhiteSpace(request.CustomerPhone))
        {
            customer = await _unitOfWork.Users.GetByPhoneAsync(request.CustomerPhone.Trim());
        }

        // --- Thanh toán tại quầy ---
        var method = string.IsNullOrWhiteSpace(request.PaymentMethod) ? "Cash" : request.PaymentMethod.Trim();
        if (!AllowedPaymentMethods.Contains(method))
            return Result<Guid>.Failure("Phương thức thanh toán không hợp lệ.");
        decimal? cashReceived = null;
        decimal? changeAmount = null;
        if (method.Equals("Cash", StringComparison.OrdinalIgnoreCase))
        {
            if (request.CashReceived is null || request.CashReceived < finalAmount)
                return Result<Guid>.Failure("Số tiền khách đưa không đủ để thanh toán.");
            cashReceived = request.CashReceived;
            changeAmount = request.CashReceived - finalAmount;
        }

        var booking = new Booking
        {
            Id = bookingId,
            ShowtimeId = showtime.Id,
            UserId = customer?.Id,
            StaffId = staff.Id,
            VatId = vatEntity?.Id,
            TotalAmount = totalAmount,
            DiscountAmount = discountAmount,
            VatAmount = vatAmount,
            FinalAmount = finalAmount,
            PaymentStatus = "Paid",
            BookingType = "Offline",
            CreatedAt = DateTime.Now,
            QrCode = NewCode("BK")
        };

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            BookingId = bookingId,
            PaymentMethod = method,
            PaymentSource = "Counter",
            Amount = finalAmount,
            CashReceived = cashReceived,
            ChangeAmount = changeAmount,
            Status = "Success",
            PaidAt = DateTime.Now
        };

        // --- Tích điểm cho thành viên ---
        if (customer is not null && finalAmount > 0)
        {
            var rate = (await _pointConfig.GetRateAsync()).Rate;
            var points = (int)Math.Floor(finalAmount * rate);
            if (points > 0)
            {
                customer.RewardPoints = (customer.RewardPoints ?? 0) + points;
                customer.UpdatedAt = DateTime.Now;
                _unitOfWork.Users.Update(customer);

                await _unitOfWork.RewardPointHistories.AddAsync(new RewardPointHistory
                {
                    Id = Guid.NewGuid(),
                    UserId = customer.Id,
                    BookingId = bookingId,
                    PointsChanged = points,
                    ActionType = "Earn",
                    Description = $"Tích điểm từ đơn {booking.QrCode}",
                    CreatedAt = DateTime.Now
                });
            }
        }

        await _unitOfWork.Bookings.AddAsync(booking);
        foreach (var t in tickets) await _unitOfWork.Tickets.AddAsync(t);
        foreach (var bf in bookingFoods) await _unitOfWork.BookingFoods.AddAsync(bf);
        await _unitOfWork.Payments.AddAsync(payment);

        // TrySaveChangesAsync trả về false khi vi phạm unique index (UX_Tickets_Showtime_Seat)
        // — backstop chống đặt trùng ghế khi có đơn khác chốt cùng lúc.
        if (!await _unitOfWork.TrySaveChangesAsync())
            return Result<Guid>.Failure("Một số ghế vừa được đặt bởi đơn khác. Vui lòng chọn lại ghế.");

        return Result<Guid>.Success(bookingId);
    }

    /// <summary>Tập hợp Id ghế đã có vé (chưa hủy) hoặc đang được giữ chỗ còn hiệu lực.</summary>
    private async Task<HashSet<Guid>> GetOccupiedSeatIdsAsync(Guid showtimeId)
    {
        var now = DateTime.Now;

        var ticketSeatIds = (await _unitOfWork.Tickets.GetAllAsync(
            t => t.ShowtimeId == showtimeId && t.Status != TicketCancelled))
            .Select(t => t.SeatId);

        var holdSeatIds = (await _unitOfWork.SeatHolds.GetAllAsync(
            h => h.ShowtimeId == showtimeId && h.Status == HoldHolding && h.ExpiresAt > now))
            .Select(h => h.SeatId);

        return ticketSeatIds.Concat(holdSeatIds).ToHashSet();
    }

    private static string NewCode(string prefix)
        => prefix + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
}
