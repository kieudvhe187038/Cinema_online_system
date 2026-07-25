using System.Security.Claims;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers.Staff;

[Authorize(Roles = "STAFF")]
[Route("Staff/Counter")]
public class StaffCounterController : Controller
{
    // Thời gian giữ ghế (phút) khi nhân viên chọn ghế tại quầy — khớp thời gian giữ của luồng đặt online.
    private const int HoldMinutes = 10;

    private readonly ICounterBookingService _counterBookingService;
    private readonly IBookingManagementService _bookingService;
    private readonly IMemberService _memberService;
    private readonly IAuditLogWriter _audit;
    private readonly IVietQrService _vietQr;

    public StaffCounterController(
        ICounterBookingService counterBookingService,
        IBookingManagementService bookingService,
        IMemberService memberService,
        IAuditLogWriter audit,
        IVietQrService vietQr)
    {
        _counterBookingService = counterBookingService;
        _bookingService = bookingService;
        _memberService = memberService;
        _audit = audit;
        _vietQr = vietQr;
    }

    // Lấy Id nhân viên đang đăng nhập từ Claims (LoginController set ClaimTypes.NameIdentifier khi đăng nhập).
    private Guid GetCurrentStaffId()
        => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // --- Trang đặt vé tại quầy (#39/#40/#42) ---
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var vm = await _counterBookingService.GetCounterDataAsync(GetCurrentStaffId());
        return View(vm);
    }

    // --- AJAX: suất chiếu theo phim ---
    [HttpGet("Showtimes/{movieId}")]
    public async Task<IActionResult> Showtimes(Guid movieId)
    {
        var showtimes = await _counterBookingService.GetShowtimesAsync(movieId);
        return Json(showtimes);
    }

    // --- AJAX: sơ đồ ghế + giá theo suất chiếu ---
    // holdToken: phiên giữ ghế riêng của tab/máy quầy gọi lên (xem ghi chú ở HoldSeat).
    [HttpGet("Seats/{showtimeId}")]
    public async Task<IActionResult> Seats(Guid showtimeId, Guid holdToken)
    {
        var seatMap = await _counterBookingService.GetSeatMapAsync(showtimeId, GetCurrentStaffId(), holdToken);
        if (seatMap is null) return NotFound();

        return Json(seatMap);
    }

    // --- AJAX: giữ 1 ghế trong lúc nhân viên nhập thông tin đơn (chặn người khác chọn trùng ghế) ---
    // holdToken do client sinh cho MỖI tab quầy (sessionStorage): nhiều máy quầy dùng chung 1 tài khoản
    // Staff nên nếu chỉ khóa theo tài khoản thì máy này coi hold của máy kia là của chính mình → không chặn.
    [HttpPost("HoldSeat")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HoldSeat(Guid showtimeId, Guid seatId, Guid holdToken)
    {
        var result = await _counterBookingService.HoldSeatAsync(showtimeId, seatId, GetCurrentStaffId(), holdToken, HoldMinutes);
        return Json(new { ok = result.Succeeded, message = result.Error });
    }

    // --- AJAX: bỏ giữ 1 ghế (nhân viên bỏ chọn) ---
    [HttpPost("ReleaseSeat")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReleaseSeat(Guid showtimeId, Guid seatId, Guid holdToken)
    {
        await _counterBookingService.ReleaseSeatAsync(showtimeId, seatId, GetCurrentStaffId(), holdToken);
        return Json(new { ok = true });
    }

    // --- AJAX: bỏ giữ toàn bộ ghế đang giữ của 1 suất (đổi suất chiếu khác / rời trang) ---
    [HttpPost("ReleaseAll")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReleaseAll(Guid showtimeId, Guid holdToken)
    {
        await _counterBookingService.ReleaseAllAsync(showtimeId, GetCurrentStaffId(), holdToken);
        return Json(new { ok = true });
    }

    // --- AJAX: xem trước áp mã khuyến mãi trước khi chốt đơn ---
    [HttpGet("PreviewPromo")]
    public async Task<IActionResult> PreviewPromo(string code, Guid? customerId, decimal seatTotal, decimal foodTotal)
    {
        var result = await _counterBookingService.PreviewPromoAsync(code, customerId, seatTotal, foodTotal);
        return Json(result);
    }

    // --- AJAX: sinh mã QR chuyển khoản (VietQR) hiển thị cho khách quét khi chọn thanh toán Chuyển khoản ---
    [HttpGet("VietQrCode")]
    public IActionResult VietQrCode(Guid showtimeId, decimal amount)
    {
        if (!_vietQr.Enabled) return Json(new { enabled = false });

        var content = "CineStar " + showtimeId.ToString("N")[..8].ToUpper();
        return Json(new
        {
            enabled = true,
            qrUrl = _vietQr.BuildQrUrl(amount, content),
            bankCode = _vietQr.BankCode,
            accountNo = _vietQr.AccountNo,
            accountName = _vietQr.AccountName,
            content
        });
    }

    // --- Trang tra cứu thành viên (#41) ---
    [HttpGet("Member")]
    public IActionResult Member()
    {
        return View();
    }

    // --- AJAX: tra cứu thành viên theo SĐT (#41) ---
    [HttpGet("LookupMember")]
    public async Task<IActionResult> LookupMember(string? phone)
    {
        var member = await _memberService.LookupByPhoneAsync(phone);
        if (member is null)
            return Json(new { found = false });

        return Json(new { found = true, member });
    }

    // --- Tạo đơn + thanh toán (#40/#42) ---
    // Trả JSON (thay vì redirect) để trang Counter xử lý lỗi tại chỗ — quan trọng nhất là lỗi
    // "ghế vừa bị đặt/giữ bởi người khác" (race condition khi sơ đồ ghế tải 1 lần rồi đứng yên
    // trong lúc Staff thao tác): JS sẽ tải lại sơ đồ ghế thay vì reload cả trang mất hết lựa chọn.
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CounterBookingRequest request)
    {
        var result = await _counterBookingService.CreateAsync(request, GetCurrentStaffId());
        if (!result.Succeeded)
        {
            return Json(new { ok = false, error = result.Error });
        }

        await _audit.LogAsync("CREATE_COUNTER_BOOKING", "Bookings", result.Data,
            newValue: new { request.ShowtimeId });

        return Json(new { ok = true, ticketUrl = Url.Action(nameof(Ticket), new { id = result.Data }) });
    }

    // --- In vé (#43) ---
    [HttpGet("Ticket/{id}")]
    public async Task<IActionResult> Ticket(Guid id)
    {
        var vm = await _bookingService.GetTicketPrintAsync(id);
        if (vm is null) return NotFound();

        return View(vm);
    }
}
