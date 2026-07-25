using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Cinema_System.Application.Common;
using Cinema_System.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cinema_System.Infrastructure.Data;

/// <summary>
/// Tự động ghi Audit_Logs cho MỌI thay đổi dữ liệu do Admin/Manager/Staff thực hiện, đọc trực tiếp
/// từ ChangeTracker của EF Core nên không cần gọi tay ở từng endpoint và luôn có đủ giá trị cũ/mới.
///
/// Phạm vi: chỉ ghi khi request hiện tại có người dùng đăng nhập thuộc role quản trị — nhờ vậy thao
/// tác của khách (đặt vé) và job nền (đồng bộ trạng thái suất chiếu) không làm nhiễu nhật ký.
/// </summary>
public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    // Role được coi là "quản lý hệ thống" — chỉ các role này mới sinh nhật ký.
    private static readonly string[] AuditedRoles = { "ADMIN", "MANAGER", "STAFF" };

    // Khu vực TỰ PHỤC VỤ (đoạn đầu của URL): hồ sơ cá nhân, lịch sử đặt vé, luồng đặt vé/chọn ghế/
    // thanh toán. Đây là tương tác của người dùng cuối, không phải quản trị hệ thống — nên dù người
    // đang đăng nhập là Manager/Staff tự đặt vé hay tự sửa hồ sơ thì cũng KHÔNG ghi nhật ký.
    private static readonly HashSet<string> SelfServiceAreas = new(StringComparer.OrdinalIgnoreCase)
    {
        "profile", "booking", "showtime"
    };

    // Bảng không ghi nhật ký:
    //  - Audit_Logs: tránh đệ quy chính nó.
    //  - Seat_Holds / Email_Logs: dữ liệu kỹ thuật sinh liên tục, không phải thao tác quản trị.
    //  - Tickets / Payments / Booking_Foods / Reward_Point_History: bảng chi tiết của một đơn — một lần
    //    bán vé tại quầy sẽ đẻ ra hàng chục dòng làm ngập nhật ký; bản thân đơn (Bookings) vẫn được ghi.
    private static readonly HashSet<string> IgnoredTables = new(StringComparer.OrdinalIgnoreCase)
    {
        "Audit_Logs", "Seat_Holds", "Email_Logs",
        "Tickets", "Payments", "Booking_Foods", "Reward_Point_History"
    };

    // Cột nhạy cảm: ghi nhận là CÓ thay đổi nhưng không lưu giá trị thật.
    private static readonly HashSet<string> MaskedColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "password_hash"
    };

    private const string MaskedValue = "***";

    // Giữ tiếng Việt dễ đọc trong cột old_value/new_value thay vì \uXXXX.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AuditRequestLog _requestLog;
    private readonly ILogger<AuditSaveChangesInterceptor> _logger;

    public AuditSaveChangesInterceptor(
        IHttpContextAccessor httpContextAccessor,
        AuditRequestLog requestLog,
        ILogger<AuditSaveChangesInterceptor> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _requestLog = requestLog;
        _logger = logger;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        AddAuditEntries(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        AddAuditEntries(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// Duyệt ChangeTracker, dựng dòng Audit_Logs cho từng entity bị thêm/sửa/xóa rồi thêm luôn vào
    /// context — các dòng này đi cùng transaction của thao tác nên không bao giờ lệch dữ liệu.
    /// Ghi nhật ký lỗi KHÔNG được làm hỏng nghiệp vụ chính nên toàn bộ được bọc try/catch.
    /// </summary>
    private void AddAuditEntries(DbContext? context)
    {
        if (context is null) return;

        try
        {
            var userId = GetAuditedUserId();
            if (userId is null) return;

            var ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
            var now = DateTime.Now;

            // ToList() vì thêm entity mới vào context sẽ làm thay đổi danh sách đang duyệt.
            var entries = context.ChangeTracker.Entries()
                .Where(ShouldAudit)
                .ToList();

            foreach (var entry in entries)
            {
                var auditLog = BuildAuditLog(entry, userId.Value, ipAddress, now);
                if (auditLog is null) continue;

                context.Add(auditLog);
                _requestLog.Track(auditLog);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ghi audit log tự động thất bại.");
        }
    }

    /// <summary>
    /// Id người thực hiện, chỉ khi đây thực sự là thao tác quản trị: có HttpContext (loại job nền),
    /// đã đăng nhập với role quản trị (loại khách hàng) và không nằm trong khu tự phục vụ.
    /// </summary>
    private Guid? GetAuditedUserId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null) return null;

        var user = httpContext.User;
        if (user?.Identity?.IsAuthenticated != true) return null;
        if (!AuditedRoles.Any(user.IsInRole)) return null;
        if (IsSelfServiceArea(httpContext.Request.Path)) return null;

        // Audit_Logs.user_id là FK bắt buộc — không parse được thì bỏ qua.
        return Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null;
    }

    // So khớp ĐOẠN ĐẦU của đường dẫn (vd "/Showtime/Confirm" -> "showtime") để không đụng nhầm
    // các khu quản trị có tên gần giống (vd "/Manager/Showtime/...").
    private static bool IsSelfServiceArea(PathString path)
    {
        var value = path.Value;
        if (string.IsNullOrWhiteSpace(value)) return false;

        var firstSegment = value.Trim('/').Split('/')[0];
        return SelfServiceAreas.Contains(firstSegment);
    }

    private static bool ShouldAudit(EntityEntry entry)
    {
        if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            return false;

        // Bảng nối many-to-many (vd Movie_Genres) không có class riêng — bỏ qua để tránh
        // sinh hàng loạt dòng log mỗi lần gán lại thể loại cho phim.
        if (entry.Metadata.IsPropertyBag) return false;

        var tableName = entry.Metadata.GetTableName();
        return !string.IsNullOrEmpty(tableName) && !IgnoredTables.Contains(tableName);
    }

    private static AuditLog? BuildAuditLog(EntityEntry entry, Guid userId, string? ipAddress, DateTime now)
    {
        var tableName = entry.Metadata.GetTableName()!;

        Dictionary<string, object?>? oldValues = null;
        Dictionary<string, object?>? newValues = null;
        string actionPrefix;

        switch (entry.State)
        {
            case EntityState.Added:
                actionPrefix = "CREATE";
                newValues = Snapshot(entry, current: true);
                break;

            case EntityState.Deleted:
                actionPrefix = "DELETE";
                oldValues = Snapshot(entry, current: false);
                break;

            default:
                actionPrefix = "UPDATE";
                oldValues = new Dictionary<string, object?>();
                newValues = new Dictionary<string, object?>();
                foreach (var property in entry.Properties)
                {
                    // Chỉ lấy cột thực sự đổi giá trị (IsModified vẫn true khi gán lại y hệt giá trị cũ).
                    if (!property.IsModified || Equals(property.OriginalValue, property.CurrentValue))
                        continue;

                    var columnName = ColumnName(property);
                    oldValues[columnName] = Mask(columnName, property.OriginalValue);
                    newValues[columnName] = Mask(columnName, property.CurrentValue);
                }
                // Không có cột nào đổi (vd chỉ Attach rồi Update lại y nguyên) -> không ghi log.
                if (newValues.Count == 0) return null;
                break;
        }

        return new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Action = $"{actionPrefix}_{tableName.ToUpperInvariant()}",
            TableName = tableName,
            RecordId = PrimaryKeyValue(entry),
            OldValue = oldValues is null ? null : JsonSerializer.Serialize(oldValues, JsonOptions),
            NewValue = newValues is null ? null : JsonSerializer.Serialize(newValues, JsonOptions),
            IpAddress = ipAddress,
            CreatedAt = now
        };
    }

    // Toàn bộ cột của bản ghi (dùng cho thêm mới/xóa).
    private static Dictionary<string, object?> Snapshot(EntityEntry entry, bool current)
    {
        var values = new Dictionary<string, object?>();
        foreach (var property in entry.Properties)
        {
            var columnName = ColumnName(property);
            values[columnName] = Mask(columnName, current ? property.CurrentValue : property.OriginalValue);
        }
        return values;
    }

    private static string ColumnName(PropertyEntry property)
        => property.Metadata.GetColumnName() ?? property.Metadata.Name;

    private static object? Mask(string columnName, object? value)
        => value is not null && MaskedColumns.Contains(columnName) ? MaskedValue : value;

    /// <summary>Khóa chính dạng Guid của bản ghi; null nếu khóa phức hợp hoặc do DB sinh sau khi lưu.</summary>
    private static Guid? PrimaryKeyValue(EntityEntry entry)
    {
        var keyProperties = entry.Metadata.FindPrimaryKey()?.Properties;
        if (keyProperties is not { Count: 1 }) return null;

        var keyEntry = entry.Property(keyProperties[0].Name);
        if (keyEntry.IsTemporary) return null;

        return keyEntry.CurrentValue is Guid key && key != Guid.Empty ? key : null;
    }
}
