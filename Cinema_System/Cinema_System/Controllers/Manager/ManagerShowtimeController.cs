using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers.Manager;

/// <summary>
/// Controller quản lý suất chiếu phim
/// </summary>
[Route("Manager/Showtime")]
public class ManagerShowtimeController : Controller
{
    private readonly IShowtimeScheduleService _showtimeService;

    public ManagerShowtimeController(IShowtimeScheduleService showtimeService)
    {
        _showtimeService = showtimeService;
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
        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Đã hủy suất chiếu." : result.Error;
        return RedirectToAction(nameof(Calendar));
    }
}
