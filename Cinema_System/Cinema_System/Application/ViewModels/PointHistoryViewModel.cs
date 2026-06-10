namespace Cinema_System.Application.ViewModels
{
    public class PointHistoryViewModel
    {
        public int PointsChanged { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? CreatedAt { get; set; }

        // Computed: hiển thị +50 / -20 cho đẹp
        public string PointsDisplay => (PointsChanged >= 0 ? "+" : "") + PointsChanged;
        public bool IsPositive => PointsChanged >= 0;

        // Đổi mã hành động sang tiếng Việt
        public string ActionLabel => ActionType switch
        {
            "Earned" => "Tích điểm",
            "Redeemed" => "Đổi quà",
            "Refund_Rollback" => "Hoàn điểm (hủy vé)",
            _ => ActionType
        };
    }
}
