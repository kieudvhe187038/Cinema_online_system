using Cinema_System.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers.Manager;

// Trang Báo cáo & Thống kê: doanh thu, vé bán, biểu đồ KPI. Chỉ MANAGER truy cập.
[Route("Manager/Report")]
[Authorize(Roles = "MANAGER")]
public class ReportController : Controller
{
    private readonly IReportService _reportService;

    public ReportController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(DateOnly? from, DateOnly? to)
    {
        var vm = await _reportService.GetReportAsync(from, to);
        return View(vm);
    }

    // Xuất báo cáo (theo đúng khoảng ngày đang xem) ra file Excel .xlsx.
    [HttpGet("Export")]
    public async Task<IActionResult> Export(DateOnly? from, DateOnly? to)
    {
        var (content, fileName) = await _reportService.ExportExcelAsync(from, to);
        return File(content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
}
