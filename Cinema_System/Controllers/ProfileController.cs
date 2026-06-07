using Cinema_System.Application.FakeData;
using Cinema_System.Application.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers
{
    public class ProfileController : Controller
    {
        // IWebHostEnvironment giúp lấy đường dẫn thư mục wwwroot để lưu file ảnh upload.
        private readonly IWebHostEnvironment _env;

        public ProfileController(IWebHostEnvironment env)
        {
            _env = env;
        }

        // ============ 1. XEM HỒ SƠ ============
        //Url: /Profile hoặc /Profile/Index (GET)
        //Hiển thị thông tin cá nhân của User hiện tại.
        public IActionResult Index()
        {
            //Lấy user từ "DB giả". Sau này: _context.Users.Find(currentUserId)
            var user = FakeUserStore.CurrentUser;
            return View(user); //Đổ user vào View Index.cshtml
        }

        // ============ 2. CẬP NHẬT HỒ SƠ - hiện form ============
        // URL: /Profile/Edit  (GET) - chỉ hiển thị form đã điền sẵn dữ liệu cũ.
        public IActionResult Edit()
        {
            var user = FakeUserStore.CurrentUser;

            // Đổ dữ liệu hiện tại vào form ViewModel để người dùng thấy và sửa.
            var vm = new UpdateProfileViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Phone = user.Phone,
                Email = user.Email,
                CurrentAvatarUrl = user.AvatarUrl,
            };
            return View(vm);
        }

        // ============ 2+3. LƯU CẬP NHẬT + UPLOAD AVATAR ============
        // URL: /Profile/Edit  (POST) - chạy khi người dùng bấm "Lưu thay đổi".
        [HttpPost]
        [ValidateAntiForgeryToken] //Chống tấn công giả mạo form (CSRF)
        public async Task<IActionResult> Edit(UpdateProfileViewModel vm)
        {
            if(!ModelState.IsValid)
            {
                vm.CurrentAvatarUrl = FakeUserStore.CurrentUser.AvatarUrl;
                return View(vm);
            }

            var user = FakeUserStore.CurrentUser;

            // --- Xử lý upload ảnh đại diện (nếu người dùng có chọn file) ---
            if(vm.AvatarFile != null && vm.AvatarFile.Length > 0)
            {
                //Chỉ cho phép ảnh
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var ext = Path.GetExtension(vm.AvatarFile.FileName).ToLower();
                if (!allowed.Contains(ext))
                {
                    ModelState.AddModelError("AvatarFile", "Chỉ chấp nhận ảnh jpg, jpeg, png, gif");
                    vm.CurrentAvatarUrl = user.AvatarUrl;
                    return View(vm);
                }

                // Tạo thư mục wwwroot/uploads/avatars nếu chưa có
                var uploadDir = Path.Combine(_env.WebRootPath, "upload", "avatar");
                Directory.CreateDirectory(uploadDir);

                // Đặt tên file mới để tránh trùng (dùng GUID)
                var fileName = Guid.NewGuid().ToString() + ext;
                var filePath = Path.Combine(uploadDir, fileName);

                // Ghi file vào ổ đĩa server
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await vm.AvatarFile.CopyToAsync(stream);
                }

                // Lưu đường dẫn (tương đối) để View hiển thị. Sau này lưu vào Users.avatar_url
                user.AvatarUrl = "/uploads/avatars/" + fileName;
            }

            // --- Cập nhật các trường text --
            user.FullName = vm.FullName;
            user.Phone = vm.Phone;
            // (SQL thật: _context.SaveChanges())

            // TempData: nhắn 1 lần, hiện sau khi chuyển trang (PRG pattern)
            TempData["Success"] = "Cập nhật hồ sơ thành công!";
            return RedirectToAction("Index"); // Quay lại màn xem hồ sơ
        }

        // ============ 4. ĐỔI MẬT KHẨU - hiện form ============
        // URL: /Profile/ChangePassword  (GET)
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        // ============ 4. ĐỔI MẬT KHẨU - xử lý ============
        // URL: /Profile/ChangePassword  (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(ChangePasswordViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm); // Sai định dạng => hiện lại form kèm lỗi

            // Kiểm tra mật khẩu cũ có đúng không.
            // DB thật: so sánh hash, ví dụ BCrypt.Verify(vm.OldPassword, user.password_hash)
            if (vm.OldPassword != FakeUserStore.CurrentPassword)
            {
                ModelState.AddModelError("OldPassword", "Mật khẩu hiện tại không đúng");
                return View(vm);
            }

            // Lưu mật khẩu mới (DB thật: lưu hash của vm.NewPassword)
            FakeUserStore.CurrentPassword = vm.NewPassword;

            TempData["Success"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("Index");
        }

        // Lấy user đang đăng nhập. Trả về null nếu CHƯA đăng nhập (Guest).
        // Tạm thời đọc từ Session. Sau khi có Login thật => thay bằng userId thật trong Session/Claims.
        private ProfileViewModel? GetLoggedInUser()
        {
            // Đọc "UserId" mà trang Login sẽ lưu vào Session sau khi đăng nhập thành công.
            var userId = HttpContext.Session.GetInt32("UserId");

            if(userId == null)
                return null; // Chưa đăng nhập => không có profile

            // Có đăng nhập => lấy thông tin user (giờ là DB giả, sau này là _context.Users.Find(userId))
            return FakeUserStore.CurrentUser;
        }
    }
}
