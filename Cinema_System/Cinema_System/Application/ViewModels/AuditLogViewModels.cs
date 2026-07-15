namespace Cinema_System.Application.ViewModels;

/// <summary>Một dòng trong danh sách nhật ký chỉnh sửa (Audit Log — #49).</summary>
public class AuditLogListItemViewModel
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? TableName { get; set; }
    public Guid? RecordId { get; set; }
    public string? IpAddress { get; set; }
    public DateTime? CreatedAt { get; set; }
}

/// <summary>ViewModel danh sách nhật ký chỉnh sửa có lọc + phân trang.</summary>
public class AuditLogListViewModel
{
    public List<AuditLogListItemViewModel> Logs { get; set; } = new();
    public string? Search { get; set; }

    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int PageSize { get; set; } = 15;
    public int TotalCount { get; set; }
}

/// <summary>ViewModel chi tiết một bản ghi nhật ký (Audit Log Detail — #50).</summary>
public class AuditLogDetailViewModel
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? UserEmail { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? TableName { get; set; }
    public Guid? RecordId { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? IpAddress { get; set; }
    public DateTime? CreatedAt { get; set; }
}
