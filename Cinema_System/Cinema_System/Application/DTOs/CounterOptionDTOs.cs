namespace Cinema_System.Application.DTOs;

/// <summary>Phim có suất chiếu khả dụng để bán tại quầy.</summary>
public class MovieOptionDTO
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public int? DurationMinutes { get; set; }

    public string? AgeRating { get; set; }
}

/// <summary>Một suất chiếu để chọn ở bước 2.</summary>
public class ShowtimeOptionDTO
{
    public Guid Id { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public string RoomName { get; set; } = string.Empty;

    public string CinemaName { get; set; } = string.Empty;

    public string RoomType { get; set; } = string.Empty;

    public int AvailableSeats { get; set; }
}

/// <summary>Một ô ghế trên sơ đồ chọn ghế.</summary>
public class SeatCellDTO
{
    public Guid Id { get; set; }

    public int RowNumber { get; set; }

    public int SeatNumber { get; set; }

    /// <summary>Nhãn hiển thị, ví dụ A1, B5.</summary>
    public string Label { get; set; } = string.Empty;

    public string SeatTypeName { get; set; } = string.Empty;

    public int ColumnSpan { get; set; } = 1;

    public decimal Price { get; set; }

    /// <summary>Available / Booked / Held / Maintenance ...</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>True nếu ghế đã có vé hoặc đang bị giữ → không chọn được.</summary>
    public bool IsOccupied { get; set; }

    /// <summary>True nếu ghế đang bị NGƯỜI KHÁC giữ tạm (chưa thành vé) — phân biệt với đã bán hẳn, chỉ có ý nghĩa khi IsOccupied = true.</summary>
    public bool IsHeld { get; set; }
}

/// <summary>Kết quả xem trước áp mã khuyến mãi tại quầy (AJAX).</summary>
public class CounterPromoPreviewDTO
{
    public bool Ok { get; set; }

    public string? Message { get; set; }

    public string? Code { get; set; }

    public decimal Discount { get; set; }
}

/// <summary>Sơ đồ ghế của một suất chiếu trả về cho trang quầy.</summary>
public class SeatMapDTO
{
    public Guid ShowtimeId { get; set; }

    public string MovieTitle { get; set; } = string.Empty;

    public string RoomName { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }

    public int TotalRows { get; set; }

    public int TotalColumns { get; set; }

    public List<SeatCellDTO> Seats { get; set; } = new();
}

/// <summary>Món ăn / nước uống còn bán.</summary>
public class FoodOptionDTO
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string? ImageUrl { get; set; }
}
