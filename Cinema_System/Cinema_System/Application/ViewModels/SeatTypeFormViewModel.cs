using System.ComponentModel.DataAnnotations;

namespace Cinema_System.Application.ViewModels;

public class SeatTypeFormViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên loại ghế")]
    [StringLength(100, ErrorMessage = "Tên loại ghế tối đa 100 ký tự")]
    [Display(Name = "Tên loại ghế")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập sức chứa")]
    [Range(1, 10, ErrorMessage = "Sức chứa phải từ 1 đến 10 người")]
    [Display(Name = "Sức chứa (số người ngồi)")]
    public int Capacity { get; set; } = 1;

    [Required(ErrorMessage = "Vui lòng nhập số ô chiếm")]
    [Range(1, 10, ErrorMessage = "Số ô chiếm phải từ 1 đến 10")]
    [Display(Name = "Số ô chiếm trên sơ đồ")]
    public int ColumnSpan { get; set; } = 1;
}
