using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Cinema_System.Application.Services;

// Truy vấn lịch chiếu cho giao diện công khai (mọi role, kể cả khách chưa đăng nhập).
public class ShowtimeService : IShowtimeService
{
    private readonly IUnitOfWork _unitOfWork;

    public ShowtimeService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // Lấy dữ liệu trang lịch chiếu: lọc theo phim/phòng/ngày + tùy chọn dropdown.
    public async Task<ShowtimePageViewModel> GetShowtimePageAsync(Guid? movieId, Guid? roomId, DateOnly? date)
    {
        var selectedDate = date ?? DateOnly.FromDateTime(DateTime.Today);
        var dayStart = selectedDate.ToDateTime(TimeOnly.MinValue);
        var dayEnd = dayStart.AddDays(1);

        // Suất chiếu trong ngày đã chọn, lọc thêm theo phim/phòng nếu có.
        var showtimes = await _unitOfWork.Showtimes.GetAllAsync(
            predicate: s =>
                s.StartTime >= dayStart && s.StartTime < dayEnd &&
                (movieId == null || s.MovieId == movieId) &&
                (roomId == null || s.RoomId == roomId) &&
                s.Status != "Cancelled",
            include: q => q
                .Include(s => s.Movie)
                .Include(s => s.Room).ThenInclude(r => r.RoomType)
                .Include(s => s.Room).ThenInclude(r => r.Cinema),
            orderBy: q => q.OrderBy(s => s.StartTime));

        var showtimeDtos = showtimes.Select(s => new ShowtimeDTO
        {
            Id = s.Id,
            MovieId = s.MovieId,
            MovieTitle = s.Movie.Title,
            MoviePosterUrl = s.Movie.PosterUrl,
            DurationMinutes = s.Movie.DurationMinutes,
            AgeRating = s.Movie.AgeRating,
            RoomId = s.RoomId,
            RoomName = s.Room.Name,
            RoomTypeName = s.Room.RoomType?.Name,
            CinemaName = s.Room.Cinema?.Name ?? string.Empty,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            Status = s.Status
        }).ToList();

        // Tùy chọn dropdown: phim & phòng (kèm tên rạp).
        var movies = await _unitOfWork.Movies.GetAllAsync(
            orderBy: q => q.OrderBy(m => m.Title));
        var rooms = await _unitOfWork.Rooms.GetAllAsync(
            include: q => q.Include(r => r.Cinema),
            orderBy: q => q.OrderBy(r => r.Name));

        return new ShowtimePageViewModel
        {
            MovieId = movieId,
            RoomId = roomId,
            SelectedDate = selectedDate,
            Movies = movies.Select(m => new ShowtimeFilterOption { Id = m.Id, Name = m.Title }).ToList(),
            Rooms = rooms.Select(r => new ShowtimeFilterOption
            {
                Id = r.Id,
                Name = r.Cinema != null ? $"{r.Name} — {r.Cinema.Name}" : r.Name
            }).ToList(),
            // Thanh chọn ngày: 14 ngày kể từ hôm nay.
            AvailableDates = Enumerable.Range(0, 14)
                .Select(i => DateOnly.FromDateTime(DateTime.Today).AddDays(i))
                .ToList(),
            Showtimes = showtimeDtos
        };
    }
}
