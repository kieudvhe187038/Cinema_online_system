namespace Cinema_System.Application.DTOs;

public class RoomTypeDTO
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    /// <summary>Số phòng đang dùng loại phòng này.</summary>
    public int RoomCount { get; set; }

    /// <summary>True nếu đang được phòng hoặc cấu hình giá tham chiếu → không cho xóa.</summary>
    public bool InUse { get; set; }
}
