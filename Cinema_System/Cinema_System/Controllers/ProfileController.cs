using AutoMapper;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers
{
    // Controller GỌN: chỉ nhận request, gọi Service, trả View. KHÔNG đụng DbContext.
    public class ProfileController : Controller
    {
        private readonly IProfileService _profileService;
        private readonly IMapper _mapper;

        public ProfileController(IProfileService profileService, IMapper mapper)
        {
            _profileService = profileService;
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
    }
}
