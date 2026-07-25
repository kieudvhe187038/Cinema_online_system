using AutoMapper;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;

namespace Cinema_System.Application.Services;

public class BookingManagementService : IBookingManagementService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public BookingManagementService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BookingListViewModel> GetBookingsAsync(
        string? search, string? bookingType, string? paymentStatus, int page, int pageSize)
    {
        if (page < 1) page = 1;

        // Lọc + phân trang được đẩy xuống SQL trong repository nên mỗi lần chỉ tải
        // đúng 1 trang — tránh kéo cả bảng Bookings về bộ nhớ gây timeout.
        var (rows, totalCount) = await _unitOfWork.Bookings.GetPagedListAsync(
            bookingType, paymentStatus, search, page, pageSize);

        var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);

        if (page > totalPages)
        {
            // Trang yêu cầu vượt quá số trang thực tế (vd sau khi dữ liệu giảm, hoặc
            // sửa tay query string) — truy vấn lại đúng trang cuối thay vì trả rows rỗng
            // trong khi CurrentPage hiển thị lệch với dữ liệu.
            page = totalPages;
            (rows, totalCount) = await _unitOfWork.Bookings.GetPagedListAsync(
                bookingType, paymentStatus, search, page, pageSize);
        }

        var items = _mapper.Map<List<BookingListItemDTO>>(rows);

        return new BookingListViewModel
        {
            Bookings = items,
            Search = search,
            BookingType = bookingType,
            PaymentStatus = paymentStatus,
            CurrentPage = page,
            TotalPages = totalPages,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<BookingManagementDetailDTO?> GetBookingDetailAsync(Guid id)
    {
        var b = await _unitOfWork.Bookings.GetDetailByIdAsync(id);
        return b is null ? null : _mapper.Map<BookingManagementDetailDTO>(b);
    }

    public async Task<TicketPrintViewModel?> GetTicketPrintAsync(Guid bookingId)
    {
        var b = await _unitOfWork.Bookings.GetDetailByIdAsync(bookingId);
        return b is null ? null : _mapper.Map<TicketPrintViewModel>(b);
    }
}
