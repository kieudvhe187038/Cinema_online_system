namespace Cinema_System.Application.Common;

/// <summary>
/// Kết quả phân trang chung cho tầng Application — độc lập với giao diện
/// (Controller tự map sang ViewModel tương ứng).
/// </summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int CurrentPage { get; init; } = 1;
    public int TotalPages { get; init; } = 1;
    public int PageSize { get; init; }
    public int TotalCount { get; init; }

    /// <summary>
    /// Phân trang một tập đã nạp sẵn trong bộ nhớ: tự kẹp <paramref name="page"/>
    /// vào [1, TotalPages] rồi cắt đúng trang. Dùng chung cho mọi danh sách.
    /// </summary>
    public static PagedResult<T> Create(IEnumerable<T> source, int page, int pageSize)
    {
        var all = source as IReadOnlyList<T> ?? source.ToList();
        var totalCount = all.Count;
        var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);

        if (page < 1) page = 1;
        if (page > totalPages) page = totalPages;

        var items = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResult<T>
        {
            Items = items,
            CurrentPage = page,
            TotalPages = totalPages,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
