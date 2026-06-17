namespace Cinema_System.Application.ViewModels;

/// <summary>
/// Model cho partial phân trang dùng chung <c>_Pager.cshtml</c>.
/// Áp dụng cho mọi trang danh sách có phân trang (windowed + ellipsis),
/// tránh tràn viền khi số trang quá lớn.
/// </summary>
public class PagerModel
{
    /// <summary>Trang hiện tại (1-based).</summary>
    public int CurrentPage { get; set; } = 1;

    /// <summary>Tổng số trang.</summary>
    public int TotalPages { get; set; } = 1;

    /// <summary>Tên action xử lý chuyển trang (mặc định "Index").</summary>
    public string Action { get; set; } = "Index";

    /// <summary>Tên controller (bỏ hậu tố "Controller"), ví dụ "StaffBooking".</summary>
    public string Controller { get; set; } = string.Empty;

    /// <summary>
    /// Các tham số lọc cần giữ lại khi chuyển trang (ví dụ search, status...).
    /// Khóa "page" do partial tự thêm — không cần đưa vào đây.
    /// </summary>
    public IDictionary<string, string?>? RouteValues { get; set; }

    /// <summary>Số trang hiển thị mỗi bên của trang hiện tại (cửa sổ). Mặc định 2.</summary>
    public int WindowSize { get; set; } = 2;
}
