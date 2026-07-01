using System.Security.Claims;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers.Manager
{
    // Manager khai báo sự cố suất chiếu + hoàn điểm. Chỉ MANAGER/ADMIN.
    [Authorize(Roles = "MANAGER,ADMIN")]
    [Route("Manager/Incident")]
    public class ManagerIncidentController : Controller
    {
        private readonly IShowtimeIncidentService _incidentService;

        public ManagerIncidentController(IShowtimeIncidentService incidentService)
        {
            _incidentService = incidentService;
        }

        // Lấy Id manager đang đăng nhập từ Claims
        private Guid GetCurrentUserId()
            => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet("")]
        public async Task<IActionResult> Index(string? scope, int page = 1)
        {
            var vm = await _incidentService.GetIndexAsync(scope, page);
            return View(vm);
        }

        [HttpPost("Restore")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(Guid showtimeId, string? scope)
        {
            var result = await _incidentService.RestoreShowtimeAsync(showtimeId);
            TempData[result.Succeeded ? "Success" : "Error"] =
                result.Succeeded ? "Đã khôi phục suất chiếu về trạng thái Đã lên lịch." : result.Error;
            return RedirectToAction(nameof(Index), new { scope });
        }

        [HttpGet("Declare")]
        public async Task<IActionResult> Declare(Guid? showtimeId)
        {
            var vm = await _incidentService.BuildDeclareFormAsync(showtimeId);
            return View(vm);
        }

        [HttpPost("Declare")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Declare(DeclareIncidentViewModel form)
        {
            // Nạp lại dropdown + giữ dữ liệu đã nhập khi cần hiển thị lại form
            async Task<DeclareIncidentViewModel> RebuildAsync()
            {
                var vm = await _incidentService.BuildDeclareFormAsync(form.ShowtimeId);
                vm.Description = form.Description;
                vm.RefundPointsRate = form.RefundPointsRate;
                vm.CompensationPromoId = form.CompensationPromoId;
                vm.CancelShowtime = form.CancelShowtime;
                return vm;
            }

            if (!ModelState.IsValid)
                return View(await RebuildAsync());

            var result = await _incidentService.DeclareAsync(form, GetCurrentUserId());
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Error!);
                return View(await RebuildAsync());
            }

            TempData["Success"] = "Đã khai báo sự cố và hoàn điểm cho khách hàng.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("Maintain")]
        public async Task<IActionResult> Maintain()
        {
            var vm = await _incidentService.BuildMaintainFormAsync();
            return View(vm);
        }

        [HttpPost("Maintain")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Maintain(MaintainRoomViewModel form)
        {
            async Task<MaintainRoomViewModel> RebuildAsync()
            {
                var vm = await _incidentService.BuildMaintainFormAsync();
                vm.RoomId = form.RoomId;
                vm.FromTime = form.FromTime;
                vm.ToTime = form.ToTime;
                vm.Description = form.Description;
                vm.RefundPointsRate = form.RefundPointsRate;
                vm.CompensationPromoId = form.CompensationPromoId;
                return vm;
            }

            if (!ModelState.IsValid)
                return View(await RebuildAsync());

            var result = await _incidentService.MaintainRoomAsync(form, GetCurrentUserId());
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Error!);
                return View(await RebuildAsync());
            }

            TempData["Success"] = result.Data;
            return RedirectToAction(nameof(Index));
        }

    }
}