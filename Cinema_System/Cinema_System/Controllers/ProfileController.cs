using Cinema_System.Application.ViewModels;
using Cinema_System.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cinema_System.Controllers
{
    public class ProfileController : Controller
    {
        // Dùng DbContext có sẵn của nhóm (scaffold từ database) thay vì tự viết.
        private readonly CinemaWebDbContext _context;

        public ProfileController(CinemaWebDbContext context)
        {
            _context = context;
        }

        // ==== TẠM THỜI: id user "đang đăng nhập" ====
        // Chưa có Login => hardcode 1 user CÓ THẬT trong DB (Đinh Hoài Dũng - staff4).
        // Khi có Login: đổi thành đọc từ Session/Claims.
        private Guid GetCurrentUserId()
        {
            return Guid.Parse("00000000-0000-0000-0002-00000000000e");
        }

        // ============ 1. XEM HỒ SƠ ============
        // GET: /Profile
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();

            // Lấy user kèm Role (Include = join sang bảng Roles để biết tên vai trò)
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound("Không tìm thấy người dùng trong database");

            // Map từ Entity (User) -> ViewModel để hiển thị.
            var vm = new ProfileViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                // Entity của nhóm dùng kiểu DateOnly -> đổi sang DateTime cho ViewModel
                DateOfBirth = user.DateOfBirth.ToDateTime(TimeOnly.MinValue),
                AvatarUrl = user.AvatarUrl,
                RoleName = user.Role?.Name ?? "UNKNOWN",
                RewardPoints = user.RewardPoints ?? 0,   // entity nullable -> mặc định 0
                Status = user.Status ?? "Active",
                CreatedAt = user.CreatedAt ?? DateTime.Now
            };
            return View(vm);
        }
    }
}
