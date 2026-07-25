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

    /// <summary>Sơ đồ ghế + giá của một suất chiếu (ghế nhân viên đang tự giữ thì KHÔNG tính là đã chiếm).</summary>
    Task<SeatMapDTO?> GetSeatMapAsync(Guid showtimeId, Guid staffId);

    /// <summary>Tạo đơn đặt vé tại quầy + thanh toán; trả về Id đơn vừa tạo.</summary>
    Task<Result<Guid>> CreateAsync(CounterBookingRequest request, Guid staffId);

    /// <summary>Giữ 1 ghế cho nhân viên trong holdMinutes phút (tạo mới hoặc gia hạn nếu đã giữ).</summary>
    Task<Result> HoldSeatAsync(Guid showtimeId, Guid seatId, Guid staffId, int holdMinutes);

    /// <summary>Bỏ giữ 1 ghế (nhân viên bỏ chọn ghế).</summary>
    Task ReleaseSeatAsync(Guid showtimeId, Guid seatId, Guid staffId);

    /// <summary>Bỏ giữ toàn bộ ghế nhân viên đang giữ của 1 suất (đổi suất/rời trang).</summary>
    Task ReleaseAllAsync(Guid showtimeId, Guid staffId);
}
