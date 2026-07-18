using AutoMapper;
using System.Security.Claims;
using Cinema_System.Application.Common;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers.Customer;

// Tầng Presentation module Đặt vé (khách hàng). Bắt buộc đăng nhập.
[Authorize]
public class BookingController : Controller
{
    private const int PageSize = 5;

    private readonly IBookingService _bookingService;
    private readonly IMapper _mapper;

    public BookingController(IBookingService bookingService, IMapper mapper)
    {
        _bookingService = bookingService;
        _mapper = mapper;
    }

    private Guid GetCurrentUserId()
        => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Lịch sử đặt vé (phân trang 5 dòng/trang)
    public async Task<IActionResult> History(int page = 1)
    {
        var history = await _bookingService.GetBookingHistoryAsync(GetCurrentUserId());
        var rows = history.Select(d => _mapper.Map<BookingHistoryViewModel>(d));

        var paged = PagedResult<BookingHistoryViewModel>.Create(rows, page, PageSize);

        var vm = new BookingHistoryPageViewModel
        {
            Items = paged.Items.ToList(),
            CurrentPage = paged.CurrentPage,
            TotalPages = paged.TotalPages,
            TotalItems = paged.TotalCount
        };
        return View(vm);
    }

    // Chi tiết 1 đơn đặt vé. Lọc theo user ở service -> đổi Id sang đơn người khác sẽ ra null.
    // Khi không có: quay về Lịch sử đặt vé kèm thông báo (giữ trong layout web, không rơi ra trang lỗi mặc định).
    public async Task<IActionResult> Detail(Guid id)
    {
        var detail = await _bookingService.GetBookingDetailAsync(id, GetCurrentUserId());
        if (detail == null)
        {
            TempData["Error"] = "Không tìm thấy đơn đặt vé, hoặc đơn này không thuộc về bạn.";
            return RedirectToAction(nameof(History));
        }

        var vm = _mapper.Map<BookingDetailViewModel>(detail);
        return View(vm);
    }

    // Vé điện tử (QR) cho 1 đơn — dùng lại dữ liệu chi tiết
    public async Task<IActionResult> ETicket(Guid id)
    {
        var detail = await _bookingService.GetBookingDetailAsync(id, GetCurrentUserId());
        if (detail == null)
        {
            TempData["Error"] = "Không tìm thấy đơn đặt vé, hoặc đơn này không thuộc về bạn.";
            return RedirectToAction(nameof(History));
        }

        var vm = _mapper.Map<BookingDetailViewModel>(detail);
        return View(vm);
    }

}
