using AutoMapper;
using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Services;

/// <summary>
/// Dịch vụ quản lý suất chiếu phim cho Manager (CRUD + lịch tuần).
/// </summary>
public class ShowtimeScheduleService : IShowtimeScheduleService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IMovieStatusService _movieStatusService;

    public ShowtimeScheduleService(IUnitOfWork unitOfWork, IMapper mapper, IMovieStatusService movieStatusService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _movieStatusService = movieStatusService;
    }

    /// <summary>
    /// Lấy dữ liệu lịch chiếu theo tuần với bộ lọc phòng, trạng thái, tên phim/phòng
    /// </summary>
    /// <param name="roomId">ID phòng chiếu (null = tất cả phòng)</param>
    /// <param name="status">Trạng thái suất chiếu (Scheduled, Live, Completed, Cancelled)</param>
    /// <param name="search">Tìm kiếm theo tên phim hoặc tên phòng</param>
    /// <param name="weekStart">Ngày bắt đầu của tuần (mặc định là hôm nay)</param>
    /// <returns>ViewModel chứa dữ liệu lịch chiếu tuần</returns>
    public async Task<ShowtimeCalendarViewModel> GetCalendarAsync(Guid? roomId, string? status, string? search, DateTime? weekStart)
    {
        var today = DateTime.Today;
        var start = weekStart?.Date ?? today;
        var offset = ((int)start.DayOfWeek + 6) % 7; // Monday-based week
        var weekBegin = start.AddDays(-offset);
        var weekEnd = weekBegin.AddDays(7);

        var rooms = await _unitOfWork.Rooms.GetAllAsync(orderBy: q => q.OrderBy(r => r.Name));
        var availableRooms = _mapper.Map<List<ItemOptionDTO>>(rooms);

        // Tự động chọn phòng đầu tiên nếu người dùng chưa chọn phòng cụ thể
        if (!roomId.HasValue && availableRooms.Any())
        {
            roomId = availableRooms.First().Id;
        }

        // Từ khóa tên phim/phòng phải khớp cả khi gõ KHÔNG DẤU — SQL Server không so sánh được
        // như vậy, nên tìm id phim/phòng khớp trước (2 bảng nhỏ) rồi mới lọc suất chiếu ở SQL.
        var searchKey = VietnameseText.ToSearchKey(search);
        var hasSearch = searchKey.Length > 0;
        var searchMovieIds = new List<Guid>();
        var searchRoomIds = new List<Guid>();

        if (hasSearch)
        {
            var allMovies = await _unitOfWork.Movies.GetAllAsync();
            searchMovieIds = allMovies
                .Where(m => VietnameseText.Contains(m.Title, searchKey))
                .Select(m => m.Id)
                .ToList();
            searchRoomIds = rooms
                .Where(r => VietnameseText.Contains(r.Name, searchKey))
                .Select(r => r.Id)
                .ToList();
        }

        // Lấy danh sách các suất chiếu nằm trong khoảng thời gian của tuần hiện tại và thỏa mãn bộ lọc
        var showtimes = await _unitOfWork.Showtimes.GetAllAsync(
            predicate: s =>
                s.EndTime > weekBegin &&
                s.StartTime < weekEnd &&
                (roomId == null || s.RoomId == roomId) &&
                (string.IsNullOrEmpty(status) || s.Status == status) &&
                (!hasSearch || searchMovieIds.Contains(s.MovieId) || searchRoomIds.Contains(s.RoomId)),
            includeProperties: new[] { "Movie", "Room" },
            orderBy: q => q.OrderBy(s => s.StartTime));

        // Đếm số vé đã bán theo từng suất chiếu (1 truy vấn duy nhất, tránh N+1).
        var showtimeIds = showtimes.Select(s => s.Id).ToList();
        var seatsSoldByShowtime = (await _unitOfWork.Tickets.GetAllAsync(t => showtimeIds.Contains(t.ShowtimeId)))
            .GroupBy(t => t.ShowtimeId)
            .ToDictionary(g => g.Key, g => g.Count());

        var showtimeDtos = new List<ShowtimeScheduleDTO>();
        var bookedCount = 0;

        foreach (var showtime in showtimes)
        {
            var seatsSold = seatsSoldByShowtime.GetValueOrDefault(showtime.Id);

            // Kiểm tra xem suất chiếu này đã có khách hàng đặt vé/đặt chỗ chưa
            var hasBookings = seatsSold > 0 || await _unitOfWork.Bookings.ExistsAsync(b => b.ShowtimeId == showtime.Id);
            if (hasBookings) bookedCount++;

            var dto = _mapper.Map<ShowtimeScheduleDTO>(showtime);
            dto.HasBookings = hasBookings;
            dto.SeatsSold = seatsSold;
            showtimeDtos.Add(dto);
        }

        var weekDays = Enumerable.Range(0, 7).Select(i => weekBegin.AddDays(i)).ToList();
        var totalMovies = showtimeDtos.Select(s => s.MovieTitle).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        var totalShowtimes = showtimeDtos.Count;
        var confirmedRate = totalShowtimes == 0 ? 0 : (int)Math.Round(bookedCount * 100.0 / totalShowtimes);

        // Tính toán các số liệu thống kê mới theo phòng đang chọn (nếu có)
        int? searchMovieTotalShowtimes = null;
        if (hasSearch)
        {
            searchMovieTotalShowtimes = await _unitOfWork.Showtimes.CountAsync(s => searchMovieIds.Contains(s.MovieId) && (roomId == null || s.RoomId == roomId));
        }

        var weekTotalShowtimes = await _unitOfWork.Showtimes.CountAsync(s => s.EndTime > weekBegin && s.StartTime < weekEnd && (roomId == null || s.RoomId == roomId));
        var weekScheduledShowtimes = await _unitOfWork.Showtimes.CountAsync(s => s.EndTime > weekBegin && s.StartTime < weekEnd && (roomId == null || s.RoomId == roomId) && (s.Status == null || s.Status == "" || s.Status == "Scheduled"));
        var weekLiveShowtimes = await _unitOfWork.Showtimes.CountAsync(s => s.EndTime > weekBegin && s.StartTime < weekEnd && (roomId == null || s.RoomId == roomId) && s.Status == "Live");
        var weekCompletedShowtimes = await _unitOfWork.Showtimes.CountAsync(s => s.EndTime > weekBegin && s.StartTime < weekEnd && (roomId == null || s.RoomId == roomId) && s.Status == "Completed");
        var weekCancelledShowtimes = await _unitOfWork.Showtimes.CountAsync(s => s.EndTime > weekBegin && s.StartTime < weekEnd && (roomId == null || s.RoomId == roomId) && s.Status == "Cancelled");

        var monthStart = new DateTime(weekBegin.Year, weekBegin.Month, 1);
        var monthEnd = monthStart.AddMonths(1);
        var monthTotalShowtimes = await _unitOfWork.Showtimes.CountAsync(s => s.EndTime > monthStart && s.StartTime < monthEnd && (roomId == null || s.RoomId == roomId));

        return new ShowtimeCalendarViewModel
        {
            AvailableRooms = availableRooms,
            SelectedRoomId = roomId,
            SelectedRoomName = roomId.HasValue ? availableRooms.FirstOrDefault(r => r.Id == roomId)?.Name ?? "Tất cả phòng chiếu" : "Tất cả phòng chiếu",
            WeekStart = weekBegin,
            WeekDays = weekDays,
            Showtimes = showtimeDtos,
            Search = search,
            StatusFilter = status,
            TotalMovies = totalMovies,
            TotalShowtimes = totalShowtimes,
            ConfirmedRate = confirmedRate,
            SearchMovieTotalShowtimes = searchMovieTotalShowtimes,
            WeekTotalShowtimes = weekTotalShowtimes,
            WeekScheduledShowtimes = weekScheduledShowtimes,
            WeekLiveShowtimes = weekLiveShowtimes,
            WeekCompletedShowtimes = weekCompletedShowtimes,
            WeekCancelledShowtimes = weekCancelledShowtimes,
            MonthTotalShowtimes = monthTotalShowtimes
        };
    }

    /// <summary>
    /// Lấy thông tin suất chiếu để chỉnh sửa
    /// </summary>
    /// <param name="id">ID suất chiếu</param>
    /// <returns>ViewModel chứa thông tin suất chiếu và danh sách phim/phòng khả dụng</returns>
    public async Task<ShowtimeFormViewModel?> GetForEditAsync(Guid id)
    {
        var showtime = await _unitOfWork.Showtimes.FirstOrDefaultAsync(
            s => s.Id == id, includeProperties: new[] { "Room" });
        if (showtime is null)
            return null;

        var movies = (await GetMovieOptionsAsync()).ToList();
        if (!movies.Any(m => m.Id == showtime.MovieId))
        {
            var currentMovie = await _unitOfWork.Movies.GetByIdAsync(showtime.MovieId);
            if (currentMovie is not null)
                movies.Insert(0, new ItemOptionDTO { Id = currentMovie.Id, Name = $"{currentMovie.Title} (Đã ngừng chiếu)" });
        }
        var rooms = await GetRoomOptionsAsync();

        var seatsSold = await _unitOfWork.Tickets.CountAsync(t => t.ShowtimeId == id);
        var hasBookings = seatsSold > 0 || await _unitOfWork.Bookings.ExistsAsync(b => b.ShowtimeId == id);

        var vm = _mapper.Map<ShowtimeFormViewModel>(showtime);
        vm.HasBookings = hasBookings;
        vm.SeatsSold = seatsSold;
        vm.Capacity = showtime.Room?.TotalSeats ?? 0;
        vm.AvailableMovies = movies;
        vm.AvailableRooms = rooms;
        return vm;
    }

    /// <summary>
    /// Lấy danh sách phim khả dụng cho dropdown. Phim chưa tới ngày khởi chiếu VẪN được xếp lịch
    /// (xếp trước ngày khởi chiếu = suất chiếu sớm) nên tên phim kèm luôn ngày khởi chiếu để Manager biết.
    /// </summary>
    /// <returns>Danh sách các phim với ID và tên</returns>
    public async Task<IEnumerable<ItemOptionDTO>> GetMovieOptionsAsync()
    {
        // Chỉ loại phim đã ngừng chiếu.
        var movies = await _unitOfWork.Movies.GetAllAsync(
            predicate: m => m.Status != MovieStatus.Stopped,
            orderBy: q => q.OrderBy(m => m.Title));

        var today = DateOnly.FromDateTime(DateTime.Now);
        return movies.Select(m => new ItemOptionDTO
        {
            Id = m.Id,
            Name = m.ReleaseDate.HasValue && m.ReleaseDate.Value > today
                ? $"{m.Title} (Khởi chiếu {m.ReleaseDate.Value:dd/MM/yyyy})"
                : m.Title
        }).ToList();
    }

    /// <summary>
    /// Lấy danh sách phòng chiếu khả dụng cho dropdown
    /// </summary>
    /// <returns>Danh sách các phòng chiếu với ID và tên</returns>
    public async Task<IEnumerable<ItemOptionDTO>> GetRoomOptionsAsync()
    {
        var rooms = await _unitOfWork.Rooms.GetAllAsync(orderBy: q => q.OrderBy(r => r.Name));
        return _mapper.Map<List<ItemOptionDTO>>(rooms);
    }

    /// <summary>
    /// Tạo mới một suất chiếu
    /// </summary>
    /// <param name="model">Dữ liệu suất chiếu cần tạo</param>
    /// <returns>Kết quả thực hiện (Success/Failure với thông báo lỗi nếu có)</returns>
    public async Task<Result> CreateAsync(ShowtimeFormViewModel model)
    {
        // 1. Past Time Restriction
        if (model.StartTime < DateTime.Now.AddMinutes(30))
            return Result.Failure("Thời gian bắt đầu phải cách hiện tại ít nhất 30 phút.");

        var movie = await _unitOfWork.Movies.GetByIdAsync(model.MovieId);
        if (movie is null)
            return Result.Failure("Phim không tồn tại.");

        // 2. Movie Status Restriction — chỉ chặn phim đã ngừng chiếu.
        // Phim sắp chiếu được phép xếp lịch: nếu suất chiếu trước ngày khởi chiếu thì phim thành "chiếu sớm".
        if (movie.Status == MovieStatus.Stopped)
            return Result.Failure("Không thể xếp lịch cho phim đã ngừng chiếu.");

        var roomExists = await _unitOfWork.Rooms.ExistsAsync(r => r.Id == model.RoomId);
        if (!roomExists)
            return Result.Failure("Phòng chiếu không tồn tại.");

        // Tự động tính thời gian kết thúc dựa vào thời lượng phim
        var duration = movie.DurationMinutes ?? 120; // 120 là giá trị dự phòng nếu phim không có thời lượng
        model.EndTime = model.StartTime.AddMinutes(duration);

        // Kiểm tra xem khoảng thời gian này phòng chiếu đã có lịch nào chưa (bỏ qua các lịch đã hủy)
        // Mỗi suất chiếu sẽ cộng thêm 20 phút dọn dẹp sau khi kết thúc.
        // Khoảng chặn của lịch chiếu là [StartTime, EndTime + 20 phút]
        var modelEndBlocked = model.EndTime.AddMinutes(20);
        var conflict = await _unitOfWork.Showtimes.ExistsAsync(s =>
            s.Status != "Cancelled" &&
            s.RoomId == model.RoomId &&
            model.StartTime < s.EndTime.AddMinutes(20) &&
            modelEndBlocked > s.StartTime);

        if (conflict)
            return Result.Failure("Phòng đã có suất chiếu khác hoặc đang trong thời gian dọn dẹp.");

        var showtime = new Showtime
        {
            Id = Guid.NewGuid(),
            MovieId = model.MovieId,
            RoomId = model.RoomId,
            StartTime = model.StartTime,
            EndTime = model.EndTime,
            Status = "Scheduled" // Luôn bắt đầu bằng Scheduled
        };

        await _unitOfWork.Showtimes.AddAsync(showtime);
        await _unitOfWork.SaveChangesAsync();

        // Có lịch mới -> cập nhật lại trạng thái phim (có thể chuyển Sắp chiếu -> Chiếu sớm).
        await _movieStatusService.SyncAndSaveAsync(showtime.MovieId);
        return Result.Success();
    }

    /// <summary>
    /// Cập nhật thông tin suất chiếu
    /// </summary>
    /// <param name="model">Dữ liệu suất chiếu cần cập nhật</param>
    /// <returns>Kết quả thực hiện (không được phép sửa nếu đã có vé)</returns>
    public async Task<Result> UpdateAsync(ShowtimeFormViewModel model)
    {
        var showtime = await _unitOfWork.Showtimes.GetByIdAsync(model.Id);
        if (showtime is null)
            return Result.Failure("Không tìm thấy suất chiếu.");

        bool hasCoreChanges = showtime.MovieId != model.MovieId ||
                              showtime.RoomId != model.RoomId ||
                              showtime.StartTime != model.StartTime;

        // 4. Live Lock
        if (hasCoreChanges && (showtime.Status == "Live" || showtime.Status == "Completed"))
            return Result.Failure("Không thể chỉnh sửa thông tin khi suất chiếu đang diễn ra hoặc đã hoàn thành.");

        if (hasCoreChanges)
        {
            // 1. Past Time Restriction
            if (model.StartTime < DateTime.Now.AddMinutes(30))
                return Result.Failure("Thời gian bắt đầu phải cách hiện tại ít nhất 30 phút.");
        }

        var movie = await _unitOfWork.Movies.GetByIdAsync(model.MovieId);
        if (movie is null)
            return Result.Failure("Phim không tồn tại.");

        // Chỉ chặn phim đã ngừng chiếu; phim sắp chiếu vẫn được xếp lịch (suất trước ngày khởi chiếu = chiếu sớm).
        if (hasCoreChanges && movie.Status == MovieStatus.Stopped)
            return Result.Failure("Không thể xếp lịch cho phim đã ngừng chiếu.");

        var roomExists = await _unitOfWork.Rooms.ExistsAsync(r => r.Id == model.RoomId);
        if (!roomExists)
            return Result.Failure("Phòng chiếu không tồn tại.");

        // Tự động tính lại thời gian kết thúc (đề phòng đổi phim hoặc đổi giờ bắt đầu)
        var duration = movie.DurationMinutes ?? 120;
        model.EndTime = model.StartTime.AddMinutes(duration);

        // Kiểm tra xem suất chiếu đã có vé hoặc lượt đặt chỗ nào chưa
        var hasBookings = await _unitOfWork.Bookings.ExistsAsync(b => b.ShowtimeId == model.Id) ||
                          await _unitOfWork.Tickets.ExistsAsync(t => t.ShowtimeId == model.Id);

        // Nếu đã có khách, chỉ cho phép cập nhật trạng thái (Live, Cancelled...) chứ không được sửa phim hay giờ
        if (hasBookings && hasCoreChanges)
            return Result.Failure("Không thể thay đổi phim, phòng chiếu hoặc thời gian của suất chiếu đã có vé hoặc đặt chỗ (chỉ được đổi trạng thái).");

        // Nếu có thay đổi về phòng hoặc thời gian, cần check lại xem có bị trùng lịch với suất chiếu khác không
        if (hasCoreChanges)
        {
            var modelEndBlocked = model.EndTime.AddMinutes(20);
            var conflict = await _unitOfWork.Showtimes.ExistsAsync(s =>
                s.Id != model.Id &&
                s.Status != "Cancelled" &&
                s.RoomId == model.RoomId &&
                model.StartTime < s.EndTime.AddMinutes(20) &&
                modelEndBlocked > s.StartTime);
            if (conflict)
                return Result.Failure("Phòng đã có suất chiếu khác hoặc đang trong thời gian dọn dẹp.");
        }

        var previousMovieId = showtime.MovieId;

        showtime.MovieId = model.MovieId;
        showtime.RoomId = model.RoomId;
        showtime.StartTime = model.StartTime;
        showtime.EndTime = model.EndTime;

        _unitOfWork.Showtimes.Update(showtime);
        await _unitOfWork.SaveChangesAsync();

        // Lịch đổi giờ/đổi phim -> cập nhật lại trạng thái của cả phim mới lẫn phim cũ.
        await _movieStatusService.SyncAndSaveAsync(showtime.MovieId);
        if (previousMovieId != showtime.MovieId)
            await _movieStatusService.SyncAndSaveAsync(previousMovieId);

        return Result.Success();
    }

    /// <summary>
    /// Hủy suất chiếu (Khi đã có vé/khách đặt, đổi trạng thái sang Cancelled thay vì xóa)
    /// </summary>
    /// <param name="id">ID suất chiếu cần hủy</param>
    /// <summary>
    /// Kiểm tra suất chiếu đã có khách đặt vé thành công (đã thanh toán) hay chưa.
    /// Dùng để quyết định hủy trực tiếp hay phải chuyển sang Quản lý sự cố (có hoàn điểm + báo khách).
    /// </summary>
    public Task<bool> HasPaidBookingsAsync(Guid id)
        => _unitOfWork.Bookings.ExistsAsync(b => b.ShowtimeId == id && b.PaymentStatus == "Paid");

    public async Task<Result> CancelAsync(Guid id)
    {
        var showtime = await _unitOfWork.Showtimes.GetByIdAsync(id);
        if (showtime is null)
            return Result.Failure("Không tìm thấy suất chiếu.");

        if (showtime.Status == "Completed")
            return Result.Failure("Suất chiếu đã hoàn thành, không thể hủy.");

        if (showtime.Status == "Cancelled")
            return Result.Failure("Suất chiếu này đã bị hủy từ trước.");

        // Suất đã có khách đặt vé -> phải hủy qua Quản lý sự cố để hoàn điểm và báo khách,
        // không cho hủy "chay" ở màn Lịch chiếu.
        if (await HasPaidBookingsAsync(id))
            return Result.Failure("Suất chiếu đã có khách đặt vé. Vui lòng thực hiện hủy qua Quản lý sự cố để hoàn điểm và thông báo cho khách hàng.");

        showtime.Status = "Cancelled";
        _unitOfWork.Showtimes.Update(showtime);
        await _unitOfWork.SaveChangesAsync();

        // Hủy suất chiếu sớm cuối cùng -> phim quay lại Sắp chiếu.
        await _movieStatusService.SyncAndSaveAsync(showtime.MovieId);
        return Result.Success();
    }

    /// <summary>
    /// Xóa suất chiếu
    /// </summary>
    /// <param name="id">ID suất chiếu cần xóa</param>
    /// <returns>Kết quả thực hiện (không được phép xóa nếu đã có vé)</returns>
    public async Task<Result> DeleteAsync(Guid id)
    {
        var showtime = await _unitOfWork.Showtimes.GetByIdAsync(id);
        if (showtime is null)
            return Result.Failure("Không tìm thấy suất chiếu.");

        if (showtime.Status == "Completed")
            return Result.Failure("Suất chiếu đã hoàn thành, không thể xóa.");

        // Chặn xóa nếu suất chiếu có dữ liệu liên quan để đảm bảo tính toàn vẹn của database
        var hasBookings = await _unitOfWork.Bookings.ExistsAsync(b => b.ShowtimeId == id);
        var hasTickets = await _unitOfWork.Tickets.ExistsAsync(t => t.ShowtimeId == id);
        var hasHolds = await _unitOfWork.SeatHolds.ExistsAsync(h => h.ShowtimeId == id);

        if (hasBookings || hasTickets || hasHolds)
            return Result.Failure("Không thể xóa suất chiếu khi đã có vé, đặt chỗ hoặc giữ ghế.");

        var movieId = showtime.MovieId;
        _unitOfWork.Showtimes.Remove(showtime);
        await _unitOfWork.SaveChangesAsync();

        // Xóa suất chiếu sớm cuối cùng -> phim quay lại Sắp chiếu.
        await _movieStatusService.SyncAndSaveAsync(movieId);
        return Result.Success();
    }
}
