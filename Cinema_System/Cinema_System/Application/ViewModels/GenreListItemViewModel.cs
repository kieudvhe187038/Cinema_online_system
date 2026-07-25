namespace Cinema_System.Application.ViewModels;

/// <summary>Một dòng trong bảng danh sách thể loại phim của Manager.</summary>
public class GenreListItemViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;

    /// <summary>Số phim đang được gán thể loại này.</summary>
    public int MovieCount { get; set; }

    /// <summary>Đang được dùng bởi ít nhất một phim → không cho xóa.</summary>
    public bool InUse => MovieCount > 0;
}
