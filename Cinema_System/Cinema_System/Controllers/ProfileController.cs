using AutoMapper;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers
{
    // Controller GỌN: chỉ nhận request, gọi Service, trả View. KHÔNG đụng DbContext.
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

        // Chưa có Login => tạm hardcode staff4. Sau đổi sang Session/Claims.
        private Guid GetCurrentUserId()
            => Guid.Parse("00000000-0000-0000-0002-00000000000e");

        // ===== 1. XEM HỒ SƠ =====
        public async Task<IActionResult> Index()
        {
            var dto = await _profileService.GetProfileAsync(GetCurrentUserId());
            if (dto == null) return NotFound("Không tìm thấy người dùng");

            var vm = _mapper.Map<ProfileViewModel>(dto);
            return View(vm);
        }

        // ===== 2. CẬP NHẬT HỒ SƠ - hiện form =====
        public async Task<IActionResult> Edit()
        {
            var dto = await _profileService.GetProfileAsync(GetCurrentUserId());
            if (dto == null) return NotFound();

            var vm = new UpdateProfileViewModel
            {
                Id = dto.Id,
                FullName = dto.FullName,
                Phone = dto.Phone,
                Email = dto.Email,
                CurrentAvatarUrl = dto.AvatarUrl
            };
            return View(vm);
        }

        // ===== 2+3. LƯU CẬP NHẬT + UPLOAD AVATAR =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UpdateProfileViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                var cur = await _profileService.GetProfileAsync(GetCurrentUserId());
                vm.CurrentAvatarUrl = cur?.AvatarUrl;
                return View(vm);
            }

            // Lưu file ảnh (việc IO của tầng Presentation), rồi chỉ đưa ĐƯỜNG DẪN cho Service.
            string? avatarUrl = null;
            string? oldAvatarUrl = null;
            if (vm.AvatarFile != null && vm.AvatarFile.Length > 0)
            {
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var ext = Path.GetExtension(vm.AvatarFile.FileName).ToLower();
                if (!allowed.Contains(ext))
                {
                    ModelState.AddModelError("AvatarFile", "Chỉ chấp nhận ảnh jpg, jpeg, png, gif");
                    return View(vm);
                }

                // Giới hạn dung lượng: tối đa 2MB
                const long maxBytes = 2 * 1024 * 1024;
                if (vm.AvatarFile.Length > maxBytes)
                {
                    ModelState.AddModelError("AvatarFile", "Ảnh quá lớn — tối đa 2MB");
                    return View(vm);
                }

                // Giới hạn kích thước: tối đa 1024x1024 px (try/catch tránh crash nếu file giả ảnh)
                try
                {
                    using var imageStream = vm.AvatarFile.OpenReadStream();
                    using var image = System.Drawing.Image.FromStream(imageStream);
                    const int maxWidth = 1024, maxHeight = 1024;
                    if (image.Width > maxWidth || image.Height > maxHeight)
                    {
                        ModelState.AddModelError("AvatarFile",
                            $"Kích thước ảnh tối đa {maxWidth}x{maxHeight}px (ảnh của bạn {image.Width}x{image.Height}px)");
                        return View(vm);
                    }
                }
                catch
                {
                    ModelState.AddModelError("AvatarFile", "File ảnh không hợp lệ hoặc bị hỏng");
                    return View(vm);
                }

                // Nhớ ảnh cũ để xoá sau khi cập nhật thành công
                var cur = await _profileService.GetProfileAsync(GetCurrentUserId());
                oldAvatarUrl = cur?.AvatarUrl;

                var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "avatars");
                Directory.CreateDirectory(uploadDir);
                var fileName = Guid.NewGuid().ToString() + ext;
                using (var stream = new FileStream(Path.Combine(uploadDir, fileName), FileMode.Create))
                {
                    await vm.AvatarFile.CopyToAsync(stream);
                }
                avatarUrl = "/uploads/avatars/" + fileName;
            }

            var dto = new UpdateProfileDto { FullName = vm.FullName, Phone = vm.Phone, AvatarUrl = avatarUrl };
            var ok = await _profileService.UpdateProfileAsync(GetCurrentUserId(), dto);
            if (!ok) return NotFound();

            // Xoá ảnh cũ để không tồn file thừa (chỉ xoá ảnh upload nội bộ)
            if (!string.IsNullOrEmpty(avatarUrl) && !string.IsNullOrEmpty(oldAvatarUrl)
                && oldAvatarUrl.StartsWith("/uploads/avatars/"))
            {
                var oldPath = Path.Combine(_env.WebRootPath, oldAvatarUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldPath))
                    System.IO.File.Delete(oldPath);
            }

            TempData["Success"] = "Cập nhật hồ sơ thành công!";
            return RedirectToAction("Index");
        }
    }
}
