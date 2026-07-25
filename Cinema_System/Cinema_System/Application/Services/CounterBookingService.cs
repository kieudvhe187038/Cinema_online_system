using AutoMapper;
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

    // Số lượng tối đa cho mỗi loại món/nước trong 1 đơn (khớp giới hạn ở luồng đặt vé online - ShowtimeService.MaxFoodPerItem).
    private const int MaxQuantityPerFood = 20;

    // Số ghế tối đa cho 1 đơn tại quầy (khớp giới hạn MAX_SEATS phía luồng khách đặt online).
    private const int MaxSeatsPerBooking = 6;

    // Phương thức thanh toán hợp lệ tại quầy (khớp với UI: Tiền mặt / Chuyển khoản).
    // Chặn giá trị tùy ý / quá dài tràn vào cột Payments.payment_method NVARCHAR(100).
    private static readonly HashSet<string> AllowedPaymentMethods =
        new(StringComparer.OrdinalIgnoreCase) { "Cash", "Transfer" };

    private readonly IUnitOfWork _unitOfWork;
    private readonly IPricingService _pricing;
    private readonly IStaffContextService _staffContext;
    private readonly IPointConfigService _pointConfig;
    private readonly IEmailService _email;
    private readonly IMapper _mapper;

    public CounterBookingService(
        IUnitOfWork unitOfWork,
        IPricingService pricing,
        IStaffContextService staffContext,
        IPointConfigService pointConfig,
        IEmailService email,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _pricing = pricing;
        _staffContext = staffContext;
        _pointConfig = pointConfig;
        _email = email;
        _mapper = mapper;
    }

    public async Task<CounterBookingViewModel> GetCounterDataAsync(Guid staffId)
    {
        var staff = await _staffContext.GetCurrentStaffAsync(staffId);
        var now = DateTime.Now;

        var showtimes = await _unitOfWork.Showtimes.GetUpcomingWithMovieAsync(now);

        var movies = _mapper.Map<List<MovieOptionDTO>>(
            showtimes
                .Where(s => s.Movie != null)
                .Select(s => s.Movie)
                .DistinctBy(m => m.Id)
                .OrderBy(m => m.Title)
                .ToList());

        var foods = _mapper.Map<List<FoodOptionDTO>>(
            await _unitOfWork.FoodBeverages.GetAllAsync(
                f => f.StockStatus != OutOfStock,
                orderBy: q => q.OrderBy(f => f.Name)));

        var vat = (await _unitOfWork.Vats.GetAllAsync(v => v.Status == StatusActive))
            .OrderByDescending(v => v.CreatedAt)
            .FirstOrDefault();

        var pointPolicy = await _pointConfig.GetPolicyAsync();

        return new CounterBookingViewModel
        {
            Movies = movies,
            Foods = foods,
            ActingStaffName = staff?.FullName ?? "(chưa có nhân viên)",
            HasStaff = staff is not null,
            VatRate = vat?.VatRate ?? 0m,
            PointValueVnd = pointPolicy.PointValueVnd
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

    public async Task<SeatMapDTO?> GetSeatMapAsync(Guid showtimeId, Guid staffId, Guid holdToken)
    {
        var showtime = await _unitOfWork.Showtimes.GetWithRoomAndMovieAsync(showtimeId);
        if (showtime is null) return null;

        var seats = await _unitOfWork.Seats.GetByRoomWithTypeAsync(showtime.RoomId);

        // Ghế chính tab quầy này đang tự giữ thì KHÔNG tính là "đã chiếm" (vẫn phải hiện chọn được/đang chọn).
        var occupied = await GetOccupiedSeatIdsAsync(showtimeId, new HoldOwner(staffId, holdToken));
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
                IsOccupied = occupied.Contains(s.Id) || !available,
                IsHeld = occupied.Held.Contains(s.Id)
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

    public async Task<Result<Guid>> CreateAsync(CounterBookingRequest request, Guid staffId)
    {
        var staff = await _staffContext.GetCurrentStaffAsync(staffId);
        if (staff is null)
            return Result<Guid>.Failure("Chưa có tài khoản nhân viên (Staff) để ghi nhận đơn.");

        if (request.SeatIds is null || request.SeatIds.Count == 0)
            return Result<Guid>.Failure("Vui lòng chọn ít nhất một ghế.");
        if (request.SeatIds.Distinct().Count() > MaxSeatsPerBooking)
            return Result<Guid>.Failure($"Mỗi đơn tối đa {MaxSeatsPerBooking} ghế.");

        var showtime = await _unitOfWork.Showtimes.GetWithRoomAndMovieAsync(request.ShowtimeId);
        if (showtime is null)
            return Result<Guid>.Failure("Không tìm thấy suất chiếu.");
        // Chỉ cho đặt suất còn lịch chiếu và chưa bắt đầu (khớp filter ở danh sách suất chiếu):
        // suất ở quá khứ, đang chiếu (Live) hay đã hủy đều bị chặn.
        if (!ShowtimeSalePolicy.IsOpenForSale(showtime.StartTime, showtime.Status, DateTime.Now))
            return Result<Guid>.Failure(ShowtimeSalePolicy.ClosedMessage);

        var seatIds = request.SeatIds.Distinct().ToList();
        var seats = await _unitOfWork.Seats.GetByIdsWithTypeAsync(seatIds);

        if (seats.Count != seatIds.Count)
            return Result<Guid>.Failure("Một số ghế không hợp lệ.");
        if (seats.Any(s => s.RoomId != showtime.RoomId))
            return Result<Guid>.Failure("Ghế không thuộc phòng của suất chiếu.");
        if (seats.Any(s => !string.Equals(s.Status, SeatAvailable, StringComparison.OrdinalIgnoreCase)))
            return Result<Guid>.Failure("Một số ghế đang bảo trì/không sử dụng.");

        // Loại trừ hold của chính tab quầy này — đó là ghế họ vừa tự giữ trong lúc chọn, không phải bị người
        // khác chiếm. Thiếu chỗ này thì CHÍNH hold của nhân viên sẽ tự chặn luôn đơn của họ ở bước tạo vé.
        var owner = new HoldOwner(staffId, request.HoldToken);
        var occupied = await GetOccupiedSeatIdsAsync(showtime.Id, owner);
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

        if (mergedFoods.Any(f => f.Quantity > MaxQuantityPerFood))
            return Result<Guid>.Failure($"Số lượng mỗi món/nước tối đa {MaxQuantityPerFood}.");

        if (mergedFoods.Count > 0)
        {
            var fbIds = mergedFoods.Select(f => f.FbId).ToList();
            var fbs = (await _unitOfWork.FoodBeverages.GetAllAsync(f => fbIds.Contains(f.Id)))
                .ToDictionary(f => f.Id);

            foreach (var item in mergedFoods)
            {
                if (!fbs.TryGetValue(item.FbId, out var fb))
                    return Result<Guid>.Failure("Món ăn/thức uống không hợp lệ.");
                // Đồ ăn có thể vừa hết hàng trong lúc nhân viên đang chọn (danh sách chỉ lọc lúc tải trang) -> check lại lúc chốt đơn.
                if (fb.StockStatus == OutOfStock)
                    return Result<Guid>.Failure($"{fb.Name} vừa hết hàng, vui lòng bỏ khỏi đơn.");

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

        // --- Khách hàng (thành viên hoặc khách lẻ) — xác định TRƯỚC vì mã giảm/điểm thưởng phụ thuộc vào đây ---
        var customer = await ResolveCustomerAsync(request.CustomerId, request.CustomerPhone);

        // --- VAT & tổng tiền trước giảm giá ---
        var vatEntity = (await _unitOfWork.Vats.GetAllAsync(v => v.Status == StatusActive))
            .OrderByDescending(v => v.CreatedAt)
            .FirstOrDefault();
        var totalAmount = ticketsTotal + foodsTotal;
        var vatAmount = vatEntity is null
            ? 0m
            : Math.Round(totalAmount * vatEntity.VatRate / 100m, 2);
        var grossTotal = totalAmount + vatAmount;

        // --- Áp mã khuyến mãi (validate lại server-side dù đã xem trước qua PreviewPromoAsync) ---
        var (promo, promoDiscount, promoError) = await ResolvePromoAsync(
            request.PromoCode, customer?.Id, ticketsTotal, foodsTotal, grossTotal);
        if (promoError != null) return Result<Guid>.Failure(promoError);
        var afterPromo = grossTotal - promoDiscount;

        // --- Dùng điểm thưởng của thành viên để giảm tiếp trên phần còn lại (khách lẻ không có điểm để dùng) ---
        var pointPolicy = await _pointConfig.GetPolicyAsync();
        var availablePoints = customer?.RewardPoints ?? 0;
        var usePoints = customer is null
            ? 0
            : Math.Min(Math.Max(0, request.PointsUsed), pointPolicy.MaxUsablePoints(availablePoints, afterPromo));
        var pointsDiscount = pointPolicy.DiscountFor(usePoints);

        var discountAmount = promoDiscount + pointsDiscount;   // tổng giảm (mã + điểm) lưu vào Booking
        var finalAmount = afterPromo - pointsDiscount;

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
            PromotionId = promo?.Id,
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

        // --- Điểm thưởng: trừ điểm đã dùng (nếu có) + cộng điểm mới, gộp 1 lần cập nhật số dư
        // (khớp cách ShowtimeService.ConfirmBookingAsync xử lý cho luồng online) ---
        if (customer is not null)
        {
            if (usePoints > 0)
            {
                await _unitOfWork.RewardPointHistories.AddAsync(new RewardPointHistory
                {
                    Id = Guid.NewGuid(),
                    UserId = customer.Id,
                    BookingId = bookingId,
                    PointsChanged = -usePoints,
                    ActionType = "Redeemed",
                    Description = "Dùng điểm giảm giá khi đặt vé tại quầy",
                    CreatedAt = DateTime.Now
                });
            }

            var earnedPoints = finalAmount > 0 ? pointPolicy.PointsEarnedFor(finalAmount) : 0;
            if (earnedPoints > 0)
            {
                await _unitOfWork.RewardPointHistories.AddAsync(new RewardPointHistory
                {
                    Id = Guid.NewGuid(),
                    UserId = customer.Id,
                    BookingId = bookingId,
                    PointsChanged = earnedPoints,
                    ActionType = "Earned",
                    Description = $"Tích điểm từ đơn {booking.QrCode}",
                    CreatedAt = DateTime.Now
                });
            }

            if (usePoints > 0 || earnedPoints > 0)
            {
                customer.RewardPoints = (customer.RewardPoints ?? 0) - usePoints + earnedPoints;
                customer.UpdatedAt = DateTime.Now;
                _unitOfWork.Users.Update(customer);
            }
        }

        await _unitOfWork.Bookings.AddAsync(booking);
        foreach (var t in tickets) await _unitOfWork.Tickets.AddAsync(t);
        foreach (var bf in bookingFoods) await _unitOfWork.BookingFoods.AddAsync(bf);
        await _unitOfWork.Payments.AddAsync(payment);

        // Ghế tab quầy này đang tự giữ cho suất này đã thành vé -> chuyển hold sang Converted (khớp cách
        // luồng khách đặt online xử lý, xem ShowtimeService.ConfirmBookingAsync).
        var myHolds = (await _unitOfWork.SeatHolds.GetAllAsync(
            h => h.ShowtimeId == showtime.Id && h.UserId == staffId && h.Status == HoldHolding && seatIds.Contains(h.SeatId)))
            .Where(owner.Owns);
        foreach (var h in myHolds)
        {
            h.Status = "Converted";
            _unitOfWork.SeatHolds.Update(h);
        }

        // TrySaveChangesAsync trả về false khi vi phạm unique index (UX_Tickets_Showtime_Seat)
        // — backstop chống đặt trùng ghế khi có đơn khác chốt cùng lúc.
        if (!await _unitOfWork.TrySaveChangesAsync())
            return Result<Guid>.Failure("Một số ghế vừa được đặt bởi đơn khác. Vui lòng chọn lại ghế.");

        // Gửi email hóa đơn/vé cho khách nếu là thành viên có email — đơn đã lưu thành công nên lỗi
        // gửi email không được làm hỏng kết quả (bọc try/catch trong hàm gửi).
        if (customer is not null && !string.IsNullOrWhiteSpace(customer.Email))
            await SendCounterReceiptEmailAsync(customer, showtime, seats, booking.QrCode, method, finalAmount);

        return Result<Guid>.Success(bookingId);
    }

    // Gửi email hóa đơn/vé (kèm mã QR) cho khách mua tại quầy. Bọc try/catch: vé đã đặt xong nên không rollback dù email lỗi.
    private async Task SendCounterReceiptEmailAsync(User customer, Showtime showtime, IReadOnlyList<Seat> seats, string bookingQr, string method, decimal total)
    {
        try
        {
            var html = BuildCounterReceiptEmailHtml(customer, showtime, seats, bookingQr, method, total);
            await _email.SendAsync(customer.Email!, "CineStar - Hóa đơn mua vé tại quầy", html);
        }
        catch
        {
            // Nuốt lỗi gửi email: booking đã lưu thành công, không được để email làm hỏng kết quả.
        }
    }

    // Dựng nội dung HTML email hóa đơn tại quầy. Ảnh QR sinh qua dịch vụ ngoài (api.qrserver.com) để email hiển thị được.
    private static string BuildCounterReceiptEmailHtml(User customer, Showtime showtime, IReadOnlyList<Seat> seats, string bookingQr, string method, decimal total)
    {
        var name = string.IsNullOrWhiteSpace(customer.FullName) ? "bạn" : customer.FullName;
        var movie = showtime.Movie?.Title ?? string.Empty;
        var room = showtime.Room?.Name ?? string.Empty;
        var cinema = showtime.Room?.Cinema?.Name ?? string.Empty;
        var start = showtime.StartTime.ToString("HH:mm - dd/MM/yyyy");
        var seatLabels = string.Join(", ", seats.Select(s => SeatLabelHelper.Build(s.RowNumber, s.SeatNumber)));
        var totalText = total.ToString("#,##0") + "₫";
        var qrImg = "https://api.qrserver.com/v1/create-qr-code/?size=220x220&data=" + Uri.EscapeDataString(bookingQr);

        string Row(string label, string value) =>
            $@"<tr>
                 <td style=""padding:6px 0;color:#64748b;font-size:14px;"">{label}</td>
                 <td style=""padding:6px 0;color:#0f172a;font-size:14px;font-weight:600;text-align:right;"">{value}</td>
               </tr>";

        return $@"
<div style=""max-width:560px;margin:0 auto;font-family:Arial,Helvetica,sans-serif;background:#f8fafc;padding:24px;"">
  <div style=""background:#ffffff;border:1px solid #e2e8f0;border-radius:16px;overflow:hidden;"">
    <div style=""background:#001c3a;padding:20px 24px;"">
      <div style=""color:#f37021;font-size:22px;font-weight:800;letter-spacing:1px;"">CINESTAR</div>
      <div style=""color:#cbd5e1;font-size:13px;margin-top:2px;"">Hóa đơn mua vé tại quầy</div>
    </div>
    <div style=""padding:24px;"">
      <p style=""margin:0 0 16px;color:#0f172a;font-size:15px;"">Xin chào <b>{name}</b>, cảm ơn bạn đã mua vé tại CineStar. Đây là hóa đơn cho đơn hàng của bạn.</p>

      <div style=""text-align:center;margin:8px 0 20px;"">
        <img src=""{qrImg}"" alt=""Mã QR vé"" width=""200"" height=""200"" style=""border:1px solid #e2e8f0;border-radius:12px;padding:8px;background:#fff;"" />
        <div style=""margin-top:8px;color:#0f172a;font-size:16px;font-weight:700;letter-spacing:1px;"">{bookingQr}</div>
      </div>

      <table style=""width:100%;border-collapse:collapse;border-top:1px solid #e2e8f0;padding-top:8px;"">
        {Row("Phim", movie)}
        {Row("Suất chiếu", start)}
        {Row("Phòng", string.IsNullOrEmpty(cinema) ? room : $"{room} · {cinema}")}
        {Row("Ghế", seatLabels)}
        {Row("Phương thức", method)}
        {Row("Tổng thanh toán", totalText)}
      </table>
    </div>
    <div style=""background:#f1f5f9;padding:14px 24px;color:#94a3b8;font-size:12px;text-align:center;"">
      Email tự động từ hệ thống CineStar. Vui lòng không trả lời email này.
    </div>
  </div>
</div>";
    }

    /// <summary>Ghế đã có vé (chưa hủy — <see cref="Booked"/>) và ghế đang được giữ chỗ còn hiệu lực (<see cref="Held"/>),
    /// tách riêng để UI phân biệt "đã bán" với "đang giữ tạm".</summary>
    private readonly record struct OccupiedSeats(HashSet<Guid> Booked, HashSet<Guid> Held)
    {
        public bool Contains(Guid seatId) => Booked.Contains(seatId) || Held.Contains(seatId);
        public int Count => Booked.Union(Held).Count();
    }

    /// <summary>Chủ sở hữu một lệnh giữ ghế ở quầy: tài khoản nhân viên + phiên (tab/máy quầy) của họ.
    /// Nhiều máy quầy thường dùng CHUNG 1 tài khoản Staff nên chỉ so <see cref="StaffId"/> là chưa đủ —
    /// phải kèm <see cref="Token"/> (mỗi tab 1 GUID riêng) thì máy này mới chặn được máy kia.</summary>
    private readonly record struct HoldOwner(Guid StaffId, Guid Token)
    {
        /// <summary>Hold này có phải do chính phiên quầy hiện tại tạo ra không?
        /// Token rỗng (trang cũ chưa gửi token / dòng hold có trước migration hold_token) → quay về
        /// đối chiếu theo tài khoản như trước, để không làm hỏng luồng đang chạy.</summary>
        public bool Owns(SeatHold hold)
            => hold.UserId == StaffId
               && (Token == Guid.Empty || hold.HoldToken == null || hold.HoldToken == Token);
    }

    /// <summary>Tập hợp ghế đã có vé (chưa hủy) hoặc đang được giữ chỗ còn hiệu lực.
    /// <paramref name="owner"/>: bỏ qua hold của chính phiên quầy này (đang tự giữ, không tính là chiếm chỗ).</summary>
    private async Task<OccupiedSeats> GetOccupiedSeatIdsAsync(Guid showtimeId, HoldOwner? owner = null)
    {
        var now = DateTime.Now;

        var ticketSeatIds = (await _unitOfWork.Tickets.GetAllAsync(
            t => t.ShowtimeId == showtimeId && t.Status != TicketCancelled))
            .Select(t => t.SeatId).ToHashSet();

        // Lọc "hold của chính mình" ở bộ nhớ (tập hold 1 suất tối đa bằng số ghế của phòng): so sánh
        // 2 vế user + token với cột nullable dịch sang SQL rất dễ sai ngữ nghĩa NULL.
        var holds = await _unitOfWork.SeatHolds.GetAllAsync(
            h => h.ShowtimeId == showtimeId && h.Status == HoldHolding && h.ExpiresAt > now);

        var holdSeatIds = holds
            .Where(h => owner is null || !owner.Value.Owns(h))
            .Select(h => h.SeatId).ToHashSet();

        return new OccupiedSeats(ticketSeatIds, holdSeatIds);
    }

    // Giữ 1 ghế cho nhân viên trong holdMinutes phút (tạo mới hoặc gia hạn nếu đã giữ) — cùng cơ chế
    // với luồng khách đặt online (ShowtimeService.HoldSeatAsync), áp cho quầy để tránh 2 nhân viên
    // (hoặc nhân viên và khách online) cùng chọn trùng 1 ghế trong lúc đang nhập thông tin đơn.
    // holdToken: phiên giữ ghế của tab/máy quầy đang thao tác — 2 máy dùng chung tài khoản Staff vẫn chặn nhau.
    public async Task<Result> HoldSeatAsync(Guid showtimeId, Guid seatId, Guid staffId, Guid holdToken, int holdMinutes)
    {
        var now = DateTime.Now;
        var owner = new HoldOwner(staffId, holdToken);

        var showtime = await _unitOfWork.Showtimes.GetByIdAsync(showtimeId);
        if (showtime is null) return Result.Failure("Không tìm thấy suất chiếu.");
        if (!ShowtimeSalePolicy.IsOpenForSale(showtime.StartTime, showtime.Status, now))
            return Result.Failure(ShowtimeSalePolicy.ClosedMessage);

        var booked = await _unitOfWork.Tickets.ExistsAsync(
            t => t.ShowtimeId == showtimeId && t.SeatId == seatId && t.Status != TicketCancelled);
        if (booked) return Result.Failure("Ghế đã được đặt.");

        // Lấy MỌI dòng còn mang trạng thái "Holding" của ghế này, KỂ CẢ đã hết hạn: hold hết hạn không tự
        // đổi trạng thái (không có job dọn), nên vẫn chiếm chỗ trong unique index UX_SeatHolds_Active
        // (ShowtimeId, SeatId) WHERE status='Holding'. Chèn thêm dòng 'Holding' mới sẽ vi phạm index.
        var holds = (await _unitOfWork.SeatHolds.GetAllAsync(
            predicate: h => h.ShowtimeId == showtimeId && h.SeatId == seatId
                && h.Status == HoldHolding)).ToList();

        // Chỉ hold CÒN HẠN của phiên khác mới thực sự chặn; hold đã hết hạn thì ai giữ trước cũng không tính.
        // "Phiên khác" gồm cả tab/máy quầy khác đang dùng CHUNG tài khoản Staff này (khác holdToken).
        if (holds.Any(h => !owner.Owns(h) && h.ExpiresAt > now))
            return Result.Failure("Ghế đang được người khác giữ.");

        // Nhờ unique index nên nhiều nhất 1 dòng — tái sử dụng để gia hạn (hold của mình) hoặc
        // tiếp quản (hold đã hết hạn, kể cả của người khác) thay vì chèn dòng mới gây trùng index.
        var existing = holds.FirstOrDefault();
        if (existing != null)
        {
            existing.UserId = staffId;
            existing.HoldToken = holdToken == Guid.Empty ? null : holdToken;
            existing.HeldAt = now;
            existing.ExpiresAt = now.AddMinutes(holdMinutes);
            _unitOfWork.SeatHolds.Update(existing);
        }
        else
        {
            await _unitOfWork.SeatHolds.AddAsync(new SeatHold
            {
                Id = Guid.NewGuid(),
                ShowtimeId = showtimeId,
                SeatId = seatId,
                UserId = staffId,
                HoldToken = holdToken == Guid.Empty ? null : holdToken,
                HeldAt = now,
                ExpiresAt = now.AddMinutes(holdMinutes),
                Status = HoldHolding
            });
        }

        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    // Bỏ giữ 1 ghế (nhân viên bỏ chọn) — chỉ nhả hold do CHÍNH tab quầy này giữ.
    public async Task ReleaseSeatAsync(Guid showtimeId, Guid seatId, Guid staffId, Guid holdToken)
    {
        var owner = new HoldOwner(staffId, holdToken);
        var holds = await _unitOfWork.SeatHolds.GetAllAsync(
            predicate: h => h.ShowtimeId == showtimeId && h.SeatId == seatId
                && h.UserId == staffId && h.Status == HoldHolding);
        await ReleaseAsync(holds.Where(owner.Owns));
    }

    // Bỏ giữ toàn bộ ghế tab quầy này đang giữ của 1 suất (đổi suất chiếu khác / rời trang).
    // KHÔNG đụng tới ghế các tab/máy quầy khác đang giữ dù chung tài khoản Staff.
    public async Task ReleaseAllAsync(Guid showtimeId, Guid staffId, Guid holdToken)
    {
        var owner = new HoldOwner(staffId, holdToken);
        var holds = await _unitOfWork.SeatHolds.GetAllAsync(
            predicate: h => h.ShowtimeId == showtimeId && h.UserId == staffId && h.Status == HoldHolding);
        await ReleaseAsync(holds.Where(owner.Owns));
    }

    private async Task ReleaseAsync(IEnumerable<SeatHold> holds)
    {
        foreach (var h in holds)
        {
            h.Status = "Released";
            _unitOfWork.SeatHolds.Update(h);
        }
        await _unitOfWork.SaveChangesAsync();
    }

    // Tra khách hàng của đơn quầy: ưu tiên Id đã tra cứu, không có thì tìm theo SĐT nhân viên gõ.
    // Bỏ mọi khoảng trắng như MemberService.LookupByPhoneAsync để khớp với SĐT lưu trong DB kể cả
    // khi nhân viên gõ trực tiếp có khoảng trắng giữa (không qua bước tra cứu thành viên trước).
    // Dùng chung cho CẢ xem trước mã lẫn lúc chốt đơn để hai bước không lệch kết quả.
    private async Task<User?> ResolveCustomerAsync(Guid? customerId, string? customerPhone)
    {
        if (customerId is Guid id)
            return await _unitOfWork.Users.GetByIdAsync(id);

        if (string.IsNullOrWhiteSpace(customerPhone))
            return null;

        var normalizedPhone = new string(customerPhone.Where(c => !char.IsWhiteSpace(c)).ToArray());
        return normalizedPhone.Length == 0
            ? null
            : await _unitOfWork.Users.GetByPhoneAsync(normalizedPhone);
    }

    // Kiểm tra mã khuyến mãi và tính số tiền giảm (áp SAU VAT, trên tổng đã gồm thuế) — cùng quy tắc với
    // luồng khách đặt online (ShowtimeService.ResolvePromoAsync).
    private async Task<(Promotion? Promo, decimal Discount, string? Error)> ResolvePromoAsync(
        string? code, Guid? customerId, decimal seatTotal, decimal foodTotal, decimal grandTotal)
    {
        if (string.IsNullOrWhiteSpace(code)) return (null, 0m, null);
        code = code.Trim();

        // Bắt buộc có tài khoản khách hàng mới được dùng mã. Khách lẻ không có định danh nên
        // không chống được việc dùng lại mã nhiều lần — chặn ngay từ đầu thay vì bỏ qua check.
        if (customerId is null)
            return (null, 0m, "Cần xác minh tài khoản khách hàng (tra cứu theo số điện thoại) mới dùng được mã giảm giá.");

        var promo = (await _unitOfWork.Promotions.GetAllAsync(predicate: p => p.Code == code)).FirstOrDefault();
        if (promo is null) return (null, 0m, "Mã giảm giá không tồn tại.");
        if (promo.Status != StatusActive) return (null, 0m, "Mã giảm giá không còn hiệu lực.");

        var now = DateTime.Now;
        if (now < promo.ValidFrom || now > promo.ValidTo)
            return (null, 0m, "Mã giảm giá đã hết hạn hoặc chưa tới ngày áp dụng.");

        var subtotal = seatTotal + foodTotal;
        if (promo.MinOrderValue.HasValue && subtotal < promo.MinOrderValue.Value)
            return (null, 0m, $"Cần đơn tối thiểu {promo.MinOrderValue.Value:N0}₫ để dùng mã này.");

        var usedByCustomer = await _unitOfWork.Bookings.CountAsync(
            b => b.PromotionId == promo.Id && b.UserId == customerId.Value);
        if (usedByCustomer >= 1) return (null, 0m, "Khách hàng đã sử dụng mã giảm giá này rồi.");

        if (promo.UsageLimit.HasValue)
        {
            var used = await _unitOfWork.Bookings.CountAsync(b => b.PromotionId == promo.Id);
            if (used >= promo.UsageLimit.Value) return (null, 0m, "Mã giảm giá đã hết lượt sử dụng.");
        }

        decimal target = promo.ApplicableTarget switch
        {
            "Ticket_Only" => seatTotal,
            "Food_Only" => foodTotal,
            _ => grandTotal
        };
        if (target <= 0) return (null, 0m, "Mã không áp dụng cho các mặt hàng trong đơn.");

        var discount = promo.DiscountType == "Percent"
            ? target * (promo.DiscountAmount / 100m)
            : promo.DiscountAmount;

        if (promo.MaxDiscountAmount.HasValue)
            discount = Math.Min(discount, promo.MaxDiscountAmount.Value);

        discount = Math.Min(discount, target);
        discount = Math.Round(discount, 0, MidpointRounding.AwayFromZero);
        if (discount <= 0) return (null, 0m, "Mã không tạo ra khoản giảm cho đơn này.");

        return (promo, discount, null);
    }

    // Xem trước áp mã khuyến mãi (AJAX) trước khi chốt đơn — dùng tạm tính vé/đồ ăn client đã tính sẵn
    // (giống cách preview không cần "giữ ghế" phía online vì quầy không có bước riêng theo phiên).
    public async Task<CounterPromoPreviewDTO> PreviewPromoAsync(
        string code, Guid? customerId, string? customerPhone, decimal seatTotal, decimal foodTotal)
    {
        // Tra khách hàng ĐÚNG như lúc chốt đơn: nhân viên có thể mới gõ SĐT mà chưa bấm "Tra",
        // nếu xem trước bỏ qua SĐT thì sẽ báo hợp lệ rồi lúc thanh toán mới báo lỗi.
        var customer = await ResolveCustomerAsync(customerId, customerPhone);

        var subtotal = seatTotal + foodTotal;
        var vatEntity = (await _unitOfWork.Vats.GetAllAsync(v => v.Status == StatusActive))
            .OrderByDescending(v => v.CreatedAt)
            .FirstOrDefault();
        var vatAmount = vatEntity is null ? 0m : Math.Round(subtotal * vatEntity.VatRate / 100m, 2);
        var grandTotal = subtotal + vatAmount;

        var (promo, discount, error) = await ResolvePromoAsync(code, customer?.Id, seatTotal, foodTotal, grandTotal);
        if (error != null || promo is null)
            return new CounterPromoPreviewDTO { Ok = false, Message = error ?? "Mã giảm giá không hợp lệ." };

        return new CounterPromoPreviewDTO
        {
            Ok = true,
            Code = promo.Code,
            Discount = discount,
            Message = $"Áp dụng mã {promo.Code}: giảm {discount:N0}₫."
        };
    }

    private static string NewCode(string prefix)
        => prefix + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
}
