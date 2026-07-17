using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers.Manager;

[Route("Manager/Vat")]
[Authorize(Roles = "MANAGER")]
public class ManagerVatController : Controller
{
    private readonly IVatService _vatService;
    private readonly IAuditLogWriter _audit;

    public ManagerVatController(IVatService vatService, IAuditLogWriter audit)
    {
        _vatService = vatService;
        _audit = audit;
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

        await _audit.LogAsync("CREATE_VAT", "VAT",
            newValue: new { model.VatRate, model.Status });

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

        await _audit.LogAsync("UPDATE_VAT", "VAT", model.Id,
            newValue: new { model.VatRate, model.Status });

        TempData["Success"] = "Cập nhật cấu hình VAT thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("ToggleStatus/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(Guid id)
    {
        var result = await _vatService.ToggleStatusAsync(id);
        if (result.Succeeded)
            await _audit.LogAsync("TOGGLE_VAT_STATUS", "VAT", id);

        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Đã cập nhật trạng thái VAT." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _vatService.DeleteAsync(id);
        if (result.Succeeded)
            await _audit.LogAsync("DELETE_VAT", "VAT", id);

        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Đã xóa cấu hình VAT." : result.Error;
        return RedirectToAction(nameof(Index));
    }
}
