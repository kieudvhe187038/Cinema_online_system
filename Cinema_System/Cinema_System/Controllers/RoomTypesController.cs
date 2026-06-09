using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers;

[Route("Manager/[controller]")]
public class RoomTypesController : Controller
{
    private readonly IRoomTypeService _roomTypeService;

    public RoomTypesController(IRoomTypeService roomTypeService)
    {
        _roomTypeService = roomTypeService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var roomTypes = await _roomTypeService.GetAllAsync();
        return View(roomTypes);
    }

    [HttpGet("Create")]
    public IActionResult Create()
    {
        return View(new RoomTypeFormViewModel());
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RoomTypeFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _roomTypeService.CreateAsync(model);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        TempData["Success"] = "Thêm loại phòng thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id}")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var vm = await _roomTypeService.GetForEditAsync(id);
        if (vm is null) return NotFound();

        return View(vm);
    }

    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(RoomTypeFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _roomTypeService.UpdateAsync(model);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        TempData["Success"] = "Cập nhật loại phòng thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _roomTypeService.DeleteAsync(id);
        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Đã xóa loại phòng." : result.Error;

        return RedirectToAction(nameof(Index));
    }
}
