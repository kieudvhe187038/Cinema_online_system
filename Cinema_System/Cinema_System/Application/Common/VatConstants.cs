namespace Cinema_System.Application.Common;

/// <summary>Trạng thái cấu hình VAT (khớp CK_VAT_status trong DB).</summary>
public static class VatStatus
{
    public const string Active = "Active";
    public const string Inactive = "Inactive";

    // Tập giá trị hợp lệ — phải khớp đúng CK_VAT_status để VM chặn trước khi xuống DB.
    public static readonly IReadOnlySet<string> AllowedValues =
        new HashSet<string>(StringComparer.Ordinal) { Active, Inactive };
}
