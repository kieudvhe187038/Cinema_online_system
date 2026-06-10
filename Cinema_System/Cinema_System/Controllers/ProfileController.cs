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
            var dto = await _profileService.GetProfileAsync(GetCurrentUserId());
            if (dto == null) return NotFound("Không tìm thấy người dùng");

            var vm = _mapper.Map<ProfileViewModel>(dto);
            return View(vm);
        }

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

            string? avatarUrl = null;
            if (vm.AvatarFile != null && vm.AvatarFile.Length > 0)
            {
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var ext = Path.GetExtension(vm.AvatarFile.FileName).ToLower();
                if (!allowed.Contains(ext))
                {
                    ModelState.AddModelError("AvatarFile", "Chỉ chấp nhận ảnh jpg, jpeg, png, gif");
                    return View(vm);
                }

                // Giới hạn DUNG LƯỢNG ảnh: tối đa 2MB
                const long maxBytes = 2 * 1024 * 1024; // 2MB = 2 * 1024 * 1024 byte
                if(vm.AvatarFile.Length > maxBytes)
                {
                    ModelState.AddModelError("AvatarFile", "Ảnh quá lớn — tối đa 2MB");
                    return View(vm);
                }

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

            TempData["Success"] = "Cập nhật hồ sơ thành công!";
            return RedirectToAction("Index");
        }

        public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var (ok, error) = await _profileService.ChangePasswordAsync(
                GetCurrentUserId(), vm.OldPassword, vm.NewPassword);

            if (!ok)
            {
                ModelState.AddModelError("OldPassword", error ?? "Đổi mật khẩu thất bại");
                return View(vm);
            }

            TempData["Success"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("Index");
        }

        // ===== 5. XEM LỊCH SỬ ĐIỂM =====
        public async Task<IActionResult> PointHistory()
        {
            var dtos = await _profileService.GetPointHistoryAsync(GetCurrentUserId());
            var vm = dtos.Select(d => _mapper.Map<PointHistoryViewModel>(d)).ToList();
            return View(vm); // truyền List<PointHistoryViewModel> ra View
        }
    }
}
