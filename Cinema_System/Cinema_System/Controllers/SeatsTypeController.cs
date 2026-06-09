using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers;

[Route("Manager/[controller]")]
public class SeatsTypeController : Controller
{
    private readonly ISeatTypeService _seatTypeService;

    public SeatsTypeController(ISeatTypeService seatTypeService)
    {
        _seatTypeService = seatTypeService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var seatTypes = await _seatTypeService.GetAllAsync();
        return View(seatTypes);
    }

    [HttpGet("Create")]
    public IActionResult Create()
    {
        return View(new SeatTypeFormViewModel());
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SeatTypeFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _seatTypeService.CreateAsync(model);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        TempData["Success"] = "Thêm loại ghế thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id}")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var vm = await _seatTypeService.GetForEditAsync(id);
        if (vm is null) return NotFound();

        return View(vm);
    }

    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(SeatTypeFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _seatTypeService.UpdateAsync(model);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        TempData["Success"] = "Cập nhật loại ghế thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _seatTypeService.DeleteAsync(id);
        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Đã xóa loại ghế." : result.Error;

        return RedirectToAction(nameof(Index));
    }
}
