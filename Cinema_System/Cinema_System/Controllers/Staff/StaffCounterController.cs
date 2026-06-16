using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers.Staff;

[Route("Staff/Counter")]
public class StaffCounterController : Controller
{
    private readonly ICounterBookingService _counterBookingService;
    private readonly IBookingService _bookingService;
    private readonly IMemberService _memberService;

    public StaffCounterController(
        ICounterBookingService counterBookingService,
        IBookingService bookingService,
        IMemberService memberService)
    {
        _counterBookingService = counterBookingService;
        _bookingService = bookingService;
        _memberService = memberService;
    }

    // --- Trang đặt vé tại quầy (#39/#40/#42) ---
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var vm = await _counterBookingService.GetCounterDataAsync();
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
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CounterBookingRequest request)
    {
        var result = await _counterBookingService.CreateAsync(request);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Đặt vé tại quầy thành công.";
        return RedirectToAction(nameof(Ticket), new { id = result.Data });
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
