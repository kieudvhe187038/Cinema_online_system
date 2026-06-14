using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers;

[Route("Manager/[controller]")]
public class FoodBeveragesController : Controller
{
    private readonly IFoodBeverageService _fbService;
    private readonly IWebHostEnvironment _env;

    private const long MaxImageBytes = 10 * 1024 * 1024; // 10MB cho ảnh
    private static readonly string[] ImageExts = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

    public FoodBeveragesController(IFoodBeverageService fbService, IWebHostEnvironment env)
    {
        _fbService = fbService;
        _env = env;
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
    [RequestSizeLimit(MaxImageBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxImageBytes)]
    public async Task<IActionResult> Create(FoodBeverageFormViewModel model)
    {
        await HandleUploadAsync(model);

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
    [RequestSizeLimit(MaxImageBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxImageBytes)]
    public async Task<IActionResult> Edit(FoodBeverageFormViewModel model)
    {
        await HandleUploadAsync(model);

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

    // ── File upload ──────────────────────────────────────────────────────

    // Lưu ảnh upload vào wwwroot/uploads/foods, gán đường dẫn vào model.
    // Không upload ảnh mới thì giữ nguyên ImageUrl cũ (đã có trong hidden field).
    private async Task HandleUploadAsync(FoodBeverageFormViewModel model)
    {
        var file = model.ImageFile;
        if (file is null || file.Length == 0)
            return;

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!ImageExts.Contains(ext))
        {
            ModelState.AddModelError(nameof(model.ImageFile),
                $"Định dạng không hợp lệ. Cho phép: {string.Join(", ", ImageExts)}");
            return;
        }

        var folder = Path.Combine(_env.WebRootPath, "uploads", "foods");
        Directory.CreateDirectory(folder);

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(folder, fileName);

        await using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        model.ImageUrl = $"/uploads/foods/{fileName}";
    }
}
