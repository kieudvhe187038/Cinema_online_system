using System.Globalization;
using System.Text;

namespace Cinema_System.Application.Common;

// Chuẩn hóa chuỗi tiếng Việt: bỏ dấu để tìm kiếm/sinh slug.
// Dùng ở tầng ứng dụng (không phải SQL) vì SQL Server không có collation nào vừa bỏ dấu
// vừa coi "đ" = "d" — collation Vietnamese_* xem "đ" là chữ cái riêng.
public static class VietnameseText
{
    // Bỏ dấu: tách ký tự có dấu thành chữ gốc + dấu (FormD) rồi loại bỏ dấu.
    // Riêng đ/Đ không tách được bằng Unicode nên thay thủ công.
    public static string RemoveDiacritics(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                continue;

            builder.Append(character switch
            {
                'đ' => 'd',
                'Đ' => 'D',
                _ => character
            });
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    // Khóa so khớp khi tìm kiếm: bỏ dấu + thường hóa. Trả về chuỗi rỗng nếu không còn ký tự nào.
    public static string ToSearchKey(string? value) => RemoveDiacritics(value).Trim().ToLowerInvariant();

    // True nếu source chứa từ khóa, bỏ qua dấu và hoa-thường.
    // searchKey phải là kết quả của ToSearchKey (chuẩn hóa sẵn để không lặp lại mỗi phần tử).
    public static bool Contains(string? source, string searchKey)
    {
        if (string.IsNullOrEmpty(searchKey)) return true;
        if (string.IsNullOrEmpty(source)) return false;

        return ToSearchKey(source).Contains(searchKey, StringComparison.Ordinal);
    }
}
