using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers;

[Route("Manager/PointSetting")]
public class PointsController : Controller
{
    private readonly IPointConfigService _pointConfigService;

    public PointsController(IPointConfigService pointConfigService)
    {
        _pointConfigService = pointConfigService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var vm = await _pointConfigService.GetRateAsync();
        return View(vm);
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(PointRateViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _pointConfigService.UpdateRateAsync(model);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        TempData["Success"] = "Cập nhật tỉ lệ tích điểm thành công.";
        return RedirectToAction(nameof(Index));
    }
}
