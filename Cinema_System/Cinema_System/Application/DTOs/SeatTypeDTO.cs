namespace Cinema_System.Application.DTOs;

public class SeatTypeDTO
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public int Capacity { get; set; }

    public int ColumnSpan { get; set; }

    /// <summary>Số ghế thực tế đang dùng loại ghế này.</summary>
    public int SeatCount { get; set; }

    /// <summary>True nếu đang được ghế hoặc cấu hình giá tham chiếu → không cho xóa.</summary>
    public bool InUse { get; set; }
}
