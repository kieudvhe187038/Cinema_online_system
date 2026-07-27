using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers.Manager;

[Route("Manager/Genre")]
[Authorize(Roles = "MANAGER")]
public class ManagerGenreController : Controller
{
    private const int PageSize = 8; // số thể loại mỗi trang

    private readonly IGenreService _genreService;
    private readonly IAuditLogWriter _audit;

    public ManagerGenreController(IGenreService genreService, IAuditLogWriter audit)
    {
        _genreService = genreService;
        _audit = audit;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        var vm = await _genreService.GetPagedAsync(search, page, PageSize);
        ViewData["Search"] = search;
        return View(vm);
    }

    [HttpGet("Create")]
    public IActionResult Create()
    {
        return View(new GenreFormViewModel());
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(GenreFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _genreService.CreateAsync(model);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        await _audit.LogAsync("CREATE_GENRE", "Genres", newValue: new { model.Name });

        TempData["Success"] = "Thêm thể loại thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id}")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var vm = await _genreService.GetForEditAsync(id);
        if (vm is null) return NotFound();

        return View(vm);
    }

    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(GenreFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _genreService.UpdateAsync(model);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        await _audit.LogAsync("UPDATE_GENRE", "Genres", model.Id, newValue: new { model.Name });

        TempData["Success"] = "Cập nhật thể loại thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _genreService.DeleteAsync(id);
        if (result.Succeeded)
            await _audit.LogAsync("DELETE_GENRE", "Genres", id);

        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Đã xóa thể loại." : result.Error;

        return RedirectToAction(nameof(Index));
    }
}
