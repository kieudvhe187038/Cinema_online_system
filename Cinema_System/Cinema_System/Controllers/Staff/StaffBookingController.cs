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

    // --- Quét QR (camera) trên trang danh sách đơn: tìm đơn theo mã rồi chuyển tới Chi tiết ---
    [HttpGet("LookupByQr")]
    public async Task<IActionResult> LookupByQr(string? qr)
    {
        var bookingId = await _bookingService.FindBookingIdByQrAsync(qr);
        if (bookingId is null)
        {
            TempData["Error"] = "Không tìm thấy đơn đặt vé với mã này.";
            return RedirectToAction(nameof(Index));
        }

        return RedirectToAction(nameof(Details), new { id = bookingId });
    }
}
