using Cinema_System.Application.Common;
using Cinema_System.Application.ViewModels;

namespace Cinema_System.Application.Interfaces;

public interface IPriceService
{
    // Lấy cấu hình giá cho màn quản lý: mở sẵn tab chỉ định + phân trang tab đó (badge các tab là tổng số).
    Task<PriceManagementViewModel> GetManagementAsync(string? tab, int page, int pageSize);

    // Tạo form Thêm mới rỗng cho 1 loại giá (kèm dropdown phim/phòng/ghế).
    Task<PriceConfigFormViewModel> BuildCreateFormAsync(string kind);

    // Lấy dữ liệu 1 cấu hình giá để đổ vào form Sửa (kèm dropdown). Null nếu không tồn tại.
    Task<PriceConfigFormViewModel?> GetForEditAsync(string kind, Guid id);

    // Nạp lại dropdown khi ModelState lỗi và cần render lại form.
    Task PopulateOptionsAsync(PriceConfigFormViewModel model);

    Task<Result> CreateAsync(PriceConfigFormViewModel model);
    Task<Result> UpdateAsync(PriceConfigFormViewModel model);

    // Xóa cấu hình giá; dòng thời gian của đối tượng được tự nối lại.
    Task<Result> DeleteAsync(string kind, Guid id);
}
