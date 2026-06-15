using AutoMapper;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers
{
    public class ProfileController : Controller
    {
        private readonly IProfileService _profileService;
        private readonly IWebHostEnvironment _env;
        private readonly IMapper _mapper;

        public ProfileController(IProfileService profileService, IWebHostEnvironment env, IMapper mapper)
        {
            _profileService = profileService;
            _env = env;
            _mapper = mapper;
        }

        private Guid GetCurrentUserId()
            => Guid.Parse("00000000-0000-0000-0002-0000000003e9"); // user001 - Nguyễn Anh Linh (Customer)

        public async Task<IActionResult> Index()
        {
            var profile = await _profileService.GetProfileAsync(GetCurrentUserId());
            if (profile == null) return NotFound("Không tìm thấy người dùng");

            var profileView = _mapper.Map<ProfileViewModel>(profile);
            return View(profileView);
        }

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
            if (editForm.AvatarFile != null && editForm.AvatarFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(editForm.AvatarFile.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("AvatarFile", "Chỉ chấp nhận ảnh jpg, jpeg, png, gif");
                    return View(editForm);
                }

                // Giới hạn DUNG LƯỢNG ảnh: tối đa 2MB
                const long maxBytes = 2 * 1024 * 1024; // 2MB = 2 * 1024 * 1024 byte
                if (editForm.AvatarFile.Length > maxBytes)
                {
                    ModelState.AddModelError("AvatarFile", "Ảnh quá lớn — tối đa 2MB");
                    return View(editForm);
                }

                // Giới hạn KÍCH THƯỚC ảnh (pixel): tối đa 1024 x 1024
                using (var imageStream = editForm.AvatarFile.OpenReadStream())
                using (var image = System.Drawing.Image.FromStream(imageStream))
                {
                    const int maxWidth = 1024, maxHeight = 1024;
                    if (image.Width > maxWidth || image.Height > maxHeight)
                    {
                        ModelState.AddModelError("AvatarFile",
                            $"Kích thước ảnh tối đa {maxWidth}x{maxHeight}px (ảnh của bạn {image.Width}x{image.Height}px)");
                        return View(editForm);
                    }
                }

                var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "avatars");
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

            TempData["Success"] = "Cập nhật hồ sơ thành công!";
            return RedirectToAction("Index");
        }

        public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

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

        // ===== 5. XEM LỊCH SỬ ĐIỂM (có phân trang) =====
        public async Task<IActionResult> PointHistory(int page = 1)
        {
            const int pageSize = 5;

            // Lấy toàn bộ rồi map sang ViewModel
            var pointHistory = await _profileService.GetPointHistoryAsync(GetCurrentUserId());
            var allRecords = pointHistory.Select(record => _mapper.Map<PointHistoryViewModel>(record)).ToList();

            var totalRecords = allRecords.Count;
            var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            // Chặn page vượt biên
            if (page < 1) page = 1;
            if (totalPages > 0 && page > totalPages) page = totalPages;

            // Cắt đúng phần của trang hiện tại
            var pageRecords = allRecords
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var pageModel = new PointHistoryPageViewModel
            {
                Items = pageRecords,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalItems = totalRecords
            };
            return View(pageModel);
        }
    }
}
