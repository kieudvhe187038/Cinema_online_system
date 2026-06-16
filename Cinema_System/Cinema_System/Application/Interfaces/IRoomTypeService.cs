using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.ViewModels;

namespace Cinema_System.Application.Interfaces;

public interface IRoomTypeService
{
    Task<IEnumerable<RoomTypeDTO>> GetAllAsync();

    Task<RoomTypeFormViewModel?> GetForEditAsync(Guid id);

    Task<Result> CreateAsync(RoomTypeFormViewModel model);

    Task<Result> UpdateAsync(RoomTypeFormViewModel model);

    Task<Result> DeleteAsync(Guid id);
}
