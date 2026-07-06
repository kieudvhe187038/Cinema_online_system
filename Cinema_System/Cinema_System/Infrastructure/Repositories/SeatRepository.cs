using Cinema_System.Application.Interfaces;
using Cinema_System.Domain.Entities;
using Cinema_System.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Cinema_System.Infrastructure.Repositories;

public class SeatRepository : GenericRepository<Seat>, ISeatRepository
{
    public SeatRepository(CinemaWebDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Seat>> GetByRoomWithTypeAsync(Guid roomId)
    {
        var items = await GetAllAsync(
            s => s.RoomId == roomId,
            include: q => q.Include(s => s.SeatType),
            orderBy: q => q.OrderBy(s => s.RowNumber).ThenBy(s => s.SeatNumber));

        return items.ToList();
    }

    public async Task<IReadOnlyList<Seat>> GetByIdsWithTypeAsync(IEnumerable<Guid> seatIds)
    {
        var ids = seatIds.ToList();
        var items = await GetAllAsync(
            s => ids.Contains(s.Id),
            include: q => q.Include(s => s.SeatType));

        return items.ToList();
    }
}
