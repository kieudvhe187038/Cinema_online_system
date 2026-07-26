namespace Cinema_System.Application.ViewModels;

// Gói dữ liệu 1 trang lịch sử điểm để truyền ra View
public class PointHistoryPageViewModel
{
    public List<PointHistoryViewModel> Items { get; set; } = new();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }

    // Tổng điểm hiện có (cho phần hero)
    public int CurrentPoints { get; set; }

    // Chính sách điểm đang áp dụng (Manager cấu hình) — để khách biết cách tích và cách dùng điểm.
    public decimal VndPerPoint { get; set; }
    public int PointValueVnd { get; set; }

    // Số tiền quy đổi được từ toàn bộ điểm đang có.
    public decimal CurrentPointsValueVnd => (decimal)CurrentPoints * PointValueVnd;
}
