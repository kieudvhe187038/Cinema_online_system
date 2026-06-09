using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers;

[Route("Manager/[controller]")]
public class FoodBeveragesController : Controller
{
    private readonly IFoodBeverageService _fbService;

    public FoodBeveragesController(IFoodBeverageService fbService)
    {
        _fbService = fbService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, string? status, int page = 1)
    {
        var vm = await _fbService.GetAllAsync(search, status, page, pageSize: 10);
        return View(vm);
    }

    [HttpGet("Create")]
    public IActionResult Create()
    {
        return View(new FoodBeverageFormViewModel());
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FoodBeverageFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _fbService.CreateAsync(model);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        TempData["Success"] = "Thêm món thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id}")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var vm = await _fbService.GetForEditAsync(id);
        if (vm is null) return NotFound();
        return View(vm);
    }

    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(FoodBeverageFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _fbService.UpdateAsync(model);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        TempData["Success"] = "Cập nhật món thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("ToggleVisibility/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleVisibility(Guid id)
    {
        var result = await _fbService.ToggleVisibilityAsync(id);
        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Đã cập nhật trạng thái hiển thị." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _fbService.DeleteAsync(id);
        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Đã xóa món khỏi menu." : result.Error;
        return RedirectToAction(nameof(Index));
    }
}
