namespace Cinema_System.Application.ViewModels;

/// <summary>
/// Dữ liệu cho partial phân trang dùng chung <c>_Pagination.cshtml</c>.
/// </summary>
public class PaginationViewModel
{
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int PageSize { get; set; }
    public int TotalCount { get; set; }

    /// <summary>Action/Controller để dựng link trang (mặc định Index).</summary>
    public string Action { get; set; } = "Index";
    public string Controller { get; set; } = null!;

    /// <summary>Nhãn đối tượng để hiển thị "… trên N {label}" (vd "phòng", "voucher").</summary>
    public string ItemLabel { get; set; } = "mục";

    /// <summary>Tham số route giữ lại trên mỗi link trang (vd tab, search, status).</summary>
    public IReadOnlyDictionary<string, string?> RouteValues { get; set; } =
        new Dictionary<string, string?>();
}
