using Cinema_System.Application.Common;
using Cinema_System.Application.ViewModels;

namespace Cinema_System.Application.Interfaces;

public interface IFoodBeverageService
{
    Task<FoodBeverageListViewModel> GetAllAsync(string? search, string? status, int page, int pageSize);
    Task<FoodBeverageFormViewModel?> GetForEditAsync(Guid id);
    Task<Result> CreateAsync(FoodBeverageFormViewModel model);
    Task<Result> UpdateAsync(FoodBeverageFormViewModel model);
    Task<Result> ToggleVisibilityAsync(Guid id);
    Task<Result> DeleteAsync(Guid id);
}
