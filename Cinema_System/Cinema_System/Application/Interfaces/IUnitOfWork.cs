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

    IGenericRepository<Seat> Seats { get; }

    IGenericRepository<PriceSeatConfig> PriceSeatConfigs { get; }

    IGenericRepository<RoomType> RoomTypes { get; }

    IGenericRepository<Room> Rooms { get; }

    IGenericRepository<PriceRoomTypeConfig> PriceRoomTypeConfigs { get; }

    IGenericRepository<PriceBaseConfig> PriceBaseConfigs { get; }

    IGenericRepository<PriceTimeConfig> PriceTimeConfigs { get; }

    IGenericRepository<Movie> Movies { get; }

    IGenericRepository<Genre> Genres { get; }

    IGenericRepository<RewardPointHistory> RewardPointHistories { get; }

    IGenericRepository<FoodBeverage> FoodBeverages { get; }

    IGenericRepository<BookingFood> BookingFoods { get; }

    IGenericRepository<Promotion> Promotions { get; }

    IGenericRepository<Booking> Bookings { get; }

    IGenericRepository<Cinema> Cinemas { get; }

    IGenericRepository<Showtime> Showtimes { get; }

    IGenericRepository<ShowtimeIncident> ShowtimeIncidents { get; }

    IGenericRepository<Ticket> Tickets { get; }

    Task<int> SaveChangesAsync();

    /// <summary>
    /// Gỡ theo dõi toàn bộ entity đang tracked. Dùng khi cần truy vấn lại rồi cập nhật
    /// (tránh xung đột "another instance with the same key is already being tracked").
    /// </summary>
    void ClearTracking();
}
