using Cinema_System.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers.Manager;

// Trang Tổng quan quản lý: KPI + booking gần đây. Chỉ MANAGER truy cập.
[Route("Manager/Dashboard")]
[Authorize(Roles = "MANAGER")]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var vm = await _dashboardService.GetDashboardAsync();
        return View(vm);
    }
}
