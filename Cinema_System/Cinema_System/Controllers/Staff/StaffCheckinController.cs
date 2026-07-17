using System.Security.Claims;
using Cinema_System.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers.Staff;

[Authorize(Roles = "STAFF")]
[Route("Staff/Checkin")]
public class StaffCheckinController : Controller
{
    private readonly ITicketCheckinService _checkinService;
    private readonly IAuditLogWriter _audit;

    public StaffCheckinController(ITicketCheckinService checkinService, IAuditLogWriter audit)
    {
        _checkinService = checkinService;
        _audit = audit;
    }

    // Lấy Id nhân viên đang đăng nhập từ Claims (LoginController set ClaimTypes.NameIdentifier khi đăng nhập).
    private Guid GetCurrentStaffId()
        => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

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
            var result = await _checkinService.CheckinAsync(qr, GetCurrentStaffId());
            if (!result.Succeeded)
                return Json(new { success = false, error = result.Error });

            await _audit.LogAsync("CHECKIN_TICKET", "Tickets", newValue: new { qr });
            return Json(new { success = true, mode = "ticket", ticket = result.Data });
        }

        // Mã đơn?
        if (await _checkinService.LookupBookingAsync(qr) is not null)
        {
            var result = await _checkinService.CheckinBookingAsync(qr, GetCurrentStaffId());
            if (!result.Succeeded)
                return Json(new { success = false, error = result.Error });

            await _audit.LogAsync("CHECKIN_BOOKING", "Bookings", newValue: new { qr });
            return Json(new { success = true, mode = "booking", booking = result.Data });
        }

        return Json(new { success = false, error = "Không tìm thấy vé hoặc đơn với mã này." });
    }
}
