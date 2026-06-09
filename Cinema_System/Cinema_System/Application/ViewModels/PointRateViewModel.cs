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

    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
