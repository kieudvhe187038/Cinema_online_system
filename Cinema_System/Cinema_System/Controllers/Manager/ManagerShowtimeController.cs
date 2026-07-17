using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers.Manager;

/// <summary>
/// Controller quản lý suất chiếu phim
/// </summary>
[Route("Manager/Showtime")]
[Authorize(Roles = "MANAGER")]
public class ManagerShowtimeController : Controller
{
    private readonly IShowtimeScheduleService _showtimeService;
    private readonly IAuditLogWriter _audit;

    public ManagerShowtimeController(IShowtimeScheduleService showtimeService, IAuditLogWriter audit)
    {
        _showtimeService = showtimeService;
        _audit = audit;
    }

    /// <summary>
    /// Chuyển hướng về trang lịch chiếu
    /// </summary>
    [HttpGet("")]
    public IActionResult Index()
    {
        return RedirectToAction(nameof(Calendar));
    }

    /// <summary>
    /// Hiển thị lịch chiếu tuần với các bộ lọc
    /// </summary>
    [HttpGet("Calendar")]
    public async Task<IActionResult> Calendar(Guid? roomId, string? status, string? search, DateTime? weekStart)
    {
        bool needsRedirect = false;
        var cleanRouteValues = new RouteValueDictionary();

        foreach (var key in Request.Query.Keys)
        {
            var value = Request.Query[key].ToString();
            if (string.Equals(key, "roomId", StringComparison.OrdinalIgnoreCase))
            {
                if (Guid.TryParse(value, out var parsedGuid))
                {
                    cleanRouteValues["roomId"] = parsedGuid;
                }
                else
                {
                    needsRedirect = true;
                }
            }
            else if (string.Equals(key, "status", StringComparison.OrdinalIgnoreCase))
            {
                var allowedStatuses = new[] { "Scheduled", "Live", "Completed", "Cancelled" };
                if (allowedStatuses.Contains(value, StringComparer.OrdinalIgnoreCase) || string.IsNullOrEmpty(value))
                {
                    if (!string.IsNullOrEmpty(value))
                        cleanRouteValues["status"] = value;
                }
                else
                {
                    needsRedirect = true;
                }
            }
            else if (string.Equals(key, "search", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    cleanRouteValues["search"] = value;
                }
            }
            else if (string.Equals(key, "weekStart", StringComparison.OrdinalIgnoreCase))
            {
                if (DateTime.TryParse(value, out var parsedDate))
                {
                    cleanRouteValues["weekStart"] = parsedDate.ToString("yyyy-MM-dd");
                }
                else
                {
                    needsRedirect = true;
                }
            }
            else
            {
                needsRedirect = true; // Query parameter lạ
            }
        }

        if (needsRedirect)
        {
            return RedirectToAction(nameof(Calendar), cleanRouteValues);
        }

        var vm = await _showtimeService.GetCalendarAsync(roomId, status, search, weekStart);
        return View(vm);
    }

    /// <summary>
    /// Trang tạo suất chiếu mới
    /// </summary>
    [HttpGet("Create")]
    public async Task<IActionResult> Create()
    {
        var vm = new ShowtimeFormViewModel
        {
            AvailableMovies = await _showtimeService.GetMovieOptionsAsync(),
            AvailableRooms = await _showtimeService.GetRoomOptionsAsync()
        };
        return View(vm);
    }

    /// <summary>
    /// Lưu thông tin suất chiếu mới
    /// </summary>
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ShowtimeFormViewModel model)
    {
        // Nạp lại danh sách dropdown để hiển thị lại nếu form bị lỗi
        model.AvailableMovies = await _showtimeService.GetMovieOptionsAsync();
        model.AvailableRooms = await _showtimeService.GetRoomOptionsAsync();

        if (!ModelState.IsValid)
            return View(model);

        var result = await _showtimeService.CreateAsync(model);
        if (!result.Succeeded)
        {
            // Hiển thị lỗi từ Service (ví dụ: trùng lịch, phim không tồn tại)
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        await _audit.LogAsync("CREATE_SHOWTIME", "Showtimes",
            newValue: new { model.MovieId, model.RoomId, model.StartTime, model.EndTime });

        TempData["Success"] = "Thêm suất chiếu thành công.";
        return RedirectToAction(nameof(Calendar));
    }

    /// <summary>
    /// Trang chỉnh sửa suất chiếu
    /// </summary>
    [HttpGet("Edit/{id}")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var vm = await _showtimeService.GetForEditAsync(id);
        if (vm is null) return NotFound();
        return View(vm);
    }

    /// <summary>
    /// Lưu các thay đổi đằng chỉnh sửa suất chiếu
    /// </summary>
    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ShowtimeFormViewModel model)
    {
        // Nạp lại danh sách dropdown để hiển thị lại nếu form bị lỗi
        model.AvailableMovies = await _showtimeService.GetMovieOptionsAsync();
        model.AvailableRooms = await _showtimeService.GetRoomOptionsAsync();

        if (!ModelState.IsValid)
            return View(model);

        var result = await _showtimeService.UpdateAsync(model);
        if (!result.Succeeded)
        {
            // Hiển thị lỗi từ Service (ví dụ: đã có vé nên không thể đổi giờ)
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        await _audit.LogAsync("UPDATE_SHOWTIME", "Showtimes", model.Id,
            newValue: new { model.MovieId, model.RoomId, model.StartTime, model.EndTime, model.Status });

        TempData["Success"] = "Cập nhật suất chiếu thành công.";
        return RedirectToAction(nameof(Calendar));
    }

    /// <summary>
    /// Xóa suất chiếu
    /// </summary>
    [HttpPost("Delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _showtimeService.DeleteAsync(id);
        if (result.Succeeded)
            await _audit.LogAsync("DELETE_SHOWTIME", "Showtimes", id);

        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Đã xóa suất chiếu." : result.Error;
        return RedirectToAction(nameof(Calendar));
    }

    /// <summary>
    /// Hủy suất chiếu
    /// </summary>
    [HttpPost("Cancel/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var result = await _showtimeService.CancelAsync(id);
        if (result.Succeeded)
            await _audit.LogAsync("CANCEL_SHOWTIME", "Showtimes", id);

        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Đã hủy suất chiếu." : result.Error;
        return RedirectToAction(nameof(Calendar));
    }
}
