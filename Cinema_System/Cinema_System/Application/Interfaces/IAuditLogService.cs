using Cinema_System.Application.ViewModels;

namespace Cinema_System.Application.Interfaces;

/// <summary>
/// Xem nhật ký chỉnh sửa dữ liệu (Audit Log — #49 danh sách, #50 chi tiết).
/// Chỉ đọc: liệt kê + xem chi tiết bản ghi trong bảng Audit_Logs.
/// </summary>
public interface IAuditLogService
{
    /// <summary>Danh sách log (mới nhất trước) có tìm kiếm theo hành động/bảng/người thực hiện + phân trang.</summary>
    Task<AuditLogListViewModel> GetLogsAsync(string? search, int page, int pageSize);

    /// <summary>Chi tiết một bản ghi log theo Id; null nếu không tồn tại.</summary>
    Task<AuditLogDetailViewModel?> GetLogDetailAsync(Guid id);
}
