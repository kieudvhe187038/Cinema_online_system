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

    IGenericRepository<Movie> Movies { get; }

    IGenericRepository<Genre> Genres { get; }

    IGenericRepository<RewardPointHistory> RewardPointHistories { get; }

    Task<int> SaveChangesAsync();
}
