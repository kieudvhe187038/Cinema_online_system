using Cinema_System.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers.Admin;

// Dashboard quản trị - CHỈ ADMIN.
[Authorize(Roles = "ADMIN")]
[Route("Admin/Dashboard")]
public class AdminDashboardController : Controller
{
    private readonly IAdminDashboardService _dashboardService;

    public AdminDashboardController(IAdminDashboardService dashboardService)
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
