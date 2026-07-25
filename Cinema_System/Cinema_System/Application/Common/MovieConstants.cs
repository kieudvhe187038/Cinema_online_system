namespace Cinema_System.Application.Common;

/// <summary>Trạng thái phim dùng chung cho truy vấn và lọc.</summary>
public static class MovieStatus
{
    public const string NowShowing = "Now Showing";
    public const string ComingSoon = "Coming Soon";

    // Phim chiếu sớm: chưa tới ngày khởi chiếu nhưng đã có suất chiếu trước ngày đó.
    public const string Special = "Special";

    // Phim đã ngừng chiếu.
    public const string Stopped = "Stopped";

    // So sánh "đã ngừng chiếu" không phân biệt hoa thường (data có thể là "Stopped").
    public const string StoppedLower = "stopped";
}

/// <summary>
/// Quy tắc suy ra trạng thái phim. Manager KHÔNG chọn trạng thái bằng tay: trạng thái được
/// tính từ ngày khởi chiếu + lịch chiếu đã xếp (xem <c>IMovieStatusService</c>).
/// </summary>
public static class MovieStatusPolicy
{
    /// <summary>Ngừng chiếu là quyết định thủ công của Manager — không được tự động ghi đè.</summary>
    public static bool IsManagerControlled(string? status)
        => string.Equals(status, MovieStatus.Stopped, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Đã tới ngày khởi chiếu (hoặc không có ngày) -> Đang chiếu.
    /// Chưa tới ngày mà đã xếp suất chiếu trước ngày đó -> Chiếu sớm.
    /// Còn lại -> Sắp chiếu.
    /// </summary>
    public static string Resolve(DateOnly? releaseDate, bool hasShowtimeBeforeRelease, DateOnly today)
    {
        if (releaseDate is null || releaseDate.Value <= today)
            return MovieStatus.NowShowing;

        return hasShowtimeBeforeRelease ? MovieStatus.Special : MovieStatus.ComingSoon;
    }
}

/// <summary>Cấu hình độ tuổi.</summary>
public static class AgeRatingPolicy
{
    // Thứ tự hiển thị ưu tiên các nhãn độ tuổi.
    public static readonly string[] DisplayOrder = { "P", "C13", "C16", "C18" };
}

/// <summary>Trạng thái suất chiếu (do <c>ShowtimeStatusBackgroundService</c> đồng bộ theo giờ chiếu).</summary>
public static class ShowtimeStatus
{
    // Đã lên lịch, chưa tới giờ chiếu — trạng thái DUY NHẤT còn bán vé được.
    public const string Scheduled = "Scheduled";

    // Đang chiếu (đã qua giờ bắt đầu, chưa tới giờ kết thúc).
    public const string Live = "Live";

    // Đã chiếu xong.
    public const string Completed = "Completed";

    // Bị hủy (sự cố / bảo trì phòng).
    public const string Cancelled = "Cancelled";
}

/// <summary>
/// Quy tắc "suất chiếu còn bán được": CHƯA tới giờ chiếu và còn ở trạng thái đã lên lịch.
/// Dùng chung cho lịch chiếu công khai, đặt vé online và bán vé tại quầy — suất ở quá khứ
/// hoặc đang chiếu (Live) không được hiển thị để chọn, cũng không được tạo đơn.
/// </summary>
public static class ShowtimeSalePolicy
{
    public const string ClosedMessage = "Suất chiếu đã bắt đầu hoặc không còn nhận đặt vé.";

    // Không dịch được sang SQL — chỉ dùng để kiểm tra lại trong bộ nhớ sau khi đã tải suất chiếu.
    public static bool IsOpenForSale(DateTime startTime, string? status, DateTime now)
        => startTime > now
           && string.Equals(status, ShowtimeStatus.Scheduled, StringComparison.OrdinalIgnoreCase);
}

/// <summary>Phạm vi thời gian khách được xem lịch chiếu và đặt vé trước.</summary>
public static class ShowtimeBrowsing
{
    // Số ngày tới hiển thị trên trang lịch chiếu, popup đặt vé nhanh và nút "Mua vé" của thẻ phim.
    // Đủ dài để đặt vé trước cho phim sắp chiếu / chiếu sớm.
    public const int AdvanceDays = 14;
}

/// <summary>Cấu hình phân trang phim.</summary>
public static class MoviePaging
{
    // Màn danh sách phim hiển thị 4 phim/hàng x 2 hàng.
    public const int DefaultPageSize = 8;
}
