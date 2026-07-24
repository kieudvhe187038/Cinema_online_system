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
    private readonly ICounterBookingService _counterBookingService;
    private readonly IBookingManagementService _bookingService;
    private readonly IMemberService _memberService;
    private readonly IAuditLogWriter _audit;

    public StaffCounterController(
        ICounterBookingService counterBookingService,
        IBookingManagementService bookingService,
        IMemberService memberService,
        IAuditLogWriter audit)
    {
        _counterBookingService = counterBookingService;
        _bookingService = bookingService;
        _memberService = memberService;
        _audit = audit;
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
    [HttpGet("Seats/{showtimeId}")]
    public async Task<IActionResult> Seats(Guid showtimeId)
    {
        var seatMap = await _counterBookingService.GetSeatMapAsync(showtimeId);
        if (seatMap is null) return NotFound();

        return Json(seatMap);
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
