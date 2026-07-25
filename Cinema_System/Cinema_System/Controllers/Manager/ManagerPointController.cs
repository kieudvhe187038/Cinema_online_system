using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers.Manager;

[Route("Manager/PointSetting")]
[Authorize(Roles = "MANAGER")]
public class ManagerPointController : Controller
{
    private readonly IPointConfigService _pointConfigService;
    private readonly IAuditLogWriter _audit;

    public ManagerPointController(IPointConfigService pointConfigService, IAuditLogWriter audit)
    {
        _pointConfigService = pointConfigService;
        _audit = audit;
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

        await _audit.LogAsync("UPDATE_POINT_RATE", "SystemConfig",
            newValue: new { model.Rate, model.PointValueVnd });

        TempData["Success"] = "Cập nhật cấu hình điểm thưởng thành công. Thay đổi áp dụng ngay cho đặt vé online và bán vé tại quầy.";
        return RedirectToAction(nameof(Index));
    }
}
