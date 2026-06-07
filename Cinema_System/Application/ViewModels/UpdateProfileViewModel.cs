using System.ComponentModel.DataAnnotations;

namespace Cinema_System.Application.ViewModels
{
    public class UpdateProfileViewModel
    {
        public int Id {  get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [Display(Name = "Họ và tên")]
        [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự")]
        public string FullName {  get; set; }

        [Display(Name = "Số điện thoại")]
        [RegularExpression(@"^(0\d{9})$", ErrorMessage = "Số điện thoại phải gồm 10 số và bắt đầu bằng 0")]
        public string? Phone {  get; set; }

        //Email để hiển thị (readonly), không cho sửa ở màn hình này
        [Display(Name = "Email")]
        public string? Email {  get; set; }

        //Ảnh đại diện hiện tại (để hiển thị preview), Không phải file upload
        public string? CurrentAvatarUrl {  get; set; }

        //File ảnh người dùng chọn để upload, IFromFile = đại diện 1 file gửi lên server.
        [Display(Name = "Ảnh đại diện mới")]
        public IFormFile? AvatarFile {  get; set; }
    }
}
