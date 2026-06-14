using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers;

[Route("Manager/Movies")]
public class MovieManagementController : Controller
{
    private readonly IMovieService _movieService;
    private readonly IWebHostEnvironment _env;

    // Giới hạn upload: 100MB cho video trailer
    private const long MaxUploadBytes = 100 * 1024 * 1024;
    private static readonly string[] ImageExts = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    private static readonly string[] VideoExts = { ".mp4", ".webm", ".ogg", ".mov" };

    public MovieManagementController(IMovieService movieService, IWebHostEnvironment env)
    {
        _movieService = movieService;
        _env = env;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, string? status, string? genre, int page = 1)
    {
        var vm = await _movieService.GetMoviesForAdminAsync(search, status, genre, page, pageSize: 5);
        return View(vm);
    }

    [HttpGet("Create")]
    public async Task<IActionResult> Create()
    {
        var vm = new MovieFormViewModel
        {
            AvailableGenres = (await _movieService.GetAllGenresAsync()).ToList()
        };
        return View(vm);
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxUploadBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
    public async Task<IActionResult> Create(MovieFormViewModel model)
    {
        await HandleUploadsAsync(model);

        if (!ModelState.IsValid)
        {
            model.AvailableGenres = (await _movieService.GetAllGenresAsync()).ToList();
            return View(model);
        }

        var result = await _movieService.CreateAsync(model);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            model.AvailableGenres = (await _movieService.GetAllGenresAsync()).ToList();
            return View(model);
        }

        TempData["Success"] = "Thêm phim thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id}")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var vm = await _movieService.GetForEditAsync(id);
        if (vm is null) return NotFound();
        return View(vm);
    }

    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxUploadBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
    public async Task<IActionResult> Edit(MovieFormViewModel model)
    {
        await HandleUploadsAsync(model);

        if (!ModelState.IsValid)
        {
            model.AvailableGenres = (await _movieService.GetAllGenresAsync()).ToList();
            return View(model);
        }

        var result = await _movieService.UpdateAsync(model);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            model.AvailableGenres = (await _movieService.GetAllGenresAsync()).ToList();
            return View(model);
        }

        TempData["Success"] = "Cập nhật phim thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("ToggleStatus/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(Guid id)
    {
        var result = await _movieService.ToggleStatusAsync(id);
        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Đã cập nhật trạng thái phim." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    // ── File upload helpers ──────────────────────────────────────────────

    // Lưu các file được upload vào wwwroot/uploads và gán đường dẫn vào model.
    // Nếu không upload file mới thì giữ nguyên đường dẫn cũ (đã có trong hidden field).
    private async Task HandleUploadsAsync(MovieFormViewModel model)
    {
        var poster = await SaveFileAsync(model.PosterFile, "posters", ImageExts, nameof(model.PosterFile));
        if (poster != null) model.PosterUrl = poster;

        var banner = await SaveFileAsync(model.BannerFile, "banners", ImageExts, nameof(model.BannerFile));
        if (banner != null) model.BannerUrl = banner;

        var trailer = await SaveFileAsync(model.TrailerFile, "trailers", VideoExts, nameof(model.TrailerFile));
        if (trailer != null) model.TrailerUrl = trailer;
    }

    // Trả về đường dẫn tương đối (vd "/uploads/posters/xxx.jpg") nếu lưu thành công, null nếu không có file.
    private async Task<string?> SaveFileAsync(IFormFile? file, string subFolder, string[] allowedExts, string fieldKey)
    {
        if (file is null || file.Length == 0)
            return null;

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExts.Contains(ext))
        {
            ModelState.AddModelError(fieldKey,
                $"Định dạng không hợp lệ. Cho phép: {string.Join(", ", allowedExts)}");
            return null;
        }

        var folder = Path.Combine(_env.WebRootPath, "uploads", subFolder);
        Directory.CreateDirectory(folder);

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(folder, fileName);

        await using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return $"/uploads/{subFolder}/{fileName}";
    }
}
