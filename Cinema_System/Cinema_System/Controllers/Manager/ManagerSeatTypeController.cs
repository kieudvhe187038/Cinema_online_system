using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers.Manager;

[Route("Manager/SeatType")]
[Authorize(Roles = "MANAGER")]
public class ManagerSeatTypeController : Controller
{
    private readonly ISeatTypeService _seatTypeService;
    private readonly IAuditLogWriter _audit;

    public ManagerSeatTypeController(ISeatTypeService seatTypeService, IAuditLogWriter audit)
    {
        _seatTypeService = seatTypeService;
        _audit = audit;
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

        await _audit.LogAsync("CREATE_SEAT_TYPE", "Seat_Types",
            newValue: new { model.Name, model.Capacity });

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

        await _audit.LogAsync("UPDATE_SEAT_TYPE", "Seat_Types", model.Id,
            newValue: new { model.Name, model.Capacity });

        TempData["Success"] = "Cập nhật loại ghế thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _seatTypeService.DeleteAsync(id);
        if (result.Succeeded)
            await _audit.LogAsync("DELETE_SEAT_TYPE", "Seat_Types", id);

        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Đã xóa loại ghế." : result.Error;

        return RedirectToAction(nameof(Index));
    }
}
