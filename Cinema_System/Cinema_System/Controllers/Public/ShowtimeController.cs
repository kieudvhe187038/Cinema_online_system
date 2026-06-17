using Cinema_System.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers.Public
{
    // Trang lịch chiếu công khai — mọi role đều xem được, kể cả khách chưa đăng nhập.
    public class ShowtimeController : Controller
    {
        private readonly IShowtimeService _showtimeService;

        public ShowtimeController(IShowtimeService showtimeService)
        {
            _showtimeService = showtimeService;
        }

        // Xem lịch chiếu, lọc theo phim / phòng / ngày (mặc định hôm nay).
        public async Task<IActionResult> Index(Guid? movieId, Guid? roomId, DateOnly? date)
        {
            var vm = await _showtimeService.GetShowtimePageAsync(movieId, roomId, date);
            return View(vm);
        }
    }
}
