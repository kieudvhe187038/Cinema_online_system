using Cinema_System.Application.DTOs;
using Cinema_System.Application.Common;

namespace Cinema_System.Application.ViewModels;

/// <summary>
/// ViewModel chứa thông tin chi tiết một bộ phim và các đánh giá được phân trang tương ứng
/// </summary>
public class MovieDetailsViewModel
{
    /// <summary>Thông tin chi tiết của bộ phim</summary>
    public MovieDTO Movie { get; set; } = null!;

    /// <summary>Danh sách các đánh giá (Reviews) đã duyệt của bộ phim, có hỗ trợ phân trang</summary>
    public PagedResult<ReviewDTO> Reviews { get; set; } = new PagedResult<ReviewDTO>();
}
