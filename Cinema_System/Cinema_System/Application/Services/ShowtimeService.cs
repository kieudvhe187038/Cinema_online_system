using AutoMapper;
using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Cinema_System.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cinema_System.Application.Services;

// Truy vấn lịch chiếu cho giao diện công khai (mọi role, kể cả khách chưa đăng nhập).
public class ShowtimeService : IShowtimeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IPricingService _pricingService;
    private readonly IEmailService _email;
    private readonly IPointConfigService _pointConfig;

    // Số lượng tối đa cho mỗi loại đồ ăn trong một đơn.
    private const int MaxFoodPerItem = 20;

    // Nhận UnitOfWork + AutoMapper qua DI (mapping các mảnh phẳng; phần tính toán vẫn dựng tay).
    public ShowtimeService(IUnitOfWork unitOfWork, IMapper mapper, IPricingService pricingService,
        IEmailService email, IPointConfigService pointConfig)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _pricingService = pricingService;
        _email = email;
        _pointConfig = pointConfig;
    }

    // Lấy dữ liệu trang lịch chiếu: lọc theo phim/thể loại/độ tuổi/loại phòng + ngày + tùy chọn dropdown.
    public async Task<ShowtimePageViewModel> GetShowtimePageAsync(Guid? movieId, Guid? genreId, string? ageRating, Guid? roomTypeId, DateOnly? date)
    {
        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);

        // Chỉ cho xem lịch từ hiện tại đến tương lai: ép ngày đã chọn về hôm nay nếu nằm trong quá khứ.
        // autoPick = true khi vào trang mà chưa chỉ định ngày (vd bấm "Mua vé" từ trang chủ).
        var autoPick = date is null;
        var selectedDate = date ?? today;
        if (selectedDate < today) selectedDate = today;

        // Khi chưa chọn ngày: nhảy tới ngày gần nhất (trong phạm vi đặt trước) chắc chắn có suất chiếu
        // theo đúng bộ lọc hiện tại — để bấm "Mua vé" luôn thấy giờ chiếu thay vì trang trống.
        if (autoPick)
        {
            var windowEnd = today.AddDays(ShowtimeBrowsing.AdvanceDays).ToDateTime(TimeOnly.MinValue);
            var upcoming = await _unitOfWork.Showtimes.GetAllAsync(
                predicate: s =>
                    s.StartTime > now && s.StartTime < windowEnd &&
                    (movieId == null || s.MovieId == movieId) &&
                    (genreId == null || s.Movie.Genres.Any(g => g.Id == genreId)) &&
                    (ageRating == null || s.Movie.AgeRating == ageRating) &&
                    (roomTypeId == null || s.Room.RoomTypeId == roomTypeId) &&
                    s.Status == ShowtimeStatus.Scheduled,
                orderBy: q => q.OrderBy(s => s.StartTime));
            var first = upcoming.FirstOrDefault();
            if (first != null) selectedDate = DateOnly.FromDateTime(first.StartTime);
        }

        var dayEnd = selectedDate.ToDateTime(TimeOnly.MinValue).AddDays(1);
        // Mốc bắt đầu: nếu là hôm nay thì chỉ lấy suất từ thời điểm hiện tại trở đi (bỏ suất đã qua trong ngày).
        var lowerBound = selectedDate == today ? now : selectedDate.ToDateTime(TimeOnly.MinValue);

        // Suất chiếu trong ngày đã chọn (từ hiện tại trở đi), lọc thêm theo phim/thể loại/độ tuổi/loại phòng nếu có.
        // Chỉ lấy suất còn bán được: bỏ suất đã qua giờ, đang chiếu (Live), đã chiếu xong (Completed) và đã hủy.
        var showtimes = await _unitOfWork.Showtimes.GetAllAsync(
            predicate: s =>
                s.StartTime >= lowerBound && s.StartTime < dayEnd &&
                (movieId == null || s.MovieId == movieId) &&
                (genreId == null || s.Movie.Genres.Any(g => g.Id == genreId)) &&
                (ageRating == null || s.Movie.AgeRating == ageRating) &&
                (roomTypeId == null || s.Room.RoomTypeId == roomTypeId) &&
                s.Status == ShowtimeStatus.Scheduled,
            include: q => q
                .Include(s => s.Movie).ThenInclude(m => m.Genres)
                .Include(s => s.Room).ThenInclude(r => r.RoomType)
                .Include(s => s.Room).ThenInclude(r => r.Cinema),
            orderBy: q => q.OrderBy(s => s.StartTime).ThenBy(s => s.Movie.Title));

        var showtimeDtos = _mapper.Map<List<ShowtimeDTO>>(showtimes);

        // Tùy chọn dropdown: phim, thể loại, độ tuổi, loại phòng.
        var movies = await _unitOfWork.Movies.GetAllAsync(
            orderBy: q => q.OrderBy(m => m.Title));
        var genres = await _unitOfWork.Genres.GetAllAsync(
            orderBy: q => q.OrderBy(g => g.Name));
        var roomTypes = await _unitOfWork.RoomTypes.GetAllAsync(
            orderBy: q => q.OrderBy(rt => rt.Name));
        // Danh sách độ tuổi: lấy các giá trị khác null đang có trên phim.
        var ageRatings = movies
            .Where(m => !string.IsNullOrEmpty(m.AgeRating))
            .Select(m => m.AgeRating!)
            .Distinct()
            .OrderBy(a => a)
            .ToList();

        return new ShowtimePageViewModel
        {
            MovieId = movieId,
            GenreId = genreId,
            AgeRating = ageRating,
            RoomTypeId = roomTypeId,
            SelectedDate = selectedDate,
            Movies = movies.Select(m => new ShowtimeFilterOption { Id = m.Id, Name = m.Title }).ToList(),
            Genres = genres.Select(g => new ShowtimeFilterOption { Id = g.Id, Name = g.Name }).ToList(),
            AgeRatings = ageRatings,
            RoomTypes = roomTypes.Select(rt => new ShowtimeFilterOption { Id = rt.Id, Name = rt.Name }).ToList(),
            // Thanh chọn ngày: phạm vi đặt trước kể từ hôm nay (không có ngày quá khứ).
            AvailableDates = Enumerable.Range(0, ShowtimeBrowsing.AdvanceDays)
                .Select(i => today.AddDays(i))
                .ToList(),
            Showtimes = showtimeDtos
        };
    }

    // Lấy ngày + suất chiếu (trong phạm vi đặt trước, từ hiện tại) của một phim cho popup "đặt vé nhanh".
    public async Task<MovieShowtimesViewModel?> GetMovieShowtimesAsync(Guid movieId)
    {
        var movie = (await _unitOfWork.Movies.GetAllAsync(predicate: m => m.Id == movieId)).FirstOrDefault();
        if (movie is null) return null;

        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);
        var windowEnd = today.AddDays(ShowtimeBrowsing.AdvanceDays).ToDateTime(TimeOnly.MinValue);

        // Suất chiếu của phim trong phạm vi đặt trước; chỉ suất còn bán được (bỏ suất đã qua giờ,
        // đang chiếu, đã chiếu xong và đã hủy).
        var showtimes = await _unitOfWork.Showtimes.GetAllAsync(
            predicate: s => s.MovieId == movieId && s.StartTime > now && s.StartTime < windowEnd
                && s.Status == ShowtimeStatus.Scheduled,
            include: q => q
                .Include(s => s.Room).ThenInclude(r => r.RoomType)
                .Include(s => s.Room).ThenInclude(r => r.Cinema),
            orderBy: q => q.OrderBy(s => s.StartTime));

        var dtos = _mapper.Map<List<ShowtimeDTO>>(showtimes);

        return new MovieShowtimesViewModel
        {
            MovieId = movie.Id,
            MovieTitle = movie.Title,
            MoviePosterUrl = movie.PosterUrl,
            Days = dtos
                .GroupBy(s => DateOnly.FromDateTime(s.StartTime))
                .OrderBy(g => g.Key)
                .Select(g => new MovieShowtimeDay
                {
                    Date = g.Key,
                    Showtimes = g.OrderBy(s => s.StartTime).ThenBy(s => s.RoomName).ToList()
                })
                .ToList()
        };
    }

    // Chặn mọi thao tác đặt vé cho suất đã qua giờ / đang chiếu / đã hủy — dùng cho trang chọn ghế,
    // giữ ghế và xác nhận đơn (danh sách lịch chiếu đã lọc sẵn, nhưng trang có thể mở từ trước
    // hoặc vào bằng link trực tiếp).
    public async Task<Result> CheckSaleOpenAsync(Guid showtimeId)
    {
        var showtime = (await _unitOfWork.Showtimes.GetAllAsync(predicate: s => s.Id == showtimeId))
            .FirstOrDefault();
        if (showtime is null) return Result.Failure("Không tìm thấy suất chiếu.");

        return EnsureOpenForSale(showtime);
    }

    // Kiểm tra một suất chiếu đã tải sẵn có còn nhận đặt vé không.
    private static Result EnsureOpenForSale(Showtime showtime)
        => ShowtimeSalePolicy.IsOpenForSale(showtime.StartTime, showtime.Status, DateTime.Now)
            ? Result.Success()
            : Result.Failure(ShowtimeSalePolicy.ClosedMessage);

    // Kiểm tra khách có đủ tuổi đặt vé theo phân loại độ tuổi của phim (P/C13/C16/C18...).
    public async Task<Result> CheckAgeRestrictionAsync(Guid showtimeId, Guid userId)
    {
        var showtime = (await _unitOfWork.Showtimes.GetAllAsync(
            predicate: s => s.Id == showtimeId,
            include: q => q.Include(s => s.Movie))).FirstOrDefault();
        if (showtime is null) return Result.Success();   // suất không tồn tại -> để caller xử lý sau

        var minAge = MinAgeFromRating(showtime.Movie.AgeRating);
        if (minAge <= 0) return Result.Success();         // phim không giới hạn độ tuổi

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user is null) return Result.Success();

        var age = CalcAge(user.DateOfBirth, DateTime.Today);
        if (age < minAge)
            return Result.Failure($"Phim này được phân loại {showtime.Movie.AgeRating} (từ {minAge} tuổi trở lên). Bạn chưa đủ tuổi để đặt vé.");

        return Result.Success();
    }

    // Suy ra tuổi tối thiểu từ nhãn phân loại: lấy số trong nhãn (C13->13, C16->16, C18->18); không có số (P) -> 0.
    private static int MinAgeFromRating(string? rating)
    {
        if (string.IsNullOrWhiteSpace(rating)) return 0;
        var digits = new string(rating.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var n) ? n : 0;
    }

    // Tính tuổi (đủ năm) từ ngày sinh tới hôm nay.
    private static int CalcAge(DateOnly dob, DateTime today)
    {
        var t = DateOnly.FromDateTime(today);
        var age = t.Year - dob.Year;
        if (dob > t.AddYears(-age)) age--;   // chưa tới sinh nhật năm nay thì trừ 1
        return age;
    }

    // Lấy sơ đồ ghế cho 1 suất chiếu, đánh dấu trạng thái từng ghế.
    public async Task<SeatSelectionViewModel?> GetSeatSelectionAsync(Guid showtimeId, Guid? currentUserId = null)
    {
        // Thông tin suất chiếu kèm phim & phòng/rạp.
        var showtimeList = await _unitOfWork.Showtimes.GetAllAsync(
            predicate: s => s.Id == showtimeId,
            include: q => q
                .Include(s => s.Movie)
                .Include(s => s.Room).ThenInclude(r => r.Cinema));
        var showtime = showtimeList.FirstOrDefault();
        if (showtime is null)
            return null;

        // Ghế đã đặt (có vé chưa hủy) và ghế đang được giữ (chưa hết hạn) của suất này.
        var bookedTickets = await _unitOfWork.Tickets.GetAllAsync(
            predicate: t => t.ShowtimeId == showtimeId && t.Status != "Cancelled");
        var bookedSeatIds = bookedTickets.Select(t => t.SeatId).ToHashSet();

        // Ghế đang được giữ bởi NGƯỜI KHÁC (hold còn hiệu lực). Hold của chính user hiện tại không tính.
        var now = DateTime.Now;
        var activeHolds = await _unitOfWork.SeatHolds.GetAllAsync(
            predicate: h => h.ShowtimeId == showtimeId && h.ExpiresAt > now
                && h.Status == "Holding" && h.UserId != currentUserId);
        var heldSeatIds = activeHolds.Select(h => h.SeatId).ToHashSet();

        // Toàn bộ ghế của phòng, sắp theo hàng rồi số ghế.
        var seats = await _unitOfWork.Seats.GetAllAsync(
            predicate: s => s.RoomId == showtime.RoomId,
            include: q => q.Include(s => s.SeatType),
            orderBy: q => q.OrderBy(s => s.RowNumber).ThenBy(s => s.SeatNumber));

        // Hàm tính giá vé theo loại ghế cho suất chiếu này.
        var seatPrice = await BuildSeatPricerAsync(showtime);

        var rows = seats
            .GroupBy(s => s.RowNumber)
            .OrderBy(g => g.Key)
            .Select(g => new SeatRowViewModel
            {
                RowNumber = g.Key,
                RowLabel = RowLabel(g.Key),
                Seats = g.OrderBy(s => s.SeatNumber).Select(s =>
                {
                    var dto = _mapper.Map<SeatDTO>(s);
                    dto.RowLabel = RowLabel(s.RowNumber);
                    dto.Price = seatPrice(s.SeatTypeId);
                    dto.State = s.Status == "Broken" ? "Broken"
                              : bookedSeatIds.Contains(s.Id) ? "Booked"
                              : heldSeatIds.Contains(s.Id) ? "Held"
                              : "Available";
                    return dto;
                }).ToList()
            })
            .ToList();

        return new SeatSelectionViewModel
        {
            ShowtimeId = showtime.Id,
            MovieTitle = showtime.Movie.Title,
            MoviePosterUrl = showtime.Movie.PosterUrl,
            AgeRating = showtime.Movie.AgeRating,
            RoomName = showtime.Room.Name,
            CinemaName = showtime.Room.Cinema?.Name ?? string.Empty,
            StartTime = showtime.StartTime,
            EndTime = showtime.EndTime,
            Rows = rows
        };
    }

    // Giữ 1 ghế cho user trong holdMinutes phút (tạo mới hoặc gia hạn nếu đã giữ).
    public async Task<Result> HoldSeatAsync(Guid showtimeId, Guid seatId, Guid userId, int holdMinutes)
    {
        var now = DateTime.Now;

        // Suất đã bắt đầu / đang chiếu / đã hủy -> không giữ ghế được (trang có thể mở từ trước giờ chiếu).
        var saleOpen = await CheckSaleOpenAsync(showtimeId);
        if (!saleOpen.Succeeded) return saleOpen;

        // Ghế đã có vé (chưa hủy) -> không giữ được.
        var booked = await _unitOfWork.Tickets.ExistsAsync(
            t => t.ShowtimeId == showtimeId && t.SeatId == seatId && t.Status != "Cancelled");
        if (booked) return Result.Failure("Ghế đã được đặt.");

        // Lấy MỌI dòng còn mang trạng thái "Holding" của ghế này, KỂ CẢ đã hết hạn: hold hết hạn không tự
        // đổi trạng thái (không có job dọn), nên vẫn chiếm chỗ trong unique index UX_SeatHolds_Active
        // (ShowtimeId, SeatId) WHERE status='Holding'. Chèn thêm dòng 'Holding' mới sẽ vi phạm index.
        var holds = (await _unitOfWork.SeatHolds.GetAllAsync(
            predicate: h => h.ShowtimeId == showtimeId && h.SeatId == seatId
                && h.Status == "Holding")).ToList();

        // Chỉ hold CÒN HẠN của người khác mới thực sự chặn; hold đã hết hạn thì ai giữ trước cũng không tính.
        if (holds.Any(h => h.UserId != userId && h.ExpiresAt > now))
            return Result.Failure("Ghế đang được người khác giữ.");

        // Nhờ unique index nên nhiều nhất 1 dòng — tái sử dụng để gia hạn (hold của mình) hoặc
        // tiếp quản (hold đã hết hạn, kể cả của người khác) thay vì chèn dòng mới gây trùng index.
        var existing = holds.FirstOrDefault();
        if (existing != null)
        {
            existing.UserId = userId;
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
                UserId = userId,
                HeldAt = now,
                ExpiresAt = now.AddMinutes(holdMinutes),
                Status = "Holding"
            });
        }

        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    // Bỏ giữ 1 ghế của user.
    public async Task ReleaseSeatAsync(Guid showtimeId, Guid seatId, Guid userId)
    {
        var holds = await _unitOfWork.SeatHolds.GetAllAsync(
            predicate: h => h.ShowtimeId == showtimeId && h.SeatId == seatId
                && h.UserId == userId && h.Status == "Holding");
        foreach (var h in holds)
        {
            h.Status = "Released";
            _unitOfWork.SeatHolds.Update(h);
        }
        await _unitOfWork.SaveChangesAsync();
    }

    // Bỏ giữ toàn bộ ghế user đang giữ trong suất (gọi khi rời trang).
    public async Task ReleaseAllAsync(Guid showtimeId, Guid userId)
    {
        var holds = await _unitOfWork.SeatHolds.GetAllAsync(
            predicate: h => h.ShowtimeId == showtimeId && h.UserId == userId && h.Status == "Holding");
        foreach (var h in holds)
        {
            h.Status = "Released";
            _unitOfWork.SeatHolds.Update(h);
        }
        await _unitOfWork.SaveChangesAsync();
    }

    // Gia hạn thời gian giữ cho toàn bộ ghế user đang giữ (heartbeat khi còn ở trang).
    public async Task ExtendHoldsAsync(Guid showtimeId, Guid userId, int holdMinutes)
    {
        var now = DateTime.Now;
        var holds = await _unitOfWork.SeatHolds.GetAllAsync(
            predicate: h => h.ShowtimeId == showtimeId && h.UserId == userId
                && h.Status == "Holding" && h.ExpiresAt > now);
        foreach (var h in holds)
        {
            h.ExpiresAt = now.AddMinutes(holdMinutes);
            _unitOfWork.SeatHolds.Update(h);
        }
        await _unitOfWork.SaveChangesAsync();
    }

    // Lấy dữ liệu trang chọn đồ ăn: ghế user đang giữ + danh sách món + thời gian giữ còn lại.
    public async Task<FoodOrderViewModel?> GetFoodOrderAsync(Guid showtimeId, Guid userId)
    {
        var now = DateTime.Now;

        // Ghế user đang giữ cho suất này; không còn ghế nào -> coi như hết phiên đặt.
        var holds = (await _unitOfWork.SeatHolds.GetAllAsync(
            predicate: h => h.ShowtimeId == showtimeId && h.UserId == userId
                && h.Status == "Holding" && h.ExpiresAt > now)).ToList();
        if (holds.Count == 0) return null;

        var showtime = (await _unitOfWork.Showtimes.GetAllAsync(
            predicate: s => s.Id == showtimeId,
            include: q => q.Include(s => s.Movie).Include(s => s.Room).ThenInclude(r => r.Cinema)))
            .FirstOrDefault();
        if (showtime is null) return null;

        var seatPrice = await BuildSeatPricerAsync(showtime);

        // Thông tin các ghế đang giữ (nhãn + giá).
        var heldSeatIds = holds.Select(h => h.SeatId).ToList();
        var seats = await _unitOfWork.Seats.GetAllAsync(
            predicate: s => heldSeatIds.Contains(s.Id),
            include: q => q.Include(s => s.SeatType),
            orderBy: q => q.OrderBy(s => s.RowNumber).ThenBy(s => s.SeatNumber));
        var seatItems = seats.Select(s => new SelectedSeatItem
        {
            Label = RowLabel(s.RowNumber) + s.SeatNumber,
            Price = seatPrice(s.SeatTypeId)
        }).ToList();

        // Danh sách món còn bán.
        var foods = await _unitOfWork.FoodBeverages.GetAllAsync(
            predicate: f => f.StockStatus == "In Stock",
            orderBy: q => q.OrderBy(f => f.Name));
        var foodItems = _mapper.Map<List<FoodBeverageDTO>>(foods);

        // Thời gian giữ còn lại = tới hold sắp hết hạn sớm nhất.
        var secondsLeft = (int)Math.Max(0, (holds.Min(h => h.ExpiresAt) - now).TotalSeconds);

        return new FoodOrderViewModel
        {
            ShowtimeId = showtimeId,
            MovieTitle = showtime.Movie.Title,
            RoomName = showtime.Room.Name,
            CinemaName = showtime.Room.Cinema?.Name ?? string.Empty,
            StartTime = showtime.StartTime,
            Seats = seatItems,
            SeatTotal = seatItems.Sum(x => x.Price),
            HoldSecondsLeft = secondsLeft,
            FoodItems = foodItems
        };
    }

    // Tạo hàm tính giá vé theo loại ghế cho 1 suất chiếu (base + phụ thu phòng/giờ/ghế).
    // Dùng chung IPricingService (single source of truth) để khớp đúng giá với luồng bán tại quầy.
    private async Task<Func<Guid, decimal>> BuildSeatPricerAsync(Showtime showtime)
    {
        var pricing = await _pricingService.GetPricingAsync(showtime);
        return pricing.PriceForSeatType;
    }

    // Ghế đang giữ kèm giá, dùng cho trang thanh toán/xác nhận.
    private sealed record HeldSeat(Guid SeatId, string Label, decimal Price);
    private sealed record HeldContext(Showtime Showtime, List<HeldSeat> Seats, int SecondsLeft);

    // Tải ghế user đang giữ (kèm giá) + suất chiếu + thời gian giữ còn lại. Null nếu hết giữ.
    private async Task<HeldContext?> LoadHeldContextAsync(Guid showtimeId, Guid userId)
    {
        var now = DateTime.Now;
        var holds = (await _unitOfWork.SeatHolds.GetAllAsync(
            predicate: h => h.ShowtimeId == showtimeId && h.UserId == userId
                && h.Status == "Holding" && h.ExpiresAt > now)).ToList();
        if (holds.Count == 0) return null;

        var showtime = (await _unitOfWork.Showtimes.GetAllAsync(
            predicate: s => s.Id == showtimeId,
            include: q => q.Include(s => s.Movie).Include(s => s.Room).ThenInclude(r => r.Cinema)))
            .FirstOrDefault();
        if (showtime is null) return null;

        var seatPrice = await BuildSeatPricerAsync(showtime);
        var heldSeatIds = holds.Select(h => h.SeatId).ToList();
        var seats = await _unitOfWork.Seats.GetAllAsync(
            predicate: s => heldSeatIds.Contains(s.Id),
            include: q => q.Include(s => s.SeatType),
            orderBy: q => q.OrderBy(s => s.RowNumber).ThenBy(s => s.SeatNumber));
        var heldSeats = seats.Select(s => new HeldSeat(s.Id, RowLabel(s.RowNumber) + s.SeatNumber, seatPrice(s.SeatTypeId))).ToList();
        var secondsLeft = (int)Math.Max(0, (holds.Min(h => h.ExpiresAt) - now).TotalSeconds);
        return new HeldContext(showtime, heldSeats, secondsLeft);
    }

    // Dựng các dòng đồ ăn từ danh sách id + số lượng (gộp trùng, bỏ qua qty <= 0 / món không tồn tại).
    private async Task<List<FoodLineItem>> BuildFoodLinesAsync(List<Guid> foodIds, List<int> foodQtys)
    {
        var qtyById = new Dictionary<Guid, int>();
        if (foodIds != null)
        {
            for (int i = 0; i < foodIds.Count; i++)
            {
                var qty = (foodQtys != null && i < foodQtys.Count) ? foodQtys[i] : 0;
                if (qty <= 0) continue;
                qtyById[foodIds[i]] = (qtyById.TryGetValue(foodIds[i], out var e) ? e : 0) + qty;
            }
        }
        if (qtyById.Count == 0) return new List<FoodLineItem>();

        var ids = qtyById.Keys.ToList();
        var foods = (await _unitOfWork.FoodBeverages.GetAllAsync(predicate: f => ids.Contains(f.Id)))
            .ToDictionary(f => f.Id);
        return qtyById.Where(kv => foods.ContainsKey(kv.Key))
            .Select(kv =>
            {
                var line = _mapper.Map<FoodLineItem>(foods[kv.Key]);
                line.Quantity = Math.Min(MaxFoodPerItem, kv.Value);   // kẹp tối đa 20/loại
                return line;
            }).ToList();
    }

    // Lấy VAT áp dụng: ưu tiên default_vat_id trong config, nếu không có thì lấy VAT đang Active.
    private async Task<(Guid? VatId, decimal Rate)> GetVatAsync()
    {
        var cfg = (await _unitOfWork.SystemConfigs.GetAllAsync(
            predicate: c => c.ConfigKey == "default_vat_id")).FirstOrDefault();

        Vat? vat = null;
        if (cfg?.ConfigValue != null && Guid.TryParse(cfg.ConfigValue, out var vid))
            vat = (await _unitOfWork.Vats.GetAllAsync(predicate: v => v.Id == vid)).FirstOrDefault();
        vat ??= (await _unitOfWork.Vats.GetAllAsync(predicate: v => v.Status == "Active")).FirstOrDefault();

        return (vat?.Id, vat?.VatRate ?? 0m);
    }

    // Kiểm tra mã khuyến mãi và tính số tiền giảm (áp SAU VAT, trên tổng đã gồm thuế).
    // Trả về (promo, discount, error): error != null nghĩa là mã không hợp lệ -> không áp.
    private async Task<(Promotion? Promo, decimal Discount, string? Error)> ResolvePromoAsync(
        string? code, Guid userId, decimal seatTotal, decimal foodTotal, decimal grandTotal)
    {
        if (string.IsNullOrWhiteSpace(code)) return (null, 0m, null);
        code = code.Trim();

        var promo = (await _unitOfWork.Promotions.GetAllAsync(predicate: p => p.Code == code)).FirstOrDefault();
        if (promo is null) return (null, 0m, "Mã giảm giá không tồn tại.");
        if (promo.Status != "Active") return (null, 0m, "Mã giảm giá không còn hiệu lực.");

        var now = DateTime.Now;
        if (now < promo.ValidFrom || now > promo.ValidTo)
            return (null, 0m, "Mã giảm giá đã hết hạn hoặc chưa tới ngày áp dụng.");

        // Đơn tối thiểu tính trên tạm tính (vé + đồ ăn) trước thuế.
        var subtotal = seatTotal + foodTotal;
        if (promo.MinOrderValue.HasValue && subtotal < promo.MinOrderValue.Value)
            return (null, 0m, $"Cần đơn tối thiểu {promo.MinOrderValue.Value:N0}₫ để dùng mã này.");

        // Mỗi user chỉ được dùng một mã đúng 1 lần.
        var usedByUser = await _unitOfWork.Bookings.CountAsync(b => b.PromotionId == promo.Id && b.UserId == userId);
        if (usedByUser >= 1) return (null, 0m, "Bạn đã sử dụng mã giảm giá này rồi.");

        // Giới hạn tổng lượt dùng toàn hệ thống: đếm số booking đã gắn mã này.
        if (promo.UsageLimit.HasValue)
        {
            var used = await _unitOfWork.Bookings.CountAsync(b => b.PromotionId == promo.Id);
            if (used >= promo.UsageLimit.Value) return (null, 0m, "Mã giảm giá đã hết lượt sử dụng.");
        }

        // Đối tượng áp dụng quyết định phần tiền được giảm.
        decimal target = promo.ApplicableTarget switch
        {
            "Ticket_Only" => seatTotal,
            "Food_Only" => foodTotal,
            _ => grandTotal            // "All" hoặc null
        };
        if (target <= 0) return (null, 0m, "Mã không áp dụng cho các mặt hàng trong đơn.");

        var discount = promo.DiscountType == "Percent"
            ? target * (promo.DiscountAmount / 100m)
            : promo.DiscountAmount;

        if (promo.MaxDiscountAmount.HasValue)
            discount = Math.Min(discount, promo.MaxDiscountAmount.Value);

        // Không giảm quá phần được áp dụng: mã "chỉ vé" ≤ tiền vé, "chỉ đồ ăn" ≤ tiền đồ ăn,
        // "cả đơn" ≤ tổng đơn (target = grandTotal). Sau đó làm tròn về đồng.
        discount = Math.Min(discount, target);
        discount = Math.Round(discount, 0, MidpointRounding.AwayFromZero);
        if (discount <= 0) return (null, 0m, "Mã không tạo ra khoản giảm cho đơn này.");

        return (promo, discount, null);
    }

    // Xem trước khi áp mã giảm giá ở trang thanh toán (AJAX): tính số tiền giảm + cập nhật điểm tối đa còn dùng.
    public async Task<PromoPreviewResult> PreviewPromoAsync(Guid showtimeId, Guid userId, List<Guid> foodIds, List<int> foodQtys, string code)
    {
        var ctx = await LoadHeldContextAsync(showtimeId, userId);
        if (ctx is null) return new PromoPreviewResult { Ok = false, Message = "Hết thời gian giữ ghế, vui lòng đặt lại." };

        var foodLines = await BuildFoodLinesAsync(foodIds, foodQtys);
        var seatTotal = ctx.Seats.Sum(s => s.Price);
        var foodTotal = foodLines.Sum(l => l.LineTotal);
        var subtotal = seatTotal + foodTotal;
        var (_, vatRate) = await GetVatAsync();
        var vatAmount = Math.Round(subtotal * vatRate, 0, MidpointRounding.AwayFromZero);
        var grandTotal = subtotal + vatAmount;

        var (promo, discount, error) = await ResolvePromoAsync(code, userId, seatTotal, foodTotal, grandTotal);
        if (error != null || promo is null)
            return new PromoPreviewResult { Ok = false, Message = error ?? "Mã giảm giá không hợp lệ." };

        var afterPromo = grandTotal - discount;
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        var availablePoints = user?.RewardPoints ?? 0;
        var pointPolicy = await _pointConfig.GetPolicyAsync();
        var maxUsablePoints = pointPolicy.MaxUsablePoints(availablePoints, afterPromo);

        return new PromoPreviewResult
        {
            Ok = true,
            Code = promo.Code,
            PromoDiscount = discount,
            FinalAmount = afterPromo,
            MaxUsablePoints = maxUsablePoints,
            Target = promo.ApplicableTarget,
            Message = $"Áp dụng mã {promo.Code}: giảm {discount:N0}₫ ({PromoTargetLabel(promo.ApplicableTarget)})."
        };
    }

    // Nhãn tiếng Việt cho đối tượng áp dụng của mã giảm giá.
    private static string PromoTargetLabel(string? target) => target switch
    {
        "Ticket_Only" => "vé",
        "Food_Only" => "đồ ăn",
        _ => "cả đơn"
    };

    // Lấy dữ liệu trang thanh toán.
    public async Task<PaymentViewModel?> GetPaymentAsync(Guid showtimeId, Guid userId, List<Guid> foodIds, List<int> foodQtys)
    {
        var ctx = await LoadHeldContextAsync(showtimeId, userId);
        if (ctx is null) return null;

        var foodLines = await BuildFoodLinesAsync(foodIds, foodQtys);
        var seatTotal = ctx.Seats.Sum(s => s.Price);
        var foodTotal = foodLines.Sum(l => l.LineTotal);

        // Tạm tính (vé + đồ ăn) -> áp VAT lên tạm tính -> tổng trước giảm.
        var subtotal = seatTotal + foodTotal;
        var (_, vatRate) = await GetVatAsync();
        var vatAmount = Math.Round(subtotal * vatRate, 0, MidpointRounding.AwayFromZero);
        var grandTotal = subtotal + vatAmount;

        // Điểm thưởng có thể dùng: không vượt số điểm đang có và không làm tổng âm.
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        var availablePoints = user?.RewardPoints ?? 0;
        var pointPolicy = await _pointConfig.GetPolicyAsync();
        var maxUsablePoints = pointPolicy.MaxUsablePoints(availablePoints, grandTotal);

        return new PaymentViewModel
        {
            ShowtimeId = showtimeId,
            MovieTitle = ctx.Showtime.Movie.Title,
            RoomName = ctx.Showtime.Room.Name,
            CinemaName = ctx.Showtime.Room.Cinema?.Name ?? string.Empty,
            StartTime = ctx.Showtime.StartTime,
            Seats = ctx.Seats.Select(s => new SelectedSeatItem { Label = s.Label, Price = s.Price }).ToList(),
            SeatTotal = seatTotal,
            FoodLines = foodLines,
            FoodTotal = foodTotal,
            Subtotal = subtotal,
            VatRate = vatRate,
            VatAmount = vatAmount,
            GrandTotal = grandTotal,
            AvailablePoints = availablePoints,
            PointValueVnd = pointPolicy.PointValueVnd,
            MaxUsablePoints = maxUsablePoints,
            HoldSecondsLeft = ctx.SecondsLeft
        };
    }

    // Xác nhận đặt vé (thanh toán giả) + dùng/cộng điểm thưởng.
    public async Task<BookingConfirmResult> ConfirmBookingAsync(Guid showtimeId, Guid userId, string method, List<Guid> foodIds, List<int> foodQtys, int pointsUsed, string? promoCode = null)
    {
        var ctx = await LoadHeldContextAsync(showtimeId, userId);
        if (ctx is null) return BookingConfirmResult.Fail("Hết thời gian giữ ghế, vui lòng đặt lại.");

        // Chốt chặn cuối: không bán vé cho suất đã tới giờ chiếu / đang chiếu / đã hủy
        // (khách có thể giữ ghế trước giờ chiếu rồi mới bấm thanh toán sau đó).
        var saleOpen = EnsureOpenForSale(ctx.Showtime);
        if (!saleOpen.Succeeded) return BookingConfirmResult.Fail(saleOpen.Error!);

        // An toàn: chặn nếu có ghế vừa bị người khác đặt thành vé.
        var seatIds = ctx.Seats.Select(s => s.SeatId).ToList();
        var alreadyBooked = await _unitOfWork.Tickets.ExistsAsync(
            t => t.ShowtimeId == showtimeId && seatIds.Contains(t.SeatId) && t.Status != "Cancelled");
        if (alreadyBooked) return BookingConfirmResult.Fail("Một số ghế vừa được đặt, vui lòng chọn lại.");

        var foodLines = await BuildFoodLinesAsync(foodIds, foodQtys);
        var now = DateTime.Now;
        var seatTotal = ctx.Seats.Sum(s => s.Price);
        var foodTotal = foodLines.Sum(l => l.LineTotal);

        // Tạm tính (vé + đồ ăn) -> áp VAT -> tổng trước giảm.
        var subtotal = seatTotal + foodTotal;
        var (vatId, vatRate) = await GetVatAsync();
        var vatAmount = Math.Round(subtotal * vatRate, 0, MidpointRounding.AwayFromZero);
        var grossTotal = subtotal + vatAmount;

        // Áp mã giảm giá (validate lại server-side); mã không hợp lệ -> báo lỗi, không đặt.
        var (promo, promoDiscount, promoError) = await ResolvePromoAsync(promoCode, userId, seatTotal, foodTotal, grossTotal);
        if (promoError != null) return BookingConfirmResult.Fail(promoError);
        var afterPromo = grossTotal - promoDiscount;

        // Dùng điểm để giảm tiếp trên phần còn lại: kẹp trong [0, số điểm đang có] và không làm tổng âm.
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        var availablePoints = user?.RewardPoints ?? 0;
        var pointPolicy = await _pointConfig.GetPolicyAsync();
        var usePoints = Math.Min(Math.Max(0, pointsUsed), pointPolicy.MaxUsablePoints(availablePoints, afterPromo));
        var pointsDiscount = pointPolicy.DiscountFor(usePoints);
        var discount = promoDiscount + pointsDiscount;     // tổng giảm (mã + điểm) lưu vào Booking
        var grand = afterPromo - pointsDiscount;

        var bookingId = Guid.NewGuid();
        var bookingQr = "BK-" + bookingId.ToString("N")[..12].ToUpper();
        await _unitOfWork.Bookings.AddAsync(new Booking
        {
            Id = bookingId,
            UserId = userId,
            ShowtimeId = showtimeId,
            PromotionId = promo?.Id,
            TotalAmount = subtotal,
            DiscountAmount = discount,
            VatId = vatId,
            VatAmount = vatAmount,
            FinalAmount = grand,
            PaymentStatus = "Paid",
            BookingType = "Online",
            CreatedAt = now,
            QrCode = bookingQr
        });

        foreach (var s in ctx.Seats)
        {
            await _unitOfWork.Tickets.AddAsync(new Ticket
            {
                Id = Guid.NewGuid(),
                BookingId = bookingId,
                ShowtimeId = showtimeId,
                SeatId = s.SeatId,
                PriceAtBooking = s.Price,
                Status = "Booked",
                QrCode = "TK-" + Guid.NewGuid().ToString("N")[..12].ToUpper()
            });
        }

        foreach (var l in foodLines)
        {
            await _unitOfWork.BookingFoods.AddAsync(new BookingFood
            {
                Id = Guid.NewGuid(),
                BookingId = bookingId,
                FbId = l.FbId,
                Quantity = l.Quantity,
                PriceAtBooking = l.Price
            });
        }

        // Thanh toán giả -> ghi nhận thành công ngay.
        await _unitOfWork.Payments.AddAsync(new Payment
        {
            Id = Guid.NewGuid(),
            BookingId = bookingId,
            PaymentMethod = method,
            PaymentSource = "Online",
            Status = "Success",
            PaidAt = now,
            Amount = grand,
            TransactionRef = method.ToUpper() + "-" + now.Ticks
        });

        // Chuyển ghế đang giữ sang Converted (đã thành vé).
        var holds = await _unitOfWork.SeatHolds.GetAllAsync(
            predicate: h => h.ShowtimeId == showtimeId && h.UserId == userId && h.Status == "Holding");
        foreach (var h in holds)
        {
            h.Status = "Converted";
            _unitOfWork.SeatHolds.Update(h);
        }

        // Trừ điểm đã dùng để giảm giá (nếu có).
        if (usePoints > 0)
        {
            await _unitOfWork.RewardPointHistories.AddAsync(new RewardPointHistory
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                BookingId = bookingId,
                PointsChanged = -usePoints,
                ActionType = "Redeemed",
                Description = "Dùng điểm giảm giá khi đặt vé",
                CreatedAt = now
            });
        }

        // Cộng điểm thưởng (booking CONFIRMED): điểm = số tiền thực trả × tỷ lệ trong cấu hình.
        var points = pointPolicy.PointsEarnedFor(grand);
        if (points > 0)
        {
            await _unitOfWork.RewardPointHistories.AddAsync(new RewardPointHistory
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                BookingId = bookingId,
                PointsChanged = points,
                ActionType = "Earned",
                Description = "Cộng điểm khi đặt vé",
                CreatedAt = now
            });
        }

        // Cập nhật số dư điểm: trừ điểm đã dùng + cộng điểm mới (1 lần ghi).
        if (user != null && (usePoints > 0 || points > 0))
        {
            user.RewardPoints = (user.RewardPoints ?? 0) - usePoints + points;
            _unitOfWork.Users.Update(user);
        }

        await _unitOfWork.SaveChangesAsync();

        // Gửi email xác nhận đặt vé kèm mã QR (không để lỗi email làm hỏng việc đặt vé đã thành công).
        await SendBookingConfirmationEmailAsync(user, ctx, bookingQr, method, grand);

        return BookingConfirmResult.Ok(bookingId, points);
    }

    // Gửi email xác nhận đặt vé kèm mã QR tới email khách. Bọc try/catch: vé đã đặt xong nên không rollback dù email lỗi.
    private async Task SendBookingConfirmationEmailAsync(User? user, HeldContext ctx, string bookingQr, string method, decimal total)
    {
        var email = user?.Email;
        if (string.IsNullOrWhiteSpace(email)) return;

        try
        {
            var html = BuildBookingEmailHtml(user!, ctx, bookingQr, method, total);
            await _email.SendAsync(email, "CineStar - Xác nhận đặt vé thành công", html);
        }
        catch
        {
            // Nuốt lỗi gửi email: booking đã lưu thành công, không được để email làm hỏng kết quả.
        }
    }

    // Dựng nội dung HTML email xác nhận. Ảnh QR sinh qua dịch vụ ngoài (api.qrserver.com) để email hiển thị được.
    private static string BuildBookingEmailHtml(User user, HeldContext ctx, string bookingQr, string method, decimal total)
    {
        var name = string.IsNullOrWhiteSpace(user.FullName) ? "bạn" : user.FullName;
        var movie = ctx.Showtime.Movie.Title;
        var room = ctx.Showtime.Room.Name;
        var cinema = ctx.Showtime.Room.Cinema?.Name ?? string.Empty;
        var start = ctx.Showtime.StartTime.ToString("HH:mm - dd/MM/yyyy");
        var seats = string.Join(", ", ctx.Seats.Select(s => s.Label));
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
      <div style=""color:#cbd5e1;font-size:13px;margin-top:2px;"">Xác nhận đặt vé thành công</div>
    </div>
    <div style=""padding:24px;"">
      <p style=""margin:0 0 16px;color:#0f172a;font-size:15px;"">Xin chào <b>{name}</b>, cảm ơn bạn đã đặt vé tại CineStar. Vé của bạn đã được xác nhận.</p>

      <div style=""text-align:center;margin:8px 0 20px;"">
        <img src=""{qrImg}"" alt=""Mã QR vé"" width=""200"" height=""200"" style=""border:1px solid #e2e8f0;border-radius:12px;padding:8px;background:#fff;"" />
        <div style=""margin-top:8px;color:#0f172a;font-size:16px;font-weight:700;letter-spacing:1px;"">{bookingQr}</div>
        <div style=""color:#64748b;font-size:12px;"">Đưa mã này tại quầy để check-in</div>
      </div>

      <table style=""width:100%;border-collapse:collapse;border-top:1px solid #e2e8f0;padding-top:8px;"">
        {Row("Phim", movie)}
        {Row("Suất chiếu", start)}
        {Row("Phòng", string.IsNullOrEmpty(cinema) ? room : $"{room} · {cinema}")}
        {Row("Ghế", seats)}
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

    // Lấy dữ liệu trang đặt vé thành công (chỉ chủ booking mới xem được).
    public async Task<PaymentSuccessViewModel?> GetBookingSuccessAsync(Guid bookingId, Guid userId)
    {
        var booking = (await _unitOfWork.Bookings.GetAllAsync(
            predicate: b => b.Id == bookingId && b.UserId == userId,
            include: q => q
                .Include(b => b.Showtime).ThenInclude(s => s.Movie)
                .Include(b => b.Showtime).ThenInclude(s => s.Room).ThenInclude(r => r.Cinema)
                .Include(b => b.Promotion))).FirstOrDefault();
        if (booking is null) return null;

        var tickets = await _unitOfWork.Tickets.GetAllAsync(
            predicate: t => t.BookingId == bookingId,
            include: q => q.Include(t => t.Seat));
        var seatLabels = tickets.OrderBy(t => t.Seat.RowNumber).ThenBy(t => t.Seat.SeatNumber)
            .Select(t => RowLabel(t.Seat.RowNumber) + t.Seat.SeatNumber).ToList();

        var bfs = (await _unitOfWork.BookingFoods.GetAllAsync(predicate: bf => bf.BookingId == bookingId)).ToList();
        var fbIds = bfs.Select(bf => bf.FbId).ToList();
        var foods = (await _unitOfWork.FoodBeverages.GetAllAsync(predicate: f => fbIds.Contains(f.Id)))
            .ToDictionary(f => f.Id);
        var foodLines = bfs.Select(bf => new FoodLineItem
        {
            FbId = bf.FbId,
            Name = foods.TryGetValue(bf.FbId, out var f) ? f.Name : "(món)",
            Quantity = bf.Quantity,
            Price = bf.PriceAtBooking
        }).ToList();

        var payment = (await _unitOfWork.Payments.GetAllAsync(predicate: p => p.BookingId == bookingId)).FirstOrDefault();
        var pointsEarned = (await _unitOfWork.RewardPointHistories.GetAllAsync(
            predicate: r => r.BookingId == bookingId && r.ActionType == "Earned")).Sum(r => r.PointsChanged);
        var user = await _unitOfWork.Users.GetByIdAsync(userId);

        var subtotal = booking.TotalAmount;
        var vatAmount = booking.VatAmount ?? 0m;
        var vatRate = subtotal > 0 ? Math.Round(vatAmount / subtotal, 2) : 0m;

        // Tách tổng giảm thành: giảm-bằng-điểm (từ lịch sử Redeemed) và giảm-bằng-mã (phần còn lại).
        var totalDiscount = booking.DiscountAmount ?? 0m;
        var redeemed = (await _unitOfWork.RewardPointHistories.GetAllAsync(
            predicate: r => r.BookingId == bookingId && r.ActionType == "Redeemed")).Sum(r => r.PointsChanged);
        var pointsUsed = -redeemed;                                   // PointsChanged âm khi tiêu điểm
        var pointPolicy = await _pointConfig.GetPolicyAsync();
        var pointsDiscount = pointPolicy.DiscountFor(pointsUsed);
        var promoDiscount = totalDiscount - pointsDiscount;

        return new PaymentSuccessViewModel
        {
            BookingId = bookingId,
            BookingCode = booking.QrCode ?? bookingId.ToString("N")[..8].ToUpper(),
            MovieTitle = booking.Showtime.Movie.Title,
            RoomName = booking.Showtime.Room.Name,
            CinemaName = booking.Showtime.Room.Cinema?.Name ?? string.Empty,
            StartTime = booking.Showtime.StartTime,
            SeatLabels = seatLabels,
            FoodLines = foodLines,
            Subtotal = subtotal,
            VatRate = vatRate,
            VatAmount = vatAmount,
            PointsUsed = pointsUsed,
            DiscountAmount = pointsDiscount,
            PromoCode = booking.Promotion?.Code,
            PromoDiscount = promoDiscount,
            GrandTotal = booking.FinalAmount,
            PaymentMethod = payment?.PaymentMethod ?? string.Empty,
            PointsEarned = pointsEarned,
            RewardPointsTotal = user?.RewardPoints ?? 0
        };
    }

    // Đổi số hàng (1,2,3...) thành nhãn chữ (A,B,C...).
    private static string RowLabel(int rowNumber)
    {
        if (rowNumber < 1) return rowNumber.ToString();
        return ((char)('A' + (rowNumber - 1) % 26)).ToString();
    }
}
