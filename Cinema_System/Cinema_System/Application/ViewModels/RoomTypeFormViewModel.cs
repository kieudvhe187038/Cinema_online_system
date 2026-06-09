using System.ComponentModel.DataAnnotations;

namespace Cinema_System.Application.ViewModels;

public class RoomTypeFormViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên loại phòng")]
    [StringLength(100, ErrorMessage = "Tên loại phòng tối đa 100 ký tự")]
    [Display(Name = "Tên loại phòng")]
    public string Name { get; set; } = null!;

    [Display(Name = "Mô tả")]
    public string? Description { get; set; }
}
