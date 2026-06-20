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

    IGenericRepository<PriceBaseConfig> PriceBaseConfigs { get; }

    IGenericRepository<PriceTimeConfig> PriceTimeConfigs { get; }

    IGenericRepository<RoomType> RoomTypes { get; }

    IGenericRepository<Room> Rooms { get; }

    IGenericRepository<Showtime> Showtimes { get; }

    IGenericRepository<Cinema> Cinemas { get; }

    IGenericRepository<Ticket> Tickets { get; }

    IGenericRepository<SeatHold> SeatHolds { get; }

    IGenericRepository<Booking> Bookings { get; }

    IGenericRepository<Payment> Payments { get; }

    IGenericRepository<Vat> Vats { get; }

    IGenericRepository<PriceRoomTypeConfig> PriceRoomTypeConfigs { get; }

    IGenericRepository<Movie> Movies { get; }

    IGenericRepository<Genre> Genres { get; }

    IGenericRepository<RewardPointHistory> RewardPointHistories { get; }

    IGenericRepository<FoodBeverage> FoodBeverages { get; }

    IGenericRepository<BookingFood> BookingFoods { get; }

    IGenericRepository<Promotion> Promotions { get; }

    Task<int> SaveChangesAsync();
}
