using Cinema_System.Application.Common;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers.Manager;

[Route("Manager/Promotion")]
[Authorize(Roles = "MANAGER")]
public class ManagerPromotionController : Controller
{
    private readonly IPromotionService _promotionService;

    public ManagerPromotionController(IPromotionService promotionService)
    {
        _promotionService = promotionService;
    }

    [HttpGet("")]
    // Trang danh sách voucher: lọc theo mã/trạng thái + phân trang.
    public async Task<IActionResult> Index(string? search, string? status, int page = 1)
    {
        var vm = await _promotionService.GetAllAsync(search, status, page, PromotionPaging.DefaultPageSize);
        return View(vm);
    }

    [HttpGet("Create")]
    // Hiển thị form tạo voucher mới.
    public IActionResult Create()
    {
        return View(new PromotionFormViewModel());
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    // Xử lý submit form tạo voucher.
    public async Task<IActionResult> Create(PromotionFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _promotionService.CreateAsync(model);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        TempData["Success"] = "Tạo voucher thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id}")]
    // Hiển thị form sửa voucher theo Id.
    public async Task<IActionResult> Edit(Guid id)
    {
        var vm = await _promotionService.GetForEditAsync(id);
        if (vm is null) return NotFound();
        return View(vm);
    }

    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    // Xử lý submit form sửa voucher.
    public async Task<IActionResult> Edit(PromotionFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _promotionService.UpdateAsync(model);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        TempData["Success"] = "Cập nhật voucher thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("ToggleStatus/{id}")]
    [ValidateAntiForgeryToken]
    // Bật/tắt voucher.
    public async Task<IActionResult> ToggleStatus(Guid id)
    {
        var result = await _promotionService.ToggleStatusAsync(id);
        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Đã cập nhật trạng thái voucher." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete/{id}")]
    [ValidateAntiForgeryToken]
    // Xóa voucher khỏi hệ thống.
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _promotionService.DeleteAsync(id);
        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Đã xóa voucher." : result.Error;
        return RedirectToAction(nameof(Index));
    }
}
