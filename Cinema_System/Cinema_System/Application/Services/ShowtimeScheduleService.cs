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

    public ShowtimeScheduleService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
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

        // Lấy danh sách các suất chiếu nằm trong khoảng thời gian của tuần hiện tại và thỏa mãn bộ lọc
        var showtimes = await _unitOfWork.Showtimes.GetAllAsync(
            predicate: s =>
                s.EndTime > weekBegin &&
                s.StartTime < weekEnd &&
                (roomId == null || s.RoomId == roomId) &&
                (string.IsNullOrEmpty(status) || s.Status == status) &&
                (string.IsNullOrEmpty(search) || s.Movie.Title.Contains(search) || s.Room.Name.Contains(search)),
            includeProperties: new[] { "Movie", "Room" },
            orderBy: q => q.OrderBy(s => s.StartTime));

        var showtimeDtos = new List<ShowtimeScheduleDTO>();
        var bookedCount = 0;

        foreach (var showtime in showtimes)
        {
            // Kiểm tra xem suất chiếu này đã có khách hàng đặt vé/đặt chỗ chưa
            var hasBookings = await _unitOfWork.Bookings.ExistsAsync(b => b.ShowtimeId == showtime.Id) ||
                              await _unitOfWork.Tickets.ExistsAsync(t => t.ShowtimeId == showtime.Id);
            if (hasBookings) bookedCount++;

            var dto = _mapper.Map<ShowtimeScheduleDTO>(showtime);
            dto.HasBookings = hasBookings;
            showtimeDtos.Add(dto);
        }

        var weekDays = Enumerable.Range(0, 7).Select(i => weekBegin.AddDays(i)).ToList();
        var totalMovies = showtimeDtos.Select(s => s.MovieTitle).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        var totalShowtimes = showtimeDtos.Count;
        var confirmedRate = totalShowtimes == 0 ? 0 : (int)Math.Round(bookedCount * 100.0 / totalShowtimes);

        // Tính toán các số liệu thống kê mới theo phòng đang chọn (nếu có)
        int? searchMovieTotalShowtimes = null;
        if (!string.IsNullOrEmpty(search))
        {
            searchMovieTotalShowtimes = await _unitOfWork.Showtimes.CountAsync(s => s.Movie.Title.Contains(search) && (roomId == null || s.RoomId == roomId));
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
        var showtime = await _unitOfWork.Showtimes.GetByIdAsync(id);
        if (showtime is null)
            return null;

        var movies = await GetMovieOptionsAsync();
        var rooms = await GetRoomOptionsAsync();

        var hasBookings = await _unitOfWork.Bookings.ExistsAsync(b => b.ShowtimeId == id) ||
                          await _unitOfWork.Tickets.ExistsAsync(t => t.ShowtimeId == id);

        var vm = _mapper.Map<ShowtimeFormViewModel>(showtime);
        vm.HasBookings = hasBookings;
        vm.AvailableMovies = movies;
        vm.AvailableRooms = rooms;
        return vm;
    }

    /// <summary>
    /// Lấy danh sách phim khả dụng cho dropdown
    /// </summary>
    /// <returns>Danh sách các phim với ID và tên</returns>
    public async Task<IEnumerable<ItemOptionDTO>> GetMovieOptionsAsync()
    {
        var movies = await _unitOfWork.Movies.GetAllAsync(orderBy: q => q.OrderBy(m => m.Title));
        return _mapper.Map<List<ItemOptionDTO>>(movies);
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

        // 2. Movie Status Restriction
        if (movie.Status == "Stopped")
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

        if (hasCoreChanges && movie.Status == "Stopped")
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

        showtime.MovieId = model.MovieId;
        showtime.RoomId = model.RoomId;
        showtime.StartTime = model.StartTime;
        showtime.EndTime = model.EndTime;

        _unitOfWork.Showtimes.Update(showtime);
        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    /// <summary>
    /// Hủy suất chiếu (Khi đã có vé/khách đặt, đổi trạng thái sang Cancelled thay vì xóa)
    /// </summary>
    /// <param name="id">ID suất chiếu cần hủy</param>
    public async Task<Result> CancelAsync(Guid id)
    {
        var showtime = await _unitOfWork.Showtimes.GetByIdAsync(id);
        if (showtime is null)
            return Result.Failure("Không tìm thấy suất chiếu.");

        if (showtime.Status == "Completed")
            return Result.Failure("Suất chiếu đã hoàn thành, không thể hủy.");

        if (showtime.Status == "Cancelled")
            return Result.Failure("Suất chiếu này đã bị hủy từ trước.");

        showtime.Status = "Cancelled";
        _unitOfWork.Showtimes.Update(showtime);
        await _unitOfWork.SaveChangesAsync();

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

        _unitOfWork.Showtimes.Remove(showtime);
        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }
}
