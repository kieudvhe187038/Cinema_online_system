using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers;

[Route("Admin/[controller]")]
public class UsersController : Controller
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? search, Guid? roleId, string? status, int page = 1)
    {
        var vm = await _userService.GetUsersAsync(search, roleId, status, page, pageSize: 10);
        return View(vm);
    }

    [HttpGet("Details/{id}")]
    public async Task<IActionResult> Details(Guid id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user is null) return NotFound();

        ViewBag.Roles = await _userService.GetRolesAsync();
        return View(user);
    }

    [HttpGet("Edit/{id}")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var vm = await _userService.GetUserForEditAsync(id);
        if (vm is null) return NotFound();

        return View(vm);
    }

    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Roles = await _userService.GetRolesAsync();
            return View(model);
        }

        var result = await _userService.UpdateUserAsync(model);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            model.Roles = await _userService.GetRolesAsync();
            return View(model);
        }

        TempData["Success"] = "Cập nhật thông tin người dùng thành công.";
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    [HttpPost("ToggleStatus")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(Guid id, bool active)
    {
        var result = await _userService.SetStatusAsync(id, active);
        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded
                ? (active ? "Đã kích hoạt tài khoản." : "Đã vô hiệu hóa tài khoản.")
                : result.Error;

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("AssignRole")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignRole(Guid id, Guid roleId)
    {
        var result = await _userService.AssignRoleAsync(id, roleId);
        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Đã cập nhật vai trò." : result.Error;

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("ResetPassword")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(Guid id)
    {
        var result = await _userService.ResetPasswordAsync(id);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["TempPassword"] = result.Data;
        TempData["Success"] = "Đã đặt lại mật khẩu. Vui lòng gửi mật khẩu tạm cho người dùng.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
