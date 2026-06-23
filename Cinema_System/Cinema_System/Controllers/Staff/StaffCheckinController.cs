using Cinema_System.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers.Staff;

[Route("Staff/Checkin")]
public class StaffCheckinController : Controller
{
    private readonly ITicketCheckinService _checkinService;

    public StaffCheckinController(ITicketCheckinService checkinService)
    {
        _checkinService = checkinService;
    }

    // --- Trang check-in vé (#Inter3) ---
    [HttpGet("")]
    public IActionResult Index()
    {
        return View();
    }

    // --- AJAX: tra cứu theo mã QR để xem trước (không thay đổi dữ liệu).
    // Mã có thể là mã VÉ (1 vé) hoặc mã ĐƠN (nhiều vé) — tự nhận diện. ---
    [HttpGet("Lookup")]
    public async Task<IActionResult> Lookup(string? qr)
    {
        var ticket = await _checkinService.LookupAsync(qr);
        if (ticket is not null)
            return Json(new { found = true, mode = "ticket", ticket });

        var booking = await _checkinService.LookupBookingAsync(qr);
        if (booking is not null)
            return Json(new { found = true, mode = "booking", booking });

        return Json(new { found = false });
    }

    // --- AJAX: xác nhận check-in (vé đơn lẻ hoặc toàn bộ vé của một đơn) ---
    [HttpPost("Confirm")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(string? qr)
    {
        // Mã vé đơn lẻ?
        if (await _checkinService.LookupAsync(qr) is not null)
        {
            var result = await _checkinService.CheckinAsync(qr);
            return result.Succeeded
                ? Json(new { success = true, mode = "ticket", ticket = result.Data })
                : Json(new { success = false, error = result.Error });
        }

        // Mã đơn?
        if (await _checkinService.LookupBookingAsync(qr) is not null)
        {
            var result = await _checkinService.CheckinBookingAsync(qr);
            return result.Succeeded
                ? Json(new { success = true, mode = "booking", booking = result.Data })
                : Json(new { success = false, error = result.Error });
        }

        return Json(new { success = false, error = "Không tìm thấy vé hoặc đơn với mã này." });
    }
}
