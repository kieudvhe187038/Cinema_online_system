namespace Cinema_System.Application.ViewModels;

// Gói dữ liệu 1 trang lịch sử điểm để truyền ra View
public class PointHistoryPageViewModel
{
    public List<PointHistoryViewModel> Items { get; set; } = new();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }
}
