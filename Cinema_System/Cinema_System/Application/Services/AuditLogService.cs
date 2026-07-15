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

        return new AuditLogListViewModel
        {
            Logs = rows.Select(a => new AuditLogListItemViewModel
            {
                Id = a.Id,
                UserName = a.User?.FullName ?? "—",
                Action = a.Action,
                TableName = a.TableName,
                RecordId = a.RecordId,
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

        return new AuditLogDetailViewModel
        {
            Id = log.Id,
            UserName = log.User?.FullName ?? "—",
            UserEmail = log.User?.Email,
            Action = log.Action,
            TableName = log.TableName,
            RecordId = log.RecordId,
            OldValue = log.OldValue,
            NewValue = log.NewValue,
            IpAddress = log.IpAddress,
            CreatedAt = log.CreatedAt
        };
    }
}
