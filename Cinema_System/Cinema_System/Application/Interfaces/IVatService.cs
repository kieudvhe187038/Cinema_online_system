using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.ViewModels;

namespace Cinema_System.Application.Interfaces;

public interface IVatService
{
    // Lấy danh sách cấu hình VAT cho trang quản lý.
    Task<IEnumerable<VatDTO>> GetAllAsync();
    // Lấy dữ liệu 1 cấu hình VAT để đổ vào form sửa.
    Task<VatFormViewModel?> GetForEditAsync(Guid id);
    // Tạo cấu hình VAT mới.
    Task<Result> CreateAsync(VatFormViewModel model);
    // Cập nhật cấu hình VAT.
    Task<Result> UpdateAsync(VatFormViewModel model);
    // Bật/tắt cấu hình VAT (Active ↔ Inactive).
    Task<Result> ToggleStatusAsync(Guid id);
    // Xóa cấu hình VAT khỏi hệ thống.
    Task<Result> DeleteAsync(Guid id);
}
