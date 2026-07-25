using Cinema_System.Application.Common;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers.Manager;

[Route("Manager/Price")]
[Authorize(Roles = "MANAGER")]
public class ManagerPriceController : Controller
{
    private readonly IPriceService _priceService;
    private readonly IAuditLogWriter _audit;

    public ManagerPriceController(IPriceService priceService, IAuditLogWriter audit)
    {
        _priceService = priceService;
        _audit = audit;
    }

    // Tên bảng DB tương ứng với từng loại cấu hình giá (dùng cho audit log).
    private static string PriceTable(string kind) => kind switch
    {
        PriceKind.Room => "Price_Room_Type_Configs",
        PriceKind.Seat => "Price_Seat_Configs",
        PriceKind.Time => "Price_Time_Configs",
        _ => "Price_Base_Configs"
    };

    [HttpGet("")]
    // Màn quản lý giá: 4 nhóm cấu hình (giá cơ bản / phụ thu phòng / ghế / giờ) theo tab, có phân trang.
    public async Task<IActionResult> Index(string? tab, int page = 1)
    {
        var vm = await _priceService.GetManagementAsync(tab, page, PricePaging.DefaultPageSize);
        return View(vm);
    }

    [HttpGet("Create/{kind}")]
    // Form thêm cấu hình giá mới cho 1 loại.
    public async Task<IActionResult> Create(string kind)
    {
        if (!PriceKind.AllowedValues.Contains(kind)) return NotFound();
        var vm = await _priceService.BuildCreateFormAsync(kind);
        return View(vm);
    }

    [HttpPost("Create/{kind}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string kind, PriceConfigFormViewModel model)
    {
        model.Kind = kind;
        if (!ModelState.IsValid)
        {
            await _priceService.PopulateOptionsAsync(model);
            return View(model);
        }

        var result = await _priceService.CreateAsync(model);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            await _priceService.PopulateOptionsAsync(model);
            return View(model);
        }

        await _audit.LogAsync("CREATE_PRICE", PriceTable(kind),
            newValue: new { kind, model.EffectiveFrom });

        TempData["Success"] = $"Đã thêm cấu hình {PriceKind.Label(kind).ToLowerInvariant()}.";
        return RedirectToAction(nameof(Index), new { tab = kind });
    }

    [HttpGet("Edit/{kind}/{id}")]
    public async Task<IActionResult> Edit(string kind, Guid id)
    {
        if (!PriceKind.AllowedValues.Contains(kind)) return NotFound();
        var vm = await _priceService.GetForEditAsync(kind, id);
        if (vm is null)
        {
            // Không tồn tại hoặc đã hết hạn / đã ngừng → không cho mở form sửa.
            TempData["Error"] = "Không thể sửa: cấu hình giá không tồn tại hoặc đã hết hạn / đã ngừng áp dụng.";
            return RedirectToAction(nameof(Index), new { tab = kind });
        }
        return View(vm);
    }

    [HttpPost("Edit/{kind}/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string kind, Guid id, PriceConfigFormViewModel model)
    {
        model.Kind = kind;
        model.Id = id;
        if (!ModelState.IsValid)
        {
            await _priceService.PopulateOptionsAsync(model);
            return View(model);
        }

        var result = await _priceService.UpdateAsync(model);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            await _priceService.PopulateOptionsAsync(model);
            return View(model);
        }

        await _audit.LogAsync("UPDATE_PRICE", PriceTable(kind), id,
            newValue: new { kind, model.EffectiveFrom });

        TempData["Success"] = $"Đã cập nhật cấu hình {PriceKind.Label(kind).ToLowerInvariant()}.";
        return RedirectToAction(nameof(Index), new { tab = kind });
    }

    // "Xóa" theo nghĩa ngừng áp dụng (soft-delete): đặt ngày kết thúc, không xóa khỏi DB.
    [HttpPost("Delete/{kind}/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string kind, Guid id)
    {
        var result = await _priceService.DeleteAsync(kind, id);
        if (result.Succeeded)
            await _audit.LogAsync("DELETE_PRICE", PriceTable(kind), id, newValue: new { kind });

        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Đã ngừng áp dụng cấu hình giá." : result.Error;
        return RedirectToAction(nameof(Index), new { tab = kind });
    }
}
