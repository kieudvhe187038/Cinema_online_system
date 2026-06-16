using System.ComponentModel.DataAnnotations;
using Cinema_System.Application.DTOs;

namespace Cinema_System.Application.ViewModels;

public class UserEditViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    [StringLength(255, ErrorMessage = "Họ tên tối đa 255 ký tự")]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [StringLength(255, ErrorMessage = "Email tối đa 255 ký tự")]
    [Display(Name = "Email")]
    public string Email { get; set; } = null!;

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [StringLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự")]
    [Display(Name = "Số điện thoại")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày sinh")]
    [DataType(DataType.Date)]
    [Display(Name = "Ngày sinh")]
    public DateOnly DateOfBirth { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn vai trò")]
    [Display(Name = "Vai trò")]
    public Guid RoleId { get; set; }

    public IEnumerable<RoleDTO> Roles { get; set; } = new List<RoleDTO>();
}
