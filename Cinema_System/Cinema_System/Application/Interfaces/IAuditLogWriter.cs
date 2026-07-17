namespace Cinema_System.Application.Interfaces;

/// <summary>
/// Ghi một dòng Audit Log (bảng Audit_Logs) cho các thao tác thay đổi dữ liệu
/// của Admin/Manager/Staff. Người thực hiện + IP tự lấy từ HttpContext hiện tại.
/// Ghi log KHÔNG được làm hỏng nghiệp vụ chính: mọi lỗi khi ghi chỉ log warning rồi bỏ qua.
/// </summary>
public interface IAuditLogWriter
{
    /// <summary>
    /// Ghi log một hành động đã thực hiện THÀNH CÔNG.
    /// </summary>
    /// <param name="action">Tên hành động dạng UPPER_SNAKE (vd: CREATE_SHOWTIME, LOCK_USER) — khớp convention dữ liệu seed.</param>
    /// <param name="tableName">Tên bảng DB bị tác động (vd: Users, Showtimes, Price_Base_Configs).</param>
    /// <param name="recordId">Id bản ghi bị tác động; null nếu không xác định được (vd: bản ghi mới do Service sinh Id).</param>
    /// <param name="oldValue">Dữ liệu trước thay đổi (object bất kỳ, sẽ serialize JSON); null nếu không có.</param>
    /// <param name="newValue">Dữ liệu sau thay đổi (object bất kỳ, sẽ serialize JSON); null nếu không có.</param>
    Task LogAsync(string action, string? tableName = null, Guid? recordId = null,
        object? oldValue = null, object? newValue = null);
}
