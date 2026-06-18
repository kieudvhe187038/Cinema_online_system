namespace Cinema_System.Application.Common;

/// <summary>
/// Phân loại cấu hình giá — ứng với 4 bảng Price_*_Configs.
/// Giá vé cuối = Giá cơ bản + Phụ thu phòng + Phụ thu ghế + Phụ thu theo giờ.
/// </summary>
public static class PriceKind
{
    public const string Base = "Base";   // Giá cơ bản (Price_Base_Configs)
    public const string Room = "Room";   // Phụ thu theo loại phòng (Price_Room_Type_Configs)
    public const string Seat = "Seat";   // Phụ thu theo loại ghế (Price_Seat_Configs)
    public const string Time = "Time";   // Phụ thu theo khung giờ/ngày (Price_Time_Configs)

    public static readonly IReadOnlySet<string> AllowedValues =
        new HashSet<string>(StringComparer.Ordinal) { Base, Room, Seat, Time };

    // Nhãn tiếng Việt hiển thị cho từng loại giá.
    public static string Label(string? kind) => kind switch
    {
        Base => "Giá cơ bản",
        Room => "Phụ thu loại phòng",
        Seat => "Phụ thu loại ghế",
        Time => "Phụ thu theo giờ",
        _ => "—"
    };
}

/// <summary>Trạng thái lưu DB của một cấu hình giá (khớp CK_P*_status: Active/Inactive).</summary>
public static class PriceConfigStatus
{
    public const string Active = "Active";
    public const string Inactive = "Inactive";
}

/// <summary>
/// Trạng thái hiển thị suy ra theo thời gian + cờ ngừng (KHÔNG do người dùng chỉnh trực tiếp).
/// Cấu hình tự "Hết hạn" sau khi qua mốc effective_to; "Đã ngừng" nếu bị end thủ công (status = Inactive).
/// </summary>
public static class PriceDisplayStatus
{
    public const string Scheduled = "Scheduled"; // Sắp áp dụng (effective_from còn ở tương lai)
    public const string Active = "Active";        // Đang áp dụng
    public const string Expired = "Expired";      // Đã hết hạn (qua effective_to)
    public const string Cancelled = "Cancelled";  // Đã ngừng thủ công (status lưu = Inactive)

    public static string Resolve(DateTime from, DateTime? to, DateTime now, string? storedStatus)
        => storedStatus == PriceConfigStatus.Inactive ? Cancelled
         : from > now ? Scheduled
         : (to is null || to > now) ? Active
         : Expired;

    // Chỉ cho phép Sửa / Ngừng những cấu hình còn hiệu lực hoặc sắp áp dụng.
    public static bool CanModify(string display) => display is Scheduled or Active;
}

/// <summary>
/// Nhóm quy tắc phụ thu theo giờ (cột rule_group) — bắt buộc chọn 1 trong 2.
/// DAY = phụ thu theo loại ngày (thứ / ngày cụ thể); TIME = phụ thu theo khung giờ.
/// </summary>
public static class PriceRuleGroup
{
    public const string Day = "DAY";
    public const string Time = "TIME";

    public static readonly IReadOnlySet<string> AllowedValues =
        new HashSet<string>(StringComparer.Ordinal) { Day, Time };

    public static string Label(string? g) => g switch
    {
        Day => "Theo loại ngày",
        Time => "Theo khung giờ",
        _ => g ?? "—"
    };
}

/// <summary>
/// Điều kiện chi tiết của phụ thu theo giờ (cột time_condition).
/// Thuộc nhóm DayType: DayOfWeek | SpecificDate. Thuộc nhóm TimeSlot: TimeRange.
/// </summary>
public static class PriceTimeCondition
{
    public const string DayOfWeek = "DayOfWeek";       // Theo thứ trong tuần
    public const string SpecificDate = "SpecificDate"; // Theo ngày cụ thể (lễ, sự kiện)
    public const string TimeRange = "TimeRange";       // Theo khung giờ trong ngày

    public static readonly IReadOnlySet<string> AllowedValues =
        new HashSet<string>(StringComparer.Ordinal) { DayOfWeek, SpecificDate, TimeRange };

    public static string Label(string? c) => c switch
    {
        DayOfWeek => "Theo thứ trong tuần",
        SpecificDate => "Theo ngày cụ thể",
        TimeRange => "Theo khung giờ",
        _ => c ?? "—"
    };
}

/// <summary>Nhãn thứ trong tuần (CK_PTime_day: 1 = Chủ Nhật → 7 = Thứ Bảy).</summary>
public static class PriceDayOfWeek
{
    public static string Label(int? d) => d switch
    {
        1 => "Chủ Nhật",
        2 => "Thứ Hai",
        3 => "Thứ Ba",
        4 => "Thứ Tư",
        5 => "Thứ Năm",
        6 => "Thứ Sáu",
        7 => "Thứ Bảy",
        _ => "—"
    };
}
