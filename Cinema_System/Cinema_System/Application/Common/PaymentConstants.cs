namespace Cinema_System.Application.Common;

/// <summary>
/// Phương thức thanh toán mà hệ thống THỰC SỰ sinh ra (khớp CK_Payments_method trong DB).
/// Thêm phương thức mới phải sửa đồng thời: hằng số ở đây, CHECK constraint trong
/// SQL/CinemaWebDB_v2.sql, và giao diện chọn phương thức tương ứng.
/// </summary>
public static class PaymentMethod
{
    // --- Tại quầy (CounterBookingService) ---
    public const string Cash = "Cash";
    public const string Transfer = "Transfer";

    // --- Online (ShowtimeController) ---
    public const string VnPay = "VNPay";
    public const string VietQr = "VietQR";

    // Đơn 0đ khi mã giảm giá/điểm thưởng phủ hết tiền — không đi qua cổng nào.
    public const string Free = "Free";

    /// <summary>Phương thức nhân viên chọn được ở quầy.</summary>
    public static readonly IReadOnlySet<string> CounterMethods =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Cash, Transfer };

    /// <summary>Phương thức khách chọn được khi đặt online.</summary>
    public static readonly IReadOnlySet<string> OnlineMethods =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { VnPay, VietQr };

    /// <summary>Thứ tự hiển thị trong báo cáo.</summary>
    public static readonly string[] DisplayOrder = { Cash, Transfer, VnPay, VietQr, Free };

    private static readonly Dictionary<string, string> Labels = new(StringComparer.OrdinalIgnoreCase)
    {
        [Cash] = "Tiền mặt",
        [Transfer] = "Chuyển khoản (quầy)",
        [VnPay] = "VNPay",
        [VietQr] = "VietQR",
        [Free] = "Đơn 0đ"
    };

    /// <summary>
    /// Nhãn tiếng Việt để hiển thị. Giá trị lạ (dữ liệu cũ trước khi chuẩn hóa) GIỮ NGUYÊN mã
    /// thay vì bỏ đi — báo cáo mà giấu bớt giao dịch thì tổng tiền sẽ sai.
    /// </summary>
    public static string Label(string? method)
        => method is not null && Labels.TryGetValue(method, out var label) ? label : method ?? string.Empty;

    /// <summary>Vị trí sắp xếp trong báo cáo; giá trị lạ đẩy xuống cuối danh sách.</summary>
    public static int SortIndex(string? method)
    {
        var index = Array.FindIndex(DisplayOrder, m => string.Equals(m, method, StringComparison.OrdinalIgnoreCase));
        return index >= 0 ? index : DisplayOrder.Length;
    }
}
