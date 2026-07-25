using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers.Manager;

[Route("Manager/[controller]")]
[Authorize(Roles = "MANAGER")]   // Chỉ Quản lý rạp mới được quản lý đồ ăn & nước.
public class FoodBeveragesController : Controller
{
    private readonly IFoodBeverageService _fbService;
    private readonly IWebHostEnvironment _env;
    private readonly IAuditLogWriter _audit;

    private const long MaxUploadBytes = 10 * 1024 * 1024; // giới hạn request
    private const long MaxImageBytes = 2 * 1024 * 1024;   // mỗi ảnh tối đa 2MB
    private static readonly string[] ImageExts = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

    public FoodBeveragesController(IFoodBeverageService fbService, IWebHostEnvironment env, IAuditLogWriter audit)
    {
        _fbService = fbService;
        _env = env;
        _audit = audit;
    }

    [HttpGet("")]
    // Trang danh sách món: lọc theo tên/tình trạng + phân trang.
    public async Task<IActionResult> Index(string? search, string? status, int page = 1)
    {
        var vm = await _fbService.GetAllAsync(search, status, page, pageSize: 10);
        return View(vm);
    }

    [HttpGet("Create")]
    // Hiển thị form thêm món mới.
    public IActionResult Create()
    {
        return View(new FoodBeverageFormViewModel());
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxUploadBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
    // Xử lý submit form thêm món (upload ảnh + lưu DB).
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

        await _audit.LogAsync("CREATE_FOOD", "Food_Beverages",
            newValue: new { model.Name, model.Price, model.StockStatus });

        TempData["Success"] = "Thêm món thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id}")]
    // Hiển thị form sửa món theo Id.
    public async Task<IActionResult> Edit(Guid id)
    {
        var vm = await _fbService.GetForEditAsync(id);
        if (vm is null) return NotFound();
        return View(vm);
    }

    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxUploadBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
    // Xử lý submit form sửa món (upload ảnh mới nếu có + cập nhật DB).
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

        await _audit.LogAsync("UPDATE_FOOD", "Food_Beverages", model.Id,
            newValue: new { model.Name, model.Price, model.StockStatus });

        TempData["Success"] = "Cập nhật món thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("ToggleVisibility/{id}")]
    [ValidateAntiForgeryToken]
    // Ẩn/hiện món khỏi menu.
    public async Task<IActionResult> ToggleVisibility(Guid id)
    {
        var result = await _fbService.ToggleVisibilityAsync(id);
        if (result.Succeeded)
            await _audit.LogAsync("TOGGLE_FOOD_VISIBILITY", "Food_Beverages", id);

        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Đã cập nhật trạng thái hiển thị." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete/{id}")]
    [ValidateAntiForgeryToken]
    // Xóa món khỏi menu và dọn luôn file ảnh kèm theo.
    public async Task<IActionResult> Delete(Guid id)
    {
        // Lấy đường dẫn ảnh trước khi xóa record để dọn file sau khi xóa thành công.
        var item = await _fbService.GetForEditAsync(id);

        var result = await _fbService.DeleteAsync(id);
        if (result.Succeeded)
        {
            DeleteFile(item?.ImageUrl);
            await _audit.LogAsync("DELETE_FOOD", "Food_Beverages", id,
                oldValue: item is null ? null : new { item.Name, item.Price });
        }

        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Đã xóa món khỏi menu." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    // ── File upload ──────────────────────────────────────────────────────

    // Lưu ảnh upload vào wwwroot/uploads/foods, gán đường dẫn vào model.
    // Không upload ảnh mới thì giữ nguyên ImageUrl cũ (đã có trong hidden field).
    // Khi thay ảnh mới thì xóa luôn ảnh cũ để tránh file rác.
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

        if (file.Length > MaxImageBytes)
        {
            ModelState.AddModelError(nameof(model.ImageFile), "Ảnh tối đa 2MB.");
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

        DeleteFile(model.ImageUrl);
        model.ImageUrl = $"/uploads/foods/{fileName}";
    }

    // Xóa file cũ trong wwwroot/uploads. Chỉ xóa file nội bộ (đường dẫn "/uploads/..."),
    // bỏ qua URL ngoài (http...) của dữ liệu cũ.
    private void DeleteFile(string? relativePath)
    {
        if (string.IsNullOrEmpty(relativePath) || !relativePath.StartsWith("/uploads/"))
            return;

        var fullPath = Path.Combine(_env.WebRootPath,
            relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(fullPath))
            System.IO.File.Delete(fullPath);
    }
}
