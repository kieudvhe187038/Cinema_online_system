using Cinema_System.Application.Common;
using Cinema_System.Application.ViewModels;

namespace Cinema_System.Application.Interfaces;

public interface IGenreService
{
    Task<PagedResult<GenreListItemViewModel>> GetPagedAsync(string? search, int page, int pageSize);

    Task<GenreFormViewModel?> GetForEditAsync(Guid id);

    Task<Result> CreateAsync(GenreFormViewModel model);

    Task<Result> UpdateAsync(GenreFormViewModel model);

    Task<Result> DeleteAsync(Guid id);
}
