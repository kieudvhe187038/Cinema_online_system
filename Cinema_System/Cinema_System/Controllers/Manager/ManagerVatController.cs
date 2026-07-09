using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers.Manager;

[Route("Manager/Vat")]
public class ManagerVatController : Controller
{
    private readonly IVatService _vatService;

    public ManagerVatController(IVatService vatService)
    {
        _vatService = vatService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var items = await _vatService.GetAllAsync();
        return View(items);
    }

    [HttpGet("Create")]
    public IActionResult Create()
    {
        return View(new VatFormViewModel());
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VatFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _vatService.CreateAsync(model);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        TempData["Success"] = "Tạo cấu hình VAT thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id}")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var vm = await _vatService.GetForEditAsync(id);
        if (vm is null) return NotFound();
        return View(vm);
    }

    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(VatFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _vatService.UpdateAsync(model);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        TempData["Success"] = "Cập nhật cấu hình VAT thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("ToggleStatus/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(Guid id)
    {
        var result = await _vatService.ToggleStatusAsync(id);
        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Đã cập nhật trạng thái VAT." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _vatService.DeleteAsync(id);
        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Đã xóa cấu hình VAT." : result.Error;
        return RedirectToAction(nameof(Index));
    }
}
