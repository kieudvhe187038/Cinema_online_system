using Cinema_System.Application.Common;
using Cinema_System.Application.ViewModels;

namespace Cinema_System.Application.Interfaces;

public interface IFoodBeverageService
{
    // Lấy danh sách món cho trang quản lý (lọc + phân trang).
    Task<FoodBeverageListViewModel> GetAllAsync(string? search, string? status, int page, int pageSize);
    // Lấy dữ liệu món để đổ vào form sửa.
    Task<FoodBeverageFormViewModel?> GetForEditAsync(Guid id);
    // Tạo món mới.
    Task<Result> CreateAsync(FoodBeverageFormViewModel model);
    // Cập nhật món.
    Task<Result> UpdateAsync(FoodBeverageFormViewModel model);
    // Ẩn/hiện món khỏi menu.
    Task<Result> ToggleVisibilityAsync(Guid id);
    // Xóa món khỏi menu.
    Task<Result> DeleteAsync(Guid id);
}
