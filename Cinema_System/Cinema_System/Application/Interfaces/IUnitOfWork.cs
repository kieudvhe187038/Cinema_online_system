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

    IGenericRepository<Movie> Movies { get; }

    IGenericRepository<Genre> Genres { get; }

    IGenericRepository<FoodBeverage> FoodBeverages { get; }

    IGenericRepository<BookingFood> BookingFoods { get; }

    Task<int> SaveChangesAsync();
}
