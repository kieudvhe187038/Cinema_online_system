using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers.Manager;

[Route("Manager/Room")]
public class ManagerRoomController : Controller
{
    private readonly IRoomService _roomService;

    public ManagerRoomController(IRoomService roomService)
    {
        _roomService = roomService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var rooms = await _roomService.GetAllAsync();
        return View(rooms);
    }

    [HttpGet("Create")]
    public async Task<IActionResult> Create()
    {
        return View(await _roomService.BuildCreateFormAsync());
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RoomFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await _roomService.PopulateOptionsAsync(model);
            return View(model);
        }

        var result = await _roomService.CreateAsync(model);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            await _roomService.PopulateOptionsAsync(model);
            return View(model);
        }

        TempData["Success"] = "Thêm phòng chiếu thành công. Hãy bố trí sơ đồ ghế cho phòng.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id}")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var vm = await _roomService.GetForEditAsync(id);
        if (vm is null) return NotFound();

        return View(vm);
    }

    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(RoomFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await _roomService.PopulateOptionsAsync(model);
            return View(model);
        }

        var result = await _roomService.UpdateAsync(model);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            await _roomService.PopulateOptionsAsync(model);
            return View(model);
        }

        TempData["Success"] = result.Data ?? "Cập nhật phòng chiếu thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _roomService.DeleteAsync(id);
        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Đã xóa phòng chiếu." : result.Error;

        return RedirectToAction(nameof(Index));
    }
}
