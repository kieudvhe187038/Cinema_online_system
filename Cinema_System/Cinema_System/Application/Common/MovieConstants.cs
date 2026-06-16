namespace Cinema_System.Application.Common;

/// <summary>Trạng thái phim dùng chung cho truy vấn và lọc.</summary>
public static class MovieStatus
{
    public const string NowShowing = "Now Showing";
    public const string ComingSoon = "Coming Soon";

    // So sánh "đã ngừng chiếu" không phân biệt hoa thường (data có thể là "Stopped").
    public const string StoppedLower = "stopped";
}

/// <summary>Trạng thái suất chiếu đặc biệt.</summary>
public static class ShowtimeStatus
{
    public const string Special = "Special";
    public const string SpecialScreening = "Special Screening";

    // Từ khóa tiếng Việt nhận diện suất chiếu đặc biệt.
    public const string SpecialKeyword = "Đặc";
}

/// <summary>Cấu hình độ tuổi.</summary>
public static class AgeRatingPolicy
{
    // Thứ tự hiển thị ưu tiên các nhãn độ tuổi.
    public static readonly string[] DisplayOrder = { "P", "C13", "C16", "C18" };
}

/// <summary>Cấu hình phân trang phim.</summary>
public static class MoviePaging
{
    // Màn danh sách phim hiển thị 4 phim/hàng x 2 hàng.
    public const int DefaultPageSize = 8;
}
