using Cinema_System.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers.Admin;

// Nhật ký chỉnh sửa dữ liệu (Audit Log) — CHỈ ADMIN. Chỉ đọc: danh sách + chi tiết.
[Route("Admin/AuditLog")]
[Authorize(Roles = "ADMIN")]
public class AuditLogController : Controller
{
    private const int PageSize = 15;

    private readonly IAuditLogService _auditLogService;

    public AuditLogController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    // #49 — Danh sách log
    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        var vm = await _auditLogService.GetLogsAsync(search, page, PageSize);
        return View(vm);
    }

    // #50 — Chi tiết log
    [HttpGet("Details/{id}")]
    public async Task<IActionResult> Details(Guid id)
    {
        var vm = await _auditLogService.GetLogDetailAsync(id);
        if (vm is null) return NotFound();

        return View(vm);
    }
}
