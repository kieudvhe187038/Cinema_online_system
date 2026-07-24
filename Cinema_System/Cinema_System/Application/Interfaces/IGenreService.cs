using Cinema_System.Application.Common;
using Cinema_System.Application.ViewModels;

namespace Cinema_System.Application.Interfaces;

public interface IGenreService
{
    Task<IEnumerable<GenreListItemViewModel>> GetAllAsync(string? search = null);

    Task<GenreFormViewModel?> GetForEditAsync(Guid id);

    Task<Result> CreateAsync(GenreFormViewModel model);

    Task<Result> UpdateAsync(GenreFormViewModel model);

    Task<Result> DeleteAsync(Guid id);
}
