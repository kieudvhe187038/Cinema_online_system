using Cinema_System.Application.Interfaces;
using Cinema_System.Domain.Entities;
using Cinema_System.Infrastructure.Data;
using Cinema_System.Infrastructure.Repositories;

namespace Cinema_System.Infrastructure.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly CinemaWebDbContext _context;

    private IGenericRepository<User>? _users;
    private IGenericRepository<Role>? _roles;
    private IGenericRepository<SystemConfig>? _systemConfigs;
    private IGenericRepository<SeatType>? _seatTypes;
    private IGenericRepository<Seat>? _seats;
    private IGenericRepository<PriceSeatConfig>? _priceSeatConfigs;
    private IGenericRepository<RoomType>? _roomTypes;
    private IGenericRepository<Room>? _rooms;
    private IGenericRepository<PriceRoomTypeConfig>? _priceRoomTypeConfigs;
    private IGenericRepository<Movie>? _movies;

    public UnitOfWork(CinemaWebDbContext context)
    {
        _context = context;
    }

    public IGenericRepository<User> Users =>
        _users ??= new GenericRepository<User>(_context);

    public IGenericRepository<Role> Roles =>
        _roles ??= new GenericRepository<Role>(_context);

    public IGenericRepository<SystemConfig> SystemConfigs =>
        _systemConfigs ??= new GenericRepository<SystemConfig>(_context);

    public IGenericRepository<SeatType> SeatTypes =>
        _seatTypes ??= new GenericRepository<SeatType>(_context);

    public IGenericRepository<Seat> Seats =>
        _seats ??= new GenericRepository<Seat>(_context);

    public IGenericRepository<PriceSeatConfig> PriceSeatConfigs =>
        _priceSeatConfigs ??= new GenericRepository<PriceSeatConfig>(_context);

    public IGenericRepository<RoomType> RoomTypes =>
        _roomTypes ??= new GenericRepository<RoomType>(_context);

    public IGenericRepository<Room> Rooms =>
        _rooms ??= new GenericRepository<Room>(_context);

    public IGenericRepository<PriceRoomTypeConfig> PriceRoomTypeConfigs =>
        _priceRoomTypeConfigs ??= new GenericRepository<PriceRoomTypeConfig>(_context);

    public IGenericRepository<Movie> Movies =>
        _movies ??= new GenericRepository<Movie>(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
