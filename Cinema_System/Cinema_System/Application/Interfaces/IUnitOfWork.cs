using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IGenericRepository<User> Users { get; }

    IGenericRepository<Role> Roles { get; }

    IGenericRepository<SystemConfig> SystemConfigs { get; }

    IGenericRepository<SeatType> SeatTypes { get; }

    IGenericRepository<Seat> Seats { get; }

    IGenericRepository<PriceSeatConfig> PriceSeatConfigs { get; }

    IGenericRepository<RoomType> RoomTypes { get; }

    IGenericRepository<Room> Rooms { get; }

    IGenericRepository<PriceRoomTypeConfig> PriceRoomTypeConfigs { get; }

    // --- Booking / counter-sale flow (Inter2 - Staff) ---
    IGenericRepository<Booking> Bookings { get; }

    IGenericRepository<Ticket> Tickets { get; }

    IGenericRepository<Payment> Payments { get; }

    IGenericRepository<BookingFood> BookingFoods { get; }

    IGenericRepository<Showtime> Showtimes { get; }

    IGenericRepository<Movie> Movies { get; }

    IGenericRepository<FoodBeverage> FoodBeverages { get; }

    IGenericRepository<RewardPointHistory> RewardPointHistories { get; }

    IGenericRepository<Promotion> Promotions { get; }

    IGenericRepository<Vat> Vats { get; }

    IGenericRepository<Cinema> Cinemas { get; }

    IGenericRepository<SeatHold> SeatHolds { get; }

    IGenericRepository<PriceBaseConfig> PriceBaseConfigs { get; }

    IGenericRepository<PriceTimeConfig> PriceTimeConfigs { get; }

    Task<int> SaveChangesAsync();
}
