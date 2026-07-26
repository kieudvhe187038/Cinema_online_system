using System.Security.Claims;
using AutoMapper;
using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers.Customer;

/// <summary>
/// Tầng Presentation module Hồ sơ: nhận request -> gọi Service -> trả View.
/// Không chứa nghiệp vụ, không đụng DbContext. Bắt buộc đăng nhập.
/// </summary>
[Authorize]
public class ProfileController : Controller
{
    private const int PointHistoryPageSize = 5;

    private readonly IProfileService _profileService;
    private readonly IWebHostEnvironment _env; // lấy đường dẫn wwwroot để lưu ảnh
    private readonly IMapper _mapper;
    private readonly IPointConfigService _pointConfigService;

    public ProfileController(IProfileService profileService, IWebHostEnvironment env, IMapper mapper,
        IPointConfigService pointConfigService)
    {
        _profileService = profileService;
        _env = env;
        _mapper = mapper;
        _pointConfigService = pointConfigService;
    }

    // Lấy Id user đang đăng nhập từ Claims (LoginController set ClaimTypes.NameIdentifier khi đăng nhập).
    private Guid GetCurrentUserId()
        => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Xem hồ sơ
    public async Task<IActionResult> Index()
    {
        var profile = await _profileService.GetProfileAsync(GetCurrentUserId());
        if (profile == null) return NotFound("Không tìm thấy người dùng");

        var profileView = _mapper.Map<ProfileViewModel>(profile);
        return View(profileView);
    }

    // Mở form chỉnh sửa, nạp sẵn dữ liệu hiện tại
    public async Task<IActionResult> Edit()
    {
        var profile = await _profileService.GetProfileAsync(GetCurrentUserId());
        if (profile == null) return NotFound();

        var editForm = new UpdateProfileViewModel
        {
            Id = profile.Id,
            FullName = profile.FullName,
            Phone = profile.Phone,
            Email = profile.Email,
            CurrentAvatarUrl = profile.AvatarUrl
        };
        return View(editForm);
    }

