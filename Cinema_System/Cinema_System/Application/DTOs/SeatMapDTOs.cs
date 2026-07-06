namespace Cinema_System.Application.DTOs;

/// <summary>Loại ghế dùng cho palette của trình thiết kế sơ đồ.</summary>
public class SeatTypeOptionDTO
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    /// <summary>Số người ngồi (thường=1, đôi=2, ba=3).</summary>
    public int Capacity { get; set; }

    /// <summary>Số ô loại ghế này chiếm trên sơ đồ.</summary>
    public int ColumnSpan { get; set; }
}

/// <summary>
/// Một ghế do trình thiết kế gửi lên (parse từ <c>SeatsJson</c>).
/// Span lấy từ <c>Seat_Types.column_span</c> ở server nên không gửi kèm.
/// </summary>
public class SeatInputDTO
{
    public int Row { get; set; }

    /// <summary>Cột bắt đầu; ghế đôi/ba chiếm thêm các cột kế tiếp.</summary>
    public int StartColumn { get; set; }

    public Guid SeatTypeId { get; set; }
}
