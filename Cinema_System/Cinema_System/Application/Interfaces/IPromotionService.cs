using Cinema_System.Application.Common;
using Cinema_System.Application.ViewModels;

namespace Cinema_System.Application.Interfaces;

public interface IPromotionService
{
    // Lấy danh sách voucher cho trang quản lý (lọc mã/trạng thái + phân trang).
    Task<PromotionListViewModel> GetAllAsync(string? search, string? status, int page, int pageSize);
    // Lấy dữ liệu voucher để đổ vào form sửa.
    Task<PromotionFormViewModel?> GetForEditAsync(Guid id);
    // Tạo voucher mới.
    Task<Result> CreateAsync(PromotionFormViewModel model);
    // Cập nhật voucher.
    Task<Result> UpdateAsync(PromotionFormViewModel model);
    // Bật/tắt voucher (Active ↔ Disabled).
    Task<Result> ToggleStatusAsync(Guid id);
    // Xóa voucher khỏi hệ thống.
    Task<Result> DeleteAsync(Guid id);
}
