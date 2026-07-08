using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Interfaces;

/// <summary>
/// Quản lý transaction và điều phối nhiều Repository trong cùng một phiên DbContext.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }

    IGenericRepository<Role> Roles { get; }

    IGenericRepository<SystemConfig> SystemConfigs { get; }

    IGenericRepository<SeatType> SeatTypes { get; }

    ISeatRepository Seats { get; }

    IGenericRepository<PriceSeatConfig> PriceSeatConfigs { get; }

    IGenericRepository<PriceRoomTypeConfig> PriceRoomTypeConfigs { get; }

    IGenericRepository<PriceBaseConfig> PriceBaseConfigs { get; }

    IGenericRepository<PriceTimeConfig> PriceTimeConfigs { get; }

    IGenericRepository<RoomType> RoomTypes { get; }

    IRoomRepository Rooms { get; }

    IGenericRepository<Movie> Movies { get; }

    IGenericRepository<Genre> Genres { get; } 

    IGenericRepository<RewardPointHistory> RewardPointHistories { get; }

    IGenericRepository<FoodBeverage> FoodBeverages { get; }

    IGenericRepository<BookingFood> BookingFoods { get; }

    IGenericRepository<Promotion> Promotions { get; }

    IBookingRepository Bookings { get; }

    IGenericRepository<Cinema> Cinemas { get; }

    IShowtimeRepository Showtimes { get; }

    IGenericRepository<ShowtimeIncident> ShowtimeIncidents { get; }

    IGenericRepository<Review> Reviews { get; }

    IGenericRepository<ChatbotLog> ChatbotLogs { get; }

    IGenericRepository<Ticket> Tickets { get; }

    IGenericRepository<SeatHold> SeatHolds { get; }

    IGenericRepository<Payment> Payments { get; }

    IGenericRepository<Vat> Vats { get; }

    Task<int> SaveChangesAsync();

    /// <summary>
    /// Lưu thay đổi nhưng trả về <c>false</c> khi vi phạm ràng buộc dữ liệu
    /// (DbUpdateException) — backstop chống đặt trùng ghế khi có người đặt cùng lúc.
    /// Các loại lỗi khác vẫn được ném ra như thường.
    /// </summary>
    Task<bool> TrySaveChangesAsync();

    /// <summary>
    /// Gỡ theo dõi toàn bộ entity đang tracked. Dùng khi cần truy vấn lại rồi cập nhật
    /// (tránh xung đột "another instance with the same key is already being tracked").
    /// </summary>
    void ClearTracking();
}
