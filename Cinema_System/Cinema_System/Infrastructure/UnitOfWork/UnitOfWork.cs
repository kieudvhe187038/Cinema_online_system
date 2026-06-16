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
    private IGenericRepository<Booking>? _bookings;
    private IGenericRepository<Ticket>? _tickets;
    private IGenericRepository<Payment>? _payments;
    private IGenericRepository<BookingFood>? _bookingFoods;
    private IGenericRepository<Showtime>? _showtimes;
    private IGenericRepository<Movie>? _movies;
    private IGenericRepository<FoodBeverage>? _foodBeverages;
    private IGenericRepository<RewardPointHistory>? _rewardPointHistories;
    private IGenericRepository<Promotion>? _promotions;
    private IGenericRepository<Vat>? _vats;
    private IGenericRepository<Cinema>? _cinemas;
    private IGenericRepository<SeatHold>? _seatHolds;
    private IGenericRepository<PriceBaseConfig>? _priceBaseConfigs;
    private IGenericRepository<PriceTimeConfig>? _priceTimeConfigs;

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

    public IGenericRepository<Booking> Bookings =>
        _bookings ??= new GenericRepository<Booking>(_context);

    public IGenericRepository<Ticket> Tickets =>
        _tickets ??= new GenericRepository<Ticket>(_context);

    public IGenericRepository<Payment> Payments =>
        _payments ??= new GenericRepository<Payment>(_context);

    public IGenericRepository<BookingFood> BookingFoods =>
        _bookingFoods ??= new GenericRepository<BookingFood>(_context);

    public IGenericRepository<Showtime> Showtimes =>
        _showtimes ??= new GenericRepository<Showtime>(_context);

    public IGenericRepository<Movie> Movies =>
        _movies ??= new GenericRepository<Movie>(_context);

    public IGenericRepository<FoodBeverage> FoodBeverages =>
        _foodBeverages ??= new GenericRepository<FoodBeverage>(_context);

    public IGenericRepository<RewardPointHistory> RewardPointHistories =>
        _rewardPointHistories ??= new GenericRepository<RewardPointHistory>(_context);

    public IGenericRepository<Promotion> Promotions =>
        _promotions ??= new GenericRepository<Promotion>(_context);

    public IGenericRepository<Vat> Vats =>
        _vats ??= new GenericRepository<Vat>(_context);

    public IGenericRepository<Cinema> Cinemas =>
        _cinemas ??= new GenericRepository<Cinema>(_context);

    public IGenericRepository<SeatHold> SeatHolds =>
        _seatHolds ??= new GenericRepository<SeatHold>(_context);

    public IGenericRepository<PriceBaseConfig> PriceBaseConfigs =>
        _priceBaseConfigs ??= new GenericRepository<PriceBaseConfig>(_context);

    public IGenericRepository<PriceTimeConfig> PriceTimeConfigs =>
        _priceTimeConfigs ??= new GenericRepository<PriceTimeConfig>(_context);

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
