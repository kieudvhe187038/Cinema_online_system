using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers;

[Route("Manager/Movies")]
public class MovieManagementController : Controller
{
    private readonly IMovieService _movieService;

    public MovieManagementController(IMovieService movieService)
    {
        _movieService = movieService;
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
    public async Task<IActionResult> Create(MovieFormViewModel model)
    {
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
    public async Task<IActionResult> Edit(MovieFormViewModel model)
    {
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
}
