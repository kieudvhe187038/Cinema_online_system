using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Cinema_System.Application.Interfaces;
using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Services;

/// <summary>
/// Ghi Audit_Logs cho các endpoint quản trị. Lấy người thực hiện (ClaimTypes.NameIdentifier)
/// và IP từ HttpContext qua IHttpContextAccessor nên Controller không phải truyền tay từng nơi.
/// </summary>
public class AuditLogWriter : IAuditLogWriter
{
    // Giữ tiếng Việt/ký tự đặc biệt dễ đọc trong cột old_value/new_value thay vì \uXXXX.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditLogWriter> _logger;

    public AuditLogWriter(
        IUnitOfWork unitOfWork,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuditLogWriter> logger)
    {
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task LogAsync(string action, string? tableName = null, Guid? recordId = null,
        object? oldValue = null, object? newValue = null)
    {
        try
        {
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
}
