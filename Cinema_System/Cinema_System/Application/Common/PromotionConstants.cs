namespace Cinema_System.Application.Common;

/// <summary>Loại giảm giá của voucher (khớp CK_Promotions_type trong DB).</summary>
public static class PromotionDiscountType
{
    public const string Percent = "Percent";
    public const string FixedAmount = "Fixed Amount";

    // Tập giá trị hợp lệ — phải khớp đúng CK_Promotions_type để VM chặn trước khi xuống DB.
    public static readonly IReadOnlySet<string> AllowedValues =
        new HashSet<string>(StringComparer.Ordinal) { Percent, FixedAmount };
}

/// <summary>Phạm vi áp dụng voucher (khớp CK_Promotions_target trong DB).</summary>
public static class PromotionTarget
{
    public const string All = "All";
    public const string TicketOnly = "Ticket_Only";
    public const string FoodOnly = "Food_Only";

    // Tập giá trị hợp lệ — phải khớp đúng CK_Promotions_target.
    public static readonly IReadOnlySet<string> AllowedValues =
        new HashSet<string>(StringComparer.Ordinal) { All, TicketOnly, FoodOnly };
}

/// <summary>Trạng thái voucher (khớp CK_Promotions_status trong DB).</summary>
public static class PromotionStatus
{
    public const string Active = "Active";
    public const string Expired = "Expired";
    public const string Disabled = "Disabled";

    // Tập giá trị hợp lệ — phải khớp đúng CK_Promotions_status.
    public static readonly IReadOnlySet<string> AllowedValues =
        new HashSet<string>(StringComparer.Ordinal) { Active, Expired, Disabled };
}

/// <summary>Cấu hình phân trang danh sách khuyến mãi.</summary>
public static class PromotionPaging
{
    public const int DefaultPageSize = 10;
}
