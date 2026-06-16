using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.ViewModels;

namespace Cinema_System.Application.Interfaces;

/// <summary>Luồng bán vé tại quầy: chọn phim/suất/ghế/đồ ăn → tạo đơn offline + thanh toán.</summary>
public interface ICounterBookingService
{
    /// <summary>Dữ liệu khởi tạo trang quầy (phim, đồ ăn, nhân viên, VAT).</summary>
    Task<CounterBookingViewModel> GetCounterDataAsync();

    /// <summary>Các suất chiếu sắp tới của một phim.</summary>
    Task<IEnumerable<ShowtimeOptionDTO>> GetShowtimesAsync(Guid movieId);

    /// <summary>Sơ đồ ghế + giá của một suất chiếu.</summary>
    Task<SeatMapDTO?> GetSeatMapAsync(Guid showtimeId);

    /// <summary>Tạo đơn đặt vé tại quầy + thanh toán; trả về Id đơn vừa tạo.</summary>
    Task<Result<Guid>> CreateAsync(CounterBookingRequest request);
}
