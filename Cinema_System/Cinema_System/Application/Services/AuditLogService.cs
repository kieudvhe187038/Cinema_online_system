using System.Text.Json;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Cinema_System.Application.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IUnitOfWork _unitOfWork;

    public AuditLogService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AuditLogListViewModel> GetLogsAsync(string? search, int page, int pageSize)
    {
        if (page < 1) page = 1;
        var key = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        // Lọc + phân trang đẩy xuống SQL (kèm người thực hiện) — không kéo cả bảng về.
        var (rows, totalCount) = await _unitOfWork.AuditLogs.GetPagedAsync(
            page, pageSize,
            predicate: key == null
                ? null
                : a => a.Action.Contains(key)
                    || (a.TableName != null && a.TableName.Contains(key))
                    || a.User.FullName.Contains(key)
                    || a.User.Email.Contains(key),
            include: q => q.Include(a => a.User),
            orderBy: q => q.OrderByDescending(a => a.CreatedAt));

        var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);

        // Trang yêu cầu vượt quá số trang thực tế -> truy vấn lại đúng trang cuối
        // (tránh danh sách rỗng trong khi CurrentPage lệch với dữ liệu).
        if (page > totalPages)
        {
            page = totalPages;
            (rows, totalCount) = await _unitOfWork.AuditLogs.GetPagedAsync(
                page, pageSize,
                predicate: key == null
                    ? null
                    : a => a.Action.Contains(key)
                        || (a.TableName != null && a.TableName.Contains(key))
                        || a.User.FullName.Contains(key)
                        || a.User.Email.Contains(key),
                include: q => q.Include(a => a.User),
                orderBy: q => q.OrderByDescending(a => a.CreatedAt));
        }

        // Tra tên dễ đọc của bản ghi bị tác động cho cả trang (mỗi bảng 1 truy vấn).
        var labels = await new AuditRecordLabeler(_unitOfWork).ResolveAsync(rows);

        return new AuditLogListViewModel
        {
            Logs = rows.Select(a => new AuditLogListItemViewModel
            {
                Id = a.Id,
                UserName = a.User?.FullName ?? "—",
                Action = a.Action,
                TableName = a.TableName,
                RecordId = a.RecordId,
                RecordLabel = labels.TryGetValue(a.Id, out var label) ? label : null,
                IpAddress = a.IpAddress,
                CreatedAt = a.CreatedAt
            }).ToList(),
            Search = search,
            CurrentPage = page,
            TotalPages = totalPages,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<AuditLogDetailViewModel?> GetLogDetailAsync(Guid id)
    {
        var log = await _unitOfWork.AuditLogs.FirstOrDefaultAsync(
            a => a.Id == id,
            include: q => q.Include(a => a.User));
        if (log is null) return null;

        var labels = await new AuditRecordLabeler(_unitOfWork).ResolveAsync(new[] { log });

        return new AuditLogDetailViewModel
        {
            Id = log.Id,
            UserName = log.User?.FullName ?? "—",
            UserEmail = log.User?.Email,
            Action = log.Action,
            TableName = log.TableName,
            RecordId = log.RecordId,
            RecordLabel = labels.TryGetValue(log.Id, out var label) ? label : null,
            OldValue = log.OldValue,
            NewValue = log.NewValue,
            IpAddress = log.IpAddress,
            CreatedAt = log.CreatedAt,
            Changes = BuildChanges(log.OldValue, log.NewValue)
        };
    }

    /// <summary>
    /// Ghép old_value/new_value thành bảng so sánh từng cột. Trả về danh sách rỗng nếu JSON không
    /// phải dạng đối tượng (log cũ ghi tay có thể là mảng/chuỗi) — khi đó view hiển thị JSON thô.
    /// </summary>
    private static List<AuditFieldChangeViewModel> BuildChanges(string? oldValue, string? newValue)
    {
        var oldFields = ParseObject(oldValue);
        var newFields = ParseObject(newValue);
        if (oldFields is null && newFields is null) return new List<AuditFieldChangeViewModel>();

        // Giữ thứ tự cột của bản ghi mới trước, rồi tới cột chỉ có ở bản ghi cũ (trường hợp xóa).
        var fieldNames = (newFields?.Keys ?? Enumerable.Empty<string>())
            .Concat(oldFields?.Keys ?? Enumerable.Empty<string>())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return fieldNames.Select(field => new AuditFieldChangeViewModel
        {
            Field = field,
            OldValue = oldFields is not null && oldFields.TryGetValue(field, out var before) ? before : null,
            NewValue = newFields is not null && newFields.TryGetValue(field, out var after) ? after : null
        }).ToList();
    }

    // JSON đối tượng -> dictionary cột/giá trị dạng chuỗi; null nếu không parse được hoặc không phải object.
    private static Dictionary<string, string?>? ParseObject(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object) return null;

            return document.RootElement.EnumerateObject().ToDictionary(
                property => property.Name,
                property => property.Value.ValueKind switch
                {
                    JsonValueKind.Null => null,
                    JsonValueKind.String => property.Value.GetString(),
                    _ => property.Value.ToString()
                },
                StringComparer.Ordinal);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
