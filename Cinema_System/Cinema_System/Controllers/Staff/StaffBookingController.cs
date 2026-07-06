using Cinema_System.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers.Staff;

[Authorize(Roles = "STAFF")]
[Route("Staff/Booking")]
public class StaffBookingController : Controller
{
    private const int PageSize = 10;

    private readonly IBookingManagementService _bookingService;

    public StaffBookingController(IBookingManagementService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? search, string? bookingType, string? paymentStatus, int page = 1)
    {
        var vm = await _bookingService.GetBookingsAsync(
            search, bookingType, paymentStatus, page, PageSize);
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
