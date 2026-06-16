using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.ViewModels;

namespace Cinema_System.Application.Interfaces;

public interface ISeatTypeService
{
    Task<IEnumerable<SeatTypeDTO>> GetAllAsync();

    Task<SeatTypeFormViewModel?> GetForEditAsync(Guid id);

    Task<Result> CreateAsync(SeatTypeFormViewModel model);

    Task<Result> UpdateAsync(SeatTypeFormViewModel model);

    Task<Result> DeleteAsync(Guid id);
}
