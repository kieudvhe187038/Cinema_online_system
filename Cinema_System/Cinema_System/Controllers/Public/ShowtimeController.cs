using System.Security.Claims;
using System.Text.Json;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers.Public
{
    // Trang lịch chiếu công khai — mọi role đều xem được, kể cả khách chưa đăng nhập.
    public class ShowtimeController : Controller
    {
        private readonly IShowtimeService _showtimeService;
        private readonly IVnpayService _vnpay;
        private readonly IVietQrService _vietqr;

        // Thời gian giữ ghế (phút) khi khách chọn ghế.
        private const int HoldMinutes = 10;

        public ShowtimeController(IShowtimeService showtimeService, IVnpayService vnpay, IVietQrService vietqr)
        {
            _showtimeService = showtimeService;
            _vnpay = vnpay;
            _vietqr = vietqr;
        }

        // Đơn đang chờ thanh toán VNPay, lưu tạm trong Session để khôi phục khi cổng trả về.
        public class VnpayPendingOrder
        {
            public Guid ShowtimeId { get; set; }
            public List<Guid> FoodIds { get; set; } = new();
            public List<int> FoodQtys { get; set; } = new();
            public int PointsUsed { get; set; }
            public string? PromoCode { get; set; }
        }

        // Id user hiện tại (lấy từ claim đăng nhập).
        private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // Xem lịch chiếu, lọc theo phim / thể loại / độ tuổi / loại phòng / ngày (mặc định hôm nay).
        public async Task<IActionResult> Index(Guid? movieId, Guid? genreId, string? ageRating, Guid? roomTypeId, DateOnly? date)
        {
            var vm = await _showtimeService.GetShowtimePageAsync(movieId, genreId, ageRating, roomTypeId, date);
            return View(vm);
        }

        // Popup "đặt vé nhanh" (AJAX): trả về partial ngày + suất chiếu của một phim để hiển thị trong modal
        // ở trang chủ / chi tiết phim (giữ nguyên trang hiện tại). Mọi role đều xem được.
        [HttpGet]
        public async Task<IActionResult> MovieShowtimes(Guid movieId)
        {
            var vm = await _showtimeService.GetMovieShowtimesAsync(movieId);
            if (vm is null) return NotFound();
            return PartialView("_MovieShowtimes", vm);
        }

        // Trang chọn ghế cho một suất chiếu (click từ lịch chiếu sang).
        // Cho CUSTOMER (tự mua) và STAFF (mua hộ khách tại quầy); guest sẽ bị chuyển tới trang đăng nhập.
        [Authorize(Roles = "CUSTOMER,STAFF")]
        public async Task<IActionResult> SelectSeats(Guid id)
        {
            // Chặn đặt vé online khi khách chưa đủ tuổi theo phân loại phim (chỉ áp cho CUSTOMER;
            // STAFF bán hộ tại quầy tự xác minh giấy tờ khách).
            if (User.IsInRole("CUSTOMER"))
            {
                var ageCheck = await _showtimeService.CheckAgeRestrictionAsync(id, CurrentUserId);
                if (!ageCheck.Succeeded)
                {
                    TempData["Error"] = ageCheck.Error;
                    return RedirectToAction(nameof(Index));
                }
            }

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

        // Xác nhận thanh toán -> nếu chọn VNPay (đã cấu hình) thì sang cổng VNPay; còn lại thanh toán giả.
        [HttpPost]
        [Authorize(Roles = "CUSTOMER,STAFF")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(Guid id, string method, List<Guid> fbId, List<int> qty, int pointsUsed = 0, string? promoCode = null)
        {
            var allowed = new[] { "VNPay", "MoMo", "VietQR" };
            if (string.IsNullOrEmpty(method) || !allowed.Contains(method)) method = "VietQR";
            fbId ??= new();
            qty ??= new();
            if (pointsUsed < 0) pointsUsed = 0;
            promoCode = string.IsNullOrWhiteSpace(promoCode) ? null : promoCode.Trim();

            // VietQR: hiển thị mã QR chuyển khoản; tạo đơn khi khách bấm "Đã chuyển khoản".
            if (method == "VietQR" && _vietqr.Enabled)
            {
                var pvm = await _showtimeService.GetPaymentAsync(id, CurrentUserId, fbId, qty);
                if (pvm is null) return RedirectToAction(nameof(SelectSeats), new { id });

                // Trừ mã giảm giá trước, rồi mới trừ điểm trên phần còn lại.
                var promoDiscount = await ResolvePromoDiscountAsync(id, fbId, qty, promoCode);
                var afterPromo = pvm.GrandTotal - promoDiscount;
                var use = Math.Max(0, Math.Min(Math.Min(pointsUsed, pvm.MaxUsablePoints), (int)(afterPromo / pvm.PointValueVnd)));
                var amount = afterPromo - (decimal)use * pvm.PointValueVnd;
                var content = "CineStar " + id.ToString("N")[..8].ToUpper();

                return View("VietQr", new VietQrViewModel
                {
                    ShowtimeId = id,
                    MovieTitle = pvm.MovieTitle,
                    StartTime = pvm.StartTime,
                    Amount = amount,
                    QrUrl = _vietqr.BuildQrUrl(amount, content),
                    TransferContent = content,
                    BankCode = _vietqr.BankCode,
                    AccountNo = _vietqr.AccountNo,
                    AccountName = _vietqr.AccountName,
                    FoodIds = fbId,
                    FoodQtys = qty,
                    PointsUsed = use,
                    PromoCode = promoCode,
                    HoldSecondsLeft = pvm.HoldSecondsLeft
                });
            }

            // VNPay thật (sandbox): tạo URL thanh toán rồi redirect sang cổng.
            if (method == "VNPay" && _vnpay.Enabled)
            {
                var vm = await _showtimeService.GetPaymentAsync(id, CurrentUserId, fbId, qty);
                if (vm is null) return RedirectToAction(nameof(SelectSeats), new { id });

                // Trừ mã giảm giá + kẹp điểm dùng vào số tiền cần thanh toán qua cổng.
                var promoDiscount = await ResolvePromoDiscountAsync(id, fbId, qty, promoCode);
                var afterPromo = vm.GrandTotal - promoDiscount;
                var usePoints = Math.Max(0, Math.Min(Math.Min(pointsUsed, vm.MaxUsablePoints), (int)(afterPromo / vm.PointValueVnd)));
                var amount = afterPromo - (decimal)usePoints * vm.PointValueVnd;

                var txnRef = DateTime.Now.Ticks.ToString();
                HttpContext.Session.SetString("vnp_" + txnRef,
                    JsonSerializer.Serialize(new VnpayPendingOrder { ShowtimeId = id, FoodIds = fbId, FoodQtys = qty, PointsUsed = usePoints, PromoCode = promoCode }));

                // Dùng IPv4 (localhost trả về "::1" IPv6 dễ gây lệch khi cổng xử lý).
                var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                var ip = (string.IsNullOrEmpty(remoteIp) || remoteIp.Contains(':')) ? "127.0.0.1" : remoteIp;
                var returnUrl = Url.Action(nameof(VnpayReturn), "Showtime", null, Request.Scheme)!;
                // OrderInfo không dấu, không khoảng trắng để tránh mọi khác biệt khi encode.
                var payUrl = _vnpay.CreatePaymentUrl(amount, txnRef,
                    $"CineStarBooking{id:N}", ip, returnUrl);
                return Redirect(payUrl);
            }

            // Thanh toán giả (MoMo hoặc VNPay chưa cấu hình): tạo booking ngay.
            var result = await _showtimeService.ConfirmBookingAsync(id, CurrentUserId, method, fbId, qty, pointsUsed, promoCode);
            if (!result.Succeeded)
            {
                TempData["Error"] = result.Error;
                return RedirectToAction(nameof(SelectSeats), new { id });
            }
            return RedirectToAction(nameof(Success), new { id = result.BookingId });
        }

        // VNPay redirect người dùng về đây sau khi thanh toán: verify chữ ký rồi tạo booking + cộng điểm.
        [Authorize(Roles = "CUSTOMER,STAFF")]
        public async Task<IActionResult> VnpayReturn()
        {
            var check = _vnpay.ValidateReturn(Request.Query);
            var key = "vnp_" + check.TxnRef;
            var json = HttpContext.Session.GetString(key);
            HttpContext.Session.Remove(key);

            if (!check.Valid || json is null)
            {
                TempData["Error"] = "Xác thực thanh toán VNPay thất bại.";
                return RedirectToAction(nameof(Index));
            }

            var pending = JsonSerializer.Deserialize<VnpayPendingOrder>(json)!;

            // ResponseCode "00" = thanh toán thành công.
            if (check.ResponseCode != "00")
            {
                TempData["Error"] = "Thanh toán VNPay không thành công hoặc đã bị hủy.";
                return RedirectToAction(nameof(SelectSeats), new { id = pending.ShowtimeId });
            }

            var result = await _showtimeService.ConfirmBookingAsync(
                pending.ShowtimeId, CurrentUserId, "VNPay", pending.FoodIds, pending.FoodQtys, pending.PointsUsed, pending.PromoCode);
            if (!result.Succeeded)
            {
                TempData["Error"] = result.Error;
                return RedirectToAction(nameof(SelectSeats), new { id = pending.ShowtimeId });
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

        // Khách bấm "Đã chuyển khoản" trên trang VietQR -> tạo đơn (xác nhận thủ công).
        [HttpPost]
        [Authorize(Roles = "CUSTOMER,STAFF")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VietQrPaid(Guid id, List<Guid> fbId, List<int> qty, int pointsUsed = 0, string? promoCode = null)
        {
            var result = await _showtimeService.ConfirmBookingAsync(
                id, CurrentUserId, "VietQR", fbId ?? new(), qty ?? new(), pointsUsed, promoCode);
            if (!result.Succeeded)
            {
                TempData["Error"] = result.Error;
                return RedirectToAction(nameof(SelectSeats), new { id });
            }
            return RedirectToAction(nameof(Success), new { id = result.BookingId });
        }

        // Áp mã giảm giá (AJAX): kiểm tra mã + trả về số tiền giảm để cập nhật tổng trên trang thanh toán.
        [HttpPost]
        [Authorize(Roles = "CUSTOMER,STAFF")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyPromo(Guid showtimeId, List<Guid> fbId, List<int> qty, string code)
        {
            var res = await _showtimeService.PreviewPromoAsync(showtimeId, CurrentUserId, fbId ?? new(), qty ?? new(), code ?? string.Empty);
            return Json(new { ok = res.Ok, message = res.Message, discount = res.PromoDiscount, maxPoints = res.MaxUsablePoints, code = res.Code });
        }

        // Tính trước số tiền mã giảm cho luồng VietQR/VNPay (0 nếu mã không hợp lệ).
        private async Task<decimal> ResolvePromoDiscountAsync(Guid showtimeId, List<Guid> fbId, List<int> qty, string? promoCode)
        {
            if (string.IsNullOrWhiteSpace(promoCode)) return 0m;
            var res = await _showtimeService.PreviewPromoAsync(showtimeId, CurrentUserId, fbId, qty, promoCode);
            return res.Ok ? res.PromoDiscount : 0m;
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
