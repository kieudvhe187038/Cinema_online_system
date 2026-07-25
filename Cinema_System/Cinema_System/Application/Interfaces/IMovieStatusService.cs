using Cinema_System.Domain.Entities;

namespace Cinema_System.Application.Interfaces;

/// <summary>
/// Đồng bộ trạng thái phim theo ngày khởi chiếu + lịch chiếu đã xếp
/// (Sắp chiếu -> Chiếu sớm -> Đang chiếu). Không đụng tới phim đã Ngừng chiếu.
/// </summary>
public interface IMovieStatusService
{
    /// <summary>
    /// Tính lại trạng thái cho một phim đã tải sẵn. Trả về true nếu trạng thái thay đổi
    /// (mới chỉ đánh dấu Update, người gọi tự SaveChanges).
    /// </summary>
    Task<bool> SyncAsync(Movie movie);

    /// <summary>
    /// Tính lại trạng thái theo Id phim và lưu ngay. Dùng sau khi thêm/sửa/hủy/xóa suất chiếu.
    /// </summary>
    Task SyncAndSaveAsync(Guid movieId);
}
