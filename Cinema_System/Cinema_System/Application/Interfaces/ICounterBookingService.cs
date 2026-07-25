using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.ViewModels;

namespace Cinema_System.Application.Interfaces;

/// <summary>Luồng bán vé tại quầy: chọn phim/suất/ghế/đồ ăn → tạo đơn offline + thanh toán.</summary>
public interface ICounterBookingService
{
    /// <summary>Dữ liệu khởi tạo trang quầy (phim, đồ ăn, nhân viên, VAT).</summary>
    Task<CounterBookingViewModel> GetCounterDataAsync(Guid staffId);

    /// <summary>Các suất chiếu sắp tới của một phim.</summary>
    Task<IEnumerable<ShowtimeOptionDTO>> GetShowtimesAsync(Guid movieId);

    /// <summary>Sơ đồ ghế + giá của một suất chiếu (ghế chính tab quầy này đang giữ thì KHÔNG tính là đã chiếm).</summary>
    /// <param name="holdToken">Phiên giữ ghế của tab/máy quầy đang thao tác (xem <see cref="HoldSeatAsync"/>).</param>
    Task<SeatMapDTO?> GetSeatMapAsync(Guid showtimeId, Guid staffId, Guid holdToken);

    /// <summary>Tạo đơn đặt vé tại quầy + thanh toán; trả về Id đơn vừa tạo.</summary>
    Task<Result<Guid>> CreateAsync(CounterBookingRequest request, Guid staffId);

    /// <summary>Xem trước mã khuyến mãi (AJAX): kiểm tra hợp lệ + tính số tiền giảm trên tạm tính hiện tại.</summary>
    Task<CounterPromoPreviewDTO> PreviewPromoAsync(string code, Guid? customerId, decimal seatTotal, decimal foodTotal);

    /// <summary>Giữ 1 ghế cho nhân viên trong holdMinutes phút (tạo mới hoặc gia hạn nếu đã giữ).</summary>
    /// <param name="holdToken">Phiên giữ ghế của tab/máy quầy đang thao tác. Nhiều máy quầy thường dùng
    /// CHUNG 1 tài khoản Staff nên chỉ khóa theo staffId là chưa đủ — token khác nhau thì máy này vẫn
    /// bị chặn khi bấm đúng ghế máy kia đang giữ.</param>
    Task<Result> HoldSeatAsync(Guid showtimeId, Guid seatId, Guid staffId, Guid holdToken, int holdMinutes);

    /// <summary>Bỏ giữ 1 ghế (nhân viên bỏ chọn ghế) — chỉ nhả hold của chính tab quầy này.</summary>
    Task ReleaseSeatAsync(Guid showtimeId, Guid seatId, Guid staffId, Guid holdToken);

    /// <summary>Bỏ giữ toàn bộ ghế tab quầy này đang giữ của 1 suất (đổi suất/rời trang).</summary>
    Task ReleaseAllAsync(Guid showtimeId, Guid staffId, Guid holdToken);
}