    // Lưu chỉnh sửa: validate -> xử lý ảnh -> cập nhật
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateProfileViewModel editForm)
    {
        if (!ModelState.IsValid)
        {
            var currentProfile = await _profileService.GetProfileAsync(GetCurrentUserId());
            editForm.CurrentAvatarUrl = currentProfile?.AvatarUrl;
            return View(editForm);
        }

        string? savedAvatarUrl = null;
        string? oldAvatarUrl = null;
        if (editForm.AvatarFile != null && editForm.AvatarFile.Length > 0)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(editForm.AvatarFile.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError("AvatarFile", "Chỉ chấp nhận ảnh jpg, jpeg, png, gif");
                return View(editForm);
            }

            const long maxBytes = 2 * 1024 * 1024; // 2MB
            if (editForm.AvatarFile.Length > maxBytes)
            {
                ModelState.AddModelError("AvatarFile", "Ảnh quá lớn — tối đa 2MB");
                return View(editForm);
            }

            // Giới hạn kích thước pixel: đọc Width/Height từ ảnh (using để tự giải phóng).
            // System.Drawing chỉ chạy trên Windows nên chỉ kiểm tra khi đang chạy Windows
            // (deploy ở môi trường khác vẫn còn chặn theo dung lượng 2MB ở trên).
            // Bọc try/catch vì file giả ảnh (đổi đuôi) sẽ làm FromStream ném lỗi -> tránh web 500.
            if (OperatingSystem.IsWindowsVersionAtLeast(6, 1))
            {
                try
                {
                    using var imageStream = editForm.AvatarFile.OpenReadStream();
                    using var image = System.Drawing.Image.FromStream(imageStream);
                    const int maxWidth = 1024, maxHeight = 1024;
                    if (image.Width > maxWidth || image.Height > maxHeight)
                    {
                        ModelState.AddModelError("AvatarFile",
                            $"Kích thước ảnh tối đa {maxWidth}x{maxHeight}px (ảnh của bạn {image.Width}x{image.Height}px)");
                        return View(editForm);
                    }
                }
                catch
                {
                    ModelState.AddModelError("AvatarFile", "File ảnh không hợp lệ hoặc bị hỏng");
                    return View(editForm);
                }
            }

            // Lưu file tên ngẫu nhiên (tránh trùng) vào wwwroot/uploads/avatars
            var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "avatars");

            // Nhớ ảnh cũ để xoá sau khi cập nhật thành công
            var currentProfile = await _profileService.GetProfileAsync(GetCurrentUserId());
            oldAvatarUrl = currentProfile?.AvatarUrl;

            Directory.CreateDirectory(uploadFolder);
            var newFileName = Guid.NewGuid().ToString() + extension;
            using (var fileStream = new FileStream(Path.Combine(uploadFolder, newFileName), FileMode.Create))
            {
                await editForm.AvatarFile.CopyToAsync(fileStream);
            }
            savedAvatarUrl = "/uploads/avatars/" + newFileName;
        }

        var updateData = new UpdateProfileDto { FullName = editForm.FullName, Phone = editForm.Phone, AvatarUrl = savedAvatarUrl };
        var isUpdated = await _profileService.UpdateProfileAsync(GetCurrentUserId(), updateData);
        if (!isUpdated) return NotFound();

        // Xoá ảnh cũ để không tồn file thừa (chỉ xoá ảnh upload nội bộ)
        if (!string.IsNullOrEmpty(savedAvatarUrl)
            && !string.IsNullOrEmpty(oldAvatarUrl)
            && oldAvatarUrl.StartsWith("/uploads/avatars/"))
        {
            var oldPath = Path.Combine(_env.WebRootPath, oldAvatarUrl.TrimStart('/'));
            if (System.IO.File.Exists(oldPath))
                System.IO.File.Delete(oldPath);
        }

        TempData["Success"] = "Cập nhật hồ sơ thành công!";
        return RedirectToAction("Index"); // PRG: tránh submit lại khi F5
    }

    // Mở form đổi mật khẩu
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    // Xử lý đổi mật khẩu
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel passwordForm)
    {
        if (!ModelState.IsValid) return View(passwordForm);

        var (isChanged, errorMessage) = await _profileService.ChangePasswordAsync(
            GetCurrentUserId(), passwordForm.OldPassword, passwordForm.NewPassword);

        if (!isChanged)
        {
            ModelState.AddModelError("OldPassword", errorMessage ?? "Đổi mật khẩu thất bại");
            return View(passwordForm);
        }

        TempData["Success"] = "Đổi mật khẩu thành công!";
        return RedirectToAction("Index");
    }

    // Xem lịch sử điểm (phân trang 5 dòng/trang, dùng chung PagedResult)
    public async Task<IActionResult> PointHistory(int page = 1)
    {
        var userId = GetCurrentUserId();
        var profile = await _profileService.GetProfileAsync(userId);   // lấy tổng điểm hiện có
        var pointHistory = await _profileService.GetPointHistoryAsync(userId);
        var allRecords = pointHistory.Select(record => _mapper.Map<PointHistoryViewModel>(record));

        var paged = PagedResult<PointHistoryViewModel>.Create(allRecords, page, PointHistoryPageSize);

        // Chính sách điểm đang áp dụng (Manager cấu hình) để hiển thị cách tích/dùng điểm.
        var pointRate = await _pointConfigService.GetRateAsync();

        var pageModel = new PointHistoryPageViewModel
        {
            Items = paged.Items.ToList(),
            CurrentPage = paged.CurrentPage,
            TotalPages = paged.TotalPages,
            TotalItems = paged.TotalCount,
            CurrentPoints = profile?.RewardPoints ?? 0,
            VndPerPoint = pointRate.VndPerPoint,
            PointValueVnd = pointRate.PointValueVnd
        };
        return View(pageModel);
    }
}
