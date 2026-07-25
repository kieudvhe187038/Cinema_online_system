using System.ComponentModel.DataAnnotations;

namespace Cinema_System.Application.ViewModels;

public class PointRateViewModel
{
    /// <summary>
    /// Tỉ lệ tích điểm: số điểm cộng cho mỗi 1 VND chi tiêu.
    /// Ví dụ 0.0001 nghĩa là 10.000đ = 1 điểm.
    /// </summary>
    [Required(ErrorMessage = "Vui lòng nhập tỉ lệ tích điểm")]
    [Range(0, 1, ErrorMessage = "Tỉ lệ phải nằm trong khoảng 0 đến 1")]
    [Display(Name = "Tỉ lệ tích điểm (điểm / 1đ)")]
    public decimal Rate { get; set; }

    /// <summary>
    /// Giá trị quy đổi khi khách DÙNG điểm để giảm giá: 1 điểm = bao nhiêu đồng.
    /// Áp dụng cho cả đặt vé online lẫn bán vé tại quầy.
    /// </summary>
    [Required(ErrorMessage = "Vui lòng nhập giá trị quy đổi của 1 điểm")]
    [Range(1, 1_000_000, ErrorMessage = "Giá trị quy đổi phải từ 1đ đến 1.000.000đ")]
    [Display(Name = "Giá trị quy đổi (đ / 1 điểm)")]
    public int PointValueVnd { get; set; }

    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    public DateTime? UpdatedAt { get; set; }

    // ── Thông tin dẫn giải cho màn hình (không nhập tay) ──

    /// <summary>Số tiền cần chi để được 1 điểm — diễn giải ngược của <see cref="Rate"/>.</summary>
    public decimal VndPerPoint => Rate <= 0 ? 0 : Math.Round(1 / Rate, 0, MidpointRounding.AwayFromZero);
}
