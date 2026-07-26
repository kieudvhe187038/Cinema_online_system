using Cinema_System.Application.Common;
using Cinema_System.Application.ViewModels;

namespace Cinema_System.Application.Interfaces;

/// <summary>
/// Chính sách điểm thưởng dùng chung: tỉ lệ tích điểm + giá trị quy đổi khi tiêu điểm.
/// Mọi nơi cần tính điểm PHẢI đọc qua đây, không đọc thẳng SystemConfig và không hardcode.
/// </summary>
public interface IPointConfigService
{
    /// <summary>Dữ liệu cho màn hình cấu hình của Manager (kèm mô tả, mốc cập nhật).</summary>
    Task<PointRateViewModel> GetRateAsync();

    /// <summary>Chính sách rút gọn để các service tính điểm/giảm giá dùng.</summary>
    Task<PointPolicy> GetPolicyAsync();

    Task<Result> UpdateRateAsync(PointRateViewModel model);
}
