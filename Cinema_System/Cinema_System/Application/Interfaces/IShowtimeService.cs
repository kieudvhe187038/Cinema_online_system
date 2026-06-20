using Cinema_System.Application.Common;
using Cinema_System.Application.ViewModels;

namespace Cinema_System.Application.Interfaces;

// Dịch vụ lịch chiếu công khai (xem suất chiếu theo phim/phòng/ngày).
public interface IShowtimeService
{
    // Lấy dữ liệu trang lịch chiếu: lọc theo phim, phòng và ngày (mặc định hôm nay).
    Task<ShowtimePageViewModel> GetShowtimePageAsync(Guid? movieId, Guid? roomId, DateOnly? date);

    // Lấy sơ đồ ghế của 1 suất chiếu kèm trạng thái (trống/đã đặt/đang giữ/hỏng); null nếu không tìm thấy suất.
    // currentUserId: ghế do chính user này đang giữ sẽ không bị tính là "đang giữ" (để họ chọn lại được).
    Task<SeatSelectionViewModel?> GetSeatSelectionAsync(Guid showtimeId, Guid? currentUserId = null);

    // Lấy dữ liệu trang chọn đồ ăn/nước: ghế user đang giữ + danh sách món + thời gian giữ còn lại.
    // Trả null nếu user không còn giữ ghế nào (hết giờ giữ) -> caller điều hướng lại.
    Task<FoodOrderViewModel?> GetFoodOrderAsync(Guid showtimeId, Guid userId);

    // Lấy dữ liệu trang thanh toán: ghế đang giữ + đồ ăn đã chọn + tổng tiền. Null nếu hết giờ giữ.
    Task<PaymentViewModel?> GetPaymentAsync(Guid showtimeId, Guid userId, List<Guid> foodIds, List<int> foodQtys);

    // Xác nhận đặt vé (thanh toán giả): tạo Booking/Tickets/Foods/Payment, chuyển hold sang Converted,
    // áp mã giảm giá (promoCode) + dùng pointsUsed điểm để giảm giá, rồi cộng điểm mới theo tỷ lệ config.
    Task<BookingConfirmResult> ConfirmBookingAsync(Guid showtimeId, Guid userId, string method, List<Guid> foodIds, List<int> foodQtys, int pointsUsed, string? promoCode = null);

    // Kiểm tra + tính trước số tiền giảm của một mã khuyến mãi cho đơn hiện tại (xem trước ở trang thanh toán).
    Task<PromoPreviewResult> PreviewPromoAsync(Guid showtimeId, Guid userId, List<Guid> foodIds, List<int> foodQtys, string code);

    // Lấy dữ liệu trang đặt vé thành công của 1 booking (chỉ cho đúng chủ booking).
    Task<PaymentSuccessViewModel?> GetBookingSuccessAsync(Guid bookingId, Guid userId);

    // Giữ 1 ghế cho user trong holdMinutes phút (tạo mới hoặc gia hạn); fail nếu ghế đã đặt/người khác giữ.
    Task<Result> HoldSeatAsync(Guid showtimeId, Guid seatId, Guid userId, int holdMinutes);

    // Bỏ giữ 1 ghế của user (đánh dấu Released).
    Task ReleaseSeatAsync(Guid showtimeId, Guid seatId, Guid userId);

    // Bỏ giữ toàn bộ ghế user đang giữ trong suất (gọi khi rời trang).
    Task ReleaseAllAsync(Guid showtimeId, Guid userId);

    // Gia hạn thời gian giữ cho toàn bộ ghế user đang giữ (heartbeat khi còn ở trang).
    Task ExtendHoldsAsync(Guid showtimeId, Guid userId, int holdMinutes);
}
