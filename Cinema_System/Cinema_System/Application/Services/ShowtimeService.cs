using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Services;

/// <summary>
/// Dịch vụ quản lý suất chiếu phim
/// </summary>
public class ShowtimeService : IShowtimeService
{
    private readonly IUnitOfWork _unitOfWork;

    public ShowtimeService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
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
        var availableRooms = rooms.Select(r => new ItemOptionDTO { Id = r.Id, Name = r.Name }).ToList();

        // Include showtimes that overlap the week range (start < weekEnd && end > weekBegin)
        var showtimes = await _unitOfWork.Showtimes.GetAllAsync(
            predicate: s =>
                s.EndTime > weekBegin &&
                s.StartTime < weekEnd &&
                (roomId == null || s.RoomId == roomId) &&
                (string.IsNullOrEmpty(status) || s.Status == status) &&
                (string.IsNullOrEmpty(search) || s.Movie.Title.Contains(search) || s.Room.Name.Contains(search)),
            includeProperties: new[] { "Movie", "Room" },
            orderBy: q => q.OrderBy(s => s.StartTime));

        var showtimeDtos = new List<ShowtimeDTO>();
        var bookedCount = 0;
        foreach (var showtime in showtimes)
        {
            var hasBookings = await _unitOfWork.Bookings.ExistsAsync(b => b.ShowtimeId == showtime.Id) ||
                              await _unitOfWork.Tickets.ExistsAsync(t => t.ShowtimeId == showtime.Id);
            if (hasBookings) bookedCount++;

            showtimeDtos.Add(new ShowtimeDTO
            {
                Id = showtime.Id,
                MovieTitle = showtime.Movie?.Title ?? "—",
                RoomName = showtime.Room?.Name ?? "—",
                StartTime = showtime.StartTime,
                EndTime = showtime.EndTime,
                Status = string.IsNullOrEmpty(showtime.Status) ? "Scheduled" : showtime.Status,
                HasBookings = hasBookings
            });
        }

        var weekDays = Enumerable.Range(0, 7).Select(i => weekBegin.AddDays(i)).ToList();
        var totalMovies = showtimeDtos.Select(s => s.MovieTitle).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        var totalShowtimes = showtimeDtos.Count;
        var confirmedRate = totalShowtimes == 0 ? 0 : (int)Math.Round(bookedCount * 100.0 / totalShowtimes);

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
            ConfirmedRate = confirmedRate
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

        return new ShowtimeFormViewModel
        {
            Id = showtime.Id,
            MovieId = showtime.MovieId,
            RoomId = showtime.RoomId,
            StartTime = showtime.StartTime,
            EndTime = showtime.EndTime,
            Status = string.IsNullOrEmpty(showtime.Status) ? "Scheduled" : showtime.Status,
            AvailableMovies = movies,
            AvailableRooms = rooms
        };
    }

    /// <summary>
    /// Lấy danh sách phim khả dụng cho dropdown
    /// </summary>
    /// <returns>Danh sách các phim với ID và tên</returns>
    public async Task<IEnumerable<ItemOptionDTO>> GetMovieOptionsAsync()
    {
        var movies = await _unitOfWork.Movies.GetAllAsync(orderBy: q => q.OrderBy(m => m.Title));
        return movies.Select(m => new ItemOptionDTO { Id = m.Id, Name = m.Title });
    }

    /// <summary>
    /// Lấy danh sách phòng chiếu khả dụng cho dropdown
    /// </summary>
    /// <returns>Danh sách các phòng chiếu với ID và tên</returns>
    public async Task<IEnumerable<ItemOptionDTO>> GetRoomOptionsAsync()
    {
        var rooms = await _unitOfWork.Rooms.GetAllAsync(orderBy: q => q.OrderBy(r => r.Name));
        return rooms.Select(r => new ItemOptionDTO { Id = r.Id, Name = r.Name });
    }

    /// <summary>
    /// Tạo mới một suất chiếu
    /// </summary>
    /// <param name="model">Dữ liệu suất chiếu cần tạo</param>
    /// <returns>Kết quả thực hiện (Success/Failure với thông báo lỗi nếu có)</returns>
    public async Task<Result> CreateAsync(ShowtimeFormViewModel model)
    {
        var movieExists = await _unitOfWork.Movies.ExistsAsync(m => m.Id == model.MovieId);
        if (!movieExists)
            return Result.Failure("Phim không tồn tại.");

        var roomExists = await _unitOfWork.Rooms.ExistsAsync(r => r.Id == model.RoomId);
        if (!roomExists)
            return Result.Failure("Phòng chiếu không tồn tại.");

        var conflict = await _unitOfWork.Showtimes.ExistsAsync(s =>
            s.RoomId == model.RoomId &&
            model.StartTime < s.EndTime &&
            model.EndTime > s.StartTime);
        if (conflict)
            return Result.Failure("Phòng đã có suất chiếu trùng thời gian.");

        var showtime = new Showtime
        {
            Id = Guid.NewGuid(),
            MovieId = model.MovieId,
            RoomId = model.RoomId,
            StartTime = model.StartTime,
            EndTime = model.EndTime,
            Status = string.IsNullOrEmpty(model.Status) ? "Scheduled" : model.Status
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

        var movieExists = await _unitOfWork.Movies.ExistsAsync(m => m.Id == model.MovieId);
        if (!movieExists)
            return Result.Failure("Phim không tồn tại.");

        var roomExists = await _unitOfWork.Rooms.ExistsAsync(r => r.Id == model.RoomId);
        if (!roomExists)
            return Result.Failure("Phòng chiếu không tồn tại.");

        var hasBookings = await _unitOfWork.Bookings.ExistsAsync(b => b.ShowtimeId == model.Id) ||
                          await _unitOfWork.Tickets.ExistsAsync(t => t.ShowtimeId == model.Id);
        if (hasBookings)
            return Result.Failure("Không thể sửa suất chiếu đã có vé hoặc đặt chỗ.");

        var conflict = await _unitOfWork.Showtimes.ExistsAsync(s =>
            s.Id != model.Id &&
            s.RoomId == model.RoomId &&
            model.StartTime < s.EndTime &&
            model.EndTime > s.StartTime);
        if (conflict)
            return Result.Failure("Phòng đã có suất chiếu trùng thời gian.");

        showtime.MovieId = model.MovieId;
        showtime.RoomId = model.RoomId;
        showtime.StartTime = model.StartTime;
        showtime.EndTime = model.EndTime;
        showtime.Status = string.IsNullOrEmpty(model.Status) ? "Scheduled" : model.Status;

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
