using Cinema_System.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers.Staff;

[Route("Staff/Booking")]
public class StaffBookingController : Controller
{
    private readonly IBookingService _bookingService;

    public StaffBookingController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? search, string? bookingType, string? paymentStatus, int page = 1)
    {
        var vm = await _bookingService.GetBookingsAsync(
            search, bookingType, paymentStatus, page, pageSize: 10);
        return View(vm);
    }

    [HttpGet("Details/{id}")]
    public async Task<IActionResult> Details(Guid id)
    {
        var detail = await _bookingService.GetBookingDetailAsync(id);
        if (detail is null) return NotFound();

        return View(detail);
    }
}
