using Cinema_System.Application.Interfaces;
using Cinema_System.Domain.Entities;
using Cinema_System.Infrastructure.Data;
using Cinema_System.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Cinema_System.Infrastructure.UnitOfWork;

/// <summary>
/// Điều phối các Repository trên cùng một DbContext và commit transaction.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly CinemaWebDbContext _context;

    // Unit of Work gom các repository lại để quản lý cùng 1 DbContext
    private IUserRepository? _users;
    private IGenericRepository<Role>? _roles;
    private IGenericRepository<SystemConfig>? _systemConfigs;
    private IGenericRepository<SeatType>? _seatTypes;
    private ISeatRepository? _seats;
    private IGenericRepository<PriceSeatConfig>? _priceSeatConfigs;
    private IGenericRepository<PriceRoomTypeConfig>? _priceRoomTypeConfigs;
    private IGenericRepository<PriceBaseConfig>? _priceBaseConfigs;
    private IGenericRepository<PriceTimeConfig>? _priceTimeConfigs;
    private IGenericRepository<RoomType>? _roomTypes;
    private IRoomRepository? _rooms;
    private IGenericRepository<Movie>? _movies;
    private IGenericRepository<Genre>? _genres;
    private IGenericRepository<RewardPointHistory>? _rewardPointHistories;
    private IGenericRepository<FoodBeverage>? _foodBeverages;
    private IGenericRepository<BookingFood>? _bookingFoods;
    private IGenericRepository<Promotion>? _promotions;
    private IBookingRepository? _bookings;
    private IGenericRepository<Cinema>? _cinemas;
    private IShowtimeRepository? _showtimes;
    private IGenericRepository<ShowtimeIncident>? _showtimeIncidents;
    private IGenericRepository<Review>? _reviews;
    private ITicketRepository? _tickets;
    private IGenericRepository<SeatHold>? _seatHolds;
    private IGenericRepository<Payment>? _payments;
    private IGenericRepository<Vat>? _vats;
    private IGenericRepository<AuditLog>? _auditLogs;

    public UnitOfWork(CinemaWebDbContext context)
    {
        _context = context;
    }

    public IUserRepository Users =>
        _users ??= new UserRepository(_context);

    public IGenericRepository<Role> Roles =>
        _roles ??= new GenericRepository<Role>(_context);

    public IGenericRepository<SystemConfig> SystemConfigs =>
        _systemConfigs ??= new GenericRepository<SystemConfig>(_context);

    public IGenericRepository<SeatType> SeatTypes =>
        _seatTypes ??= new GenericRepository<SeatType>(_context);

    public ISeatRepository Seats =>
        _seats ??= new SeatRepository(_context);

    public IGenericRepository<PriceSeatConfig> PriceSeatConfigs =>
        _priceSeatConfigs ??= new GenericRepository<PriceSeatConfig>(_context);

    public IGenericRepository<PriceRoomTypeConfig> PriceRoomTypeConfigs =>
        _priceRoomTypeConfigs ??= new GenericRepository<PriceRoomTypeConfig>(_context);

    public IGenericRepository<PriceBaseConfig> PriceBaseConfigs =>
        _priceBaseConfigs ??= new GenericRepository<PriceBaseConfig>(_context);

    public IGenericRepository<PriceTimeConfig> PriceTimeConfigs =>
        _priceTimeConfigs ??= new GenericRepository<PriceTimeConfig>(_context);

    public IGenericRepository<RoomType> RoomTypes =>
        _roomTypes ??= new GenericRepository<RoomType>(_context);

    public IRoomRepository Rooms =>
        _rooms ??= new RoomRepository(_context);

    public IGenericRepository<Movie> Movies =>
        _movies ??= new GenericRepository<Movie>(_context);

    public IGenericRepository<Genre> Genres =>
        _genres ??= new GenericRepository<Genre>(_context);

    public IGenericRepository<RewardPointHistory> RewardPointHistories =>
        _rewardPointHistories ??= new GenericRepository<RewardPointHistory>(_context);

    public IGenericRepository<FoodBeverage> FoodBeverages =>
        _foodBeverages ??= new GenericRepository<FoodBeverage>(_context);

    public IGenericRepository<BookingFood> BookingFoods =>
        _bookingFoods ??= new GenericRepository<BookingFood>(_context);

    public IGenericRepository<Promotion> Promotions =>
        _promotions ??= new GenericRepository<Promotion>(_context);

    public IBookingRepository Bookings =>
        _bookings ??= new BookingRepository(_context);

    public IGenericRepository<Cinema> Cinemas =>
        _cinemas ??= new GenericRepository<Cinema>(_context);

    public IShowtimeRepository Showtimes =>
        _showtimes ??= new ShowtimeRepository(_context);

    public IGenericRepository<ShowtimeIncident> ShowtimeIncidents =>
        _showtimeIncidents ??= new GenericRepository<ShowtimeIncident>(_context);

    public IGenericRepository<Review> Reviews =>
        _reviews ??= new GenericRepository<Review>(_context);

    public ITicketRepository Tickets =>
        _tickets ??= new TicketRepository(_context);

    public IGenericRepository<SeatHold> SeatHolds =>
        _seatHolds ??= new GenericRepository<SeatHold>(_context);

    public IGenericRepository<Payment> Payments =>
        _payments ??= new GenericRepository<Payment>(_context);

    public IGenericRepository<Vat> Vats =>
        _vats ??= new GenericRepository<Vat>(_context);

    public IGenericRepository<AuditLog> AuditLogs =>
        _auditLogs ??= new GenericRepository<AuditLog>(_context);

    // Lưu tất cả thay đổi của DbContext trong 1 transaction logic
    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Lưu thay đổi nhưng trả về <c>false</c> khi vi phạm ràng buộc dữ liệu
    /// (DbUpdateException) — backstop chống đặt trùng ghế khi có người đặt cùng lúc.
    /// Các loại lỗi khác vẫn được ném ra như thường.
    /// </summary>
    public async Task<bool> TrySaveChangesAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            // Backstop unique index (vd UX_Tickets_Showtime_Seat) khi đặt trùng ghế.
            return false;
        }
    }

    // Gỡ theo dõi toàn bộ entity đang tracked (xem IUnitOfWork.ClearTracking).
    public void ClearTracking()
    {
        _context.ChangeTracker.Clear();
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
