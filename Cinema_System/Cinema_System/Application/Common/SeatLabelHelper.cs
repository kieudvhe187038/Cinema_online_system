namespace Cinema_System.Application.Common;

/// <summary>Tạo nhãn ghế dạng A1, B5... từ số hàng/cột.</summary>
public static class SeatLabelHelper
{
    public static string Build(int rowNumber, int seatNumber)
    {
        var rowLabel = rowNumber >= 1 && rowNumber <= 26
            ? ((char)('A' + rowNumber - 1)).ToString()
            : rowNumber.ToString();

        return $"{rowLabel}{seatNumber}";
    }
}
