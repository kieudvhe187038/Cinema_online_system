using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Common;

/// <summary>
/// Các dòng Audit_Logs mà interceptor đã tự sinh trong request hiện tại (đăng ký Scoped).
/// Dùng để <see cref="Interfaces.IAuditLogWriter"/> đặt lại tên hành động nghiệp vụ cho đúng dòng
/// (vd "UPDATE_MOVIES" tự sinh -> "TOGGLE_MOVIE_STATUS") thay vì ghi thêm một dòng trùng lặp.
/// </summary>
public class AuditRequestLog
{
    private readonly List<AuditLog> _pending = new();

    /// <summary>Interceptor gọi khi vừa tạo một dòng log tự động.</summary>
    public void Track(AuditLog entry) => _pending.Add(entry);

    /// <summary>
    /// Lấy ra (và bỏ khỏi hàng chờ) các dòng khớp bảng/bản ghi để gán tên hành động.
    /// Bỏ khỏi hàng chờ để một dòng không bị hai lời gọi khác nhau đặt tên chồng lên nhau.
    /// </summary>
    public IReadOnlyList<AuditLog> TakeMatching(string? tableName, Guid? recordId)
    {
        var matched = _pending
            .Where(entry =>
                (string.IsNullOrEmpty(tableName) ||
                 string.Equals(entry.TableName, tableName, StringComparison.OrdinalIgnoreCase)) &&
                (recordId is null || entry.RecordId == recordId))
            .ToList();

        foreach (var entry in matched)
            _pending.Remove(entry);

        return matched;
    }
}
