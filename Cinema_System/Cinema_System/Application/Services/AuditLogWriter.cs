using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Cinema_System.Application.Common;
using Cinema_System.Application.Interfaces;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Services;

/// <summary>
/// Đặt tên hành động nghiệp vụ cho nhật ký. Nội dung thay đổi (cột nào, cũ/mới) do
/// <c>AuditSaveChangesInterceptor</c> tự ghi khi SaveChanges; lớp này chỉ đổi tên hành động của
/// những dòng vừa sinh ra trong request (vd "UPDATE_MOVIES" -> "TOGGLE_MOVIE_STATUS") để log đọc
/// theo nghiệp vụ thay vì theo bảng. Thao tác không đụng tới DB (không có dòng tự động nào) thì
/// vẫn ghi một dòng riêng như trước.
/// </summary>
public class AuditLogWriter : IAuditLogWriter
{
    // Cột action trong DB giới hạn 100 ký tự.
    private const int MaxActionLength = 100;

    // Giữ tiếng Việt/ký tự đặc biệt dễ đọc trong cột old_value/new_value thay vì \uXXXX.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AuditRequestLog _requestLog;
    private readonly ILogger<AuditLogWriter> _logger;

    public AuditLogWriter(
        IUnitOfWork unitOfWork,
        IHttpContextAccessor httpContextAccessor,
        AuditRequestLog requestLog,
        ILogger<AuditLogWriter> logger)
    {
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
        _requestLog = requestLog;
        _logger = logger;
    }

    public async Task LogAsync(string action, string? tableName = null, Guid? recordId = null,
        object? oldValue = null, object? newValue = null)
    {
        try
        {
            action = action.Length > MaxActionLength ? action[..MaxActionLength] : action;

            // Ưu tiên gắn tên vào dòng interceptor vừa sinh -> tránh 2 dòng cho cùng một thao tác.
            if (await TryRenameAutoEntriesAsync(action, tableName, recordId, oldValue, newValue))
                return;

            var httpContext = _httpContextAccessor.HttpContext;

            // Audit_Logs.user_id là FK bắt buộc — không xác định được người thực hiện thì bỏ qua.
            var userIdClaim = httpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("Bỏ qua audit log '{Action}': không xác định được user hiện tại.", action);
                return;
            }

            await _unitOfWork.AuditLogs.AddAsync(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Action = action,
                TableName = tableName,
                RecordId = recordId,
                OldValue = oldValue is null ? null : JsonSerializer.Serialize(oldValue, JsonOptions),
                NewValue = newValue is null ? null : JsonSerializer.Serialize(newValue, JsonOptions),
                IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString(),
                CreatedAt = DateTime.Now
            });
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Ghi log thất bại không được làm hỏng thao tác nghiệp vụ đã thành công.
            _logger.LogWarning(ex, "Ghi audit log '{Action}' thất bại.", action);
        }
    }

    /// <summary>
    /// Đổi tên hành động cho các dòng interceptor đã tự sinh trong request này và bổ sung dữ liệu
    /// Controller truyền vào (nếu interceptor chưa có). Trả về false khi không có dòng nào khớp —
    /// khi đó người gọi ghi một dòng độc lập như cũ.
    /// </summary>
    private async Task<bool> TryRenameAutoEntriesAsync(string action, string? tableName, Guid? recordId,
        object? oldValue, object? newValue)
    {
        var autoEntries = _requestLog.TakeMatching(tableName, recordId);
        if (autoEntries.Count == 0) return false;

        foreach (var entry in autoEntries)
        {
            entry.Action = action;
            entry.RecordId ??= recordId;

            // Interceptor không thấy được thông tin ngoài DB (vd mật khẩu tạm) nên chỉ bù khi còn trống.
            if (oldValue is not null && entry.OldValue is null)
                entry.OldValue = JsonSerializer.Serialize(oldValue, JsonOptions);
            if (newValue is not null && entry.NewValue is null)
                entry.NewValue = JsonSerializer.Serialize(newValue, JsonOptions);

            _unitOfWork.AuditLogs.Update(entry);
        }

        await _unitOfWork.SaveChangesAsync();
        return true;
    }
}
