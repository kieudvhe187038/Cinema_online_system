using System.Security.Claims;
using Cinema_System.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers.Public
{
    // Trang lịch chiếu công khai — mọi role đều xem được, kể cả khách chưa đăng nhập.
    public class ShowtimeController : Controller
    {
        private readonly IShowtimeService _showtimeService;

        // Thời gian giữ ghế (phút) khi khách chọn ghế.
        private const int HoldMinutes = 10;

        public ShowtimeController(IShowtimeService showtimeService)
        {
            _showtimeService = showtimeService;
        }

        // Id user hiện tại (lấy từ claim đăng nhập).
        private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // Xem lịch chiếu, lọc theo phim / phòng / ngày (mặc định hôm nay).
        public async Task<IActionResult> Index(Guid? movieId, Guid? roomId, DateOnly? date)
        {
            var vm = await _showtimeService.GetShowtimePageAsync(movieId, roomId, date);
            return View(vm);
        }

        // Trang chọn ghế cho một suất chiếu (click từ lịch chiếu sang).
        // Cho CUSTOMER (tự mua) và STAFF (mua hộ khách tại quầy); guest sẽ bị chuyển tới trang đăng nhập.
        [Authorize(Roles = "CUSTOMER,STAFF")]
        public async Task<IActionResult> SelectSeats(Guid id)
        {
            var vm = await _showtimeService.GetSeatSelectionAsync(id, CurrentUserId);
            if (vm is null) return NotFound();
            return View(vm);
        }

        // Trang chọn đồ ăn & nước (bước sau khi chọn ghế).
        // Nếu user không còn giữ ghế nào (hết giờ giữ) -> quay lại trang chọn ghế.
        [Authorize(Roles = "CUSTOMER,STAFF")]
        public async Task<IActionResult> Food(Guid id)
        {
            var vm = await _showtimeService.GetFoodOrderAsync(id, CurrentUserId);
            if (vm is null) return RedirectToAction(nameof(SelectSeats), new { id });
            return View(vm);
        }

        // Trang thanh toán (nhận đồ ăn đã chọn từ trang Food qua form POST).
        [HttpPost]
        [Authorize(Roles = "CUSTOMER,STAFF")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Payment(Guid id, List<Guid> fbId, List<int> qty)
        {
            var vm = await _showtimeService.GetPaymentAsync(id, CurrentUserId, fbId ?? new(), qty ?? new());
            if (vm is null) return RedirectToAction(nameof(SelectSeats), new { id });
            return View(vm);
        }

        // Xác nhận thanh toán (giả lập) -> tạo booking, dùng/cộng điểm, rồi sang trang thành công.
        [HttpPost]
        [Authorize(Roles = "CUSTOMER,STAFF")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(Guid id, string method, List<Guid> fbId, List<int> qty, int pointsUsed = 0)
        {
            var allowed = new[] { "VNPay", "MoMo" };
            if (string.IsNullOrEmpty(method) || !allowed.Contains(method)) method = "VNPay";
            fbId ??= new();
            qty ??= new();
            if (pointsUsed < 0) pointsUsed = 0;

            var result = await _showtimeService.ConfirmBookingAsync(id, CurrentUserId, method, fbId, qty, pointsUsed);
            if (!result.Succeeded)
            {
                TempData["Error"] = result.Error;
                return RedirectToAction(nameof(SelectSeats), new { id });
            }
            return RedirectToAction(nameof(Success), new { id = result.BookingId });
        }

        // Trang đặt vé thành công.
        [Authorize(Roles = "CUSTOMER,STAFF")]
        public async Task<IActionResult> Success(Guid id)
        {
            var vm = await _showtimeService.GetBookingSuccessAsync(id, CurrentUserId);
            if (vm is null) return NotFound();
            return View(vm);
        }

        // Giữ 1 ghế trong 10 phút cho user hiện tại.
        [HttpPost]
        [Authorize(Roles = "CUSTOMER,STAFF")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HoldSeat(Guid showtimeId, Guid seatId)
        {
            var result = await _showtimeService.HoldSeatAsync(showtimeId, seatId, CurrentUserId, HoldMinutes);
            return Json(new { ok = result.Succeeded, message = result.Error });
        }

        // Bỏ giữ 1 ghế.
        [HttpPost]
        [Authorize(Roles = "CUSTOMER,STAFF")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReleaseSeat(Guid showtimeId, Guid seatId)
        {
            await _showtimeService.ReleaseSeatAsync(showtimeId, seatId, CurrentUserId);
            return Json(new { ok = true });
        }

        // Bỏ giữ toàn bộ ghế đang giữ (gọi khi rời trang, qua navigator.sendBeacon).
        [HttpPost]
        [Authorize(Roles = "CUSTOMER,STAFF")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReleaseAll(Guid showtimeId)
        {
            await _showtimeService.ReleaseAllAsync(showtimeId, CurrentUserId);
            return Json(new { ok = true });
        }

        // Gia hạn thời gian giữ ghế (heartbeat khi khách vẫn ở trang chọn ghế).
        [HttpPost]
        [Authorize(Roles = "CUSTOMER,STAFF")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExtendHolds(Guid showtimeId)
        {
            await _showtimeService.ExtendHoldsAsync(showtimeId, CurrentUserId, HoldMinutes);
            return Json(new { ok = true });
        }
    }
}
