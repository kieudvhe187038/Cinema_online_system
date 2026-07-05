namespace Cinema_System.Application.Common;

/// <summary>Tạo nhãn ghế dạng A1, B5... từ số hàng/cột.</summary>
public static class SeatLabelHelper
{
    public static string Build(int rowNumber, int seatNumber)
        => $"{BuildRowLabel(rowNumber)}{seatNumber}";

    /// <summary>
    /// Chuyển số hàng thành nhãn chữ kiểu Excel: 1→A, 26→Z, 27→AA, 28→AB...
    /// Hàng &lt;= 0 giữ nguyên dạng số để không tạo nhãn sai.
    /// </summary>
    private static string BuildRowLabel(int rowNumber)
    {
        if (rowNumber <= 0)
            return rowNumber.ToString();

        var label = string.Empty;
        while (rowNumber > 0)
        {
            var remainder = (rowNumber - 1) % 26;
            label = (char)('A' + remainder) + label;
            rowNumber = (rowNumber - 1) / 26;
        }

        return label;
    }
}
