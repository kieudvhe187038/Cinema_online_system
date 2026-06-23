using Cinema_System.Application.Interfaces;
using Cinema_System.Domain.Entities;
using Cinema_System.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Cinema_System.Infrastructure.Repositories;

public class RoomRepository : GenericRepository<Room>, IRoomRepository
{
    public RoomRepository(CinemaWebDbContext context) : base(context)
    {
    }

    public async Task<Room?> GetByIdWithRoomTypeAsync(Guid roomId)
    {
        return await FirstOrDefaultAsync(r => r.Id == roomId, q => q.Include(r => r.RoomType));
    }
}
