using Cinema_System.Application.Common;
using Cinema_System.Application.DTOs;
using Cinema_System.Application.ViewModels;

namespace Cinema_System.Application.Interfaces;

public interface IRoomService
{
    Task<PagedResult<RoomDTO>> GetPagedAsync(int page, int pageSize);

    /// <summary>Tạo VM rỗng cho form Thêm (đã nạp dropdown loại phòng + palette loại ghế).</summary>
    Task<RoomFormViewModel> BuildCreateFormAsync();

    Task<RoomFormViewModel?> GetForEditAsync(Guid id);

    /// <summary>Nạp lại dropdown loại phòng + palette loại ghế khi ModelState không hợp lệ.</summary>
    Task PopulateOptionsAsync(RoomFormViewModel model);

    Task<Result> CreateAsync(RoomFormViewModel model);

    /// <summary>Cập nhật phòng. Data = thông báo mô tả kết quả (sửa tại chỗ / tạo bản mới...).</summary>
    Task<Result<string>> UpdateAsync(RoomFormViewModel model);

    Task<Result> DeleteAsync(Guid id);
}
