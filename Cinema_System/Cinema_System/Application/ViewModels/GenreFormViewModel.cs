using System.ComponentModel.DataAnnotations;

namespace Cinema_System.Application.ViewModels;

/// <summary>ViewModel dùng chung cho form Tạo mới và Chỉnh sửa thể loại phim.</summary>
public class GenreFormViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên thể loại")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Tên thể loại phải từ 2 đến 100 ký tự")]
    [Display(Name = "Tên thể loại")]
    public string Name { get; set; } = null!;

    /// <summary>Đang được gán cho ít nhất một phim → khóa nút xóa.</summary>
    public bool InUse { get; set; }
}
