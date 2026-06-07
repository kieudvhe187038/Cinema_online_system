namespace Cinema_System.Application.ViewModels
{
    public class ProfileViewModel
    {
        //ViewModel = "Túi đựng dữ liệu" Controller gửi sang View để hiển thị thông tin hồ sơ.
        //Các thuộc tính tương ứng cột trong bảng Users (theo thiết kế DB của nhóm).
        public int Id {  get; set; }
        public string FullName {  get; set; }
        public string Email {  get; set; }
        public string? Phone {  get; set; }
        public string? AvatarUrl {  get; set; }
        public string RoleName {  get; set; }
        public int RewardPoints {  get; set; }
        public string Status {  get; set; }
        public DateTime CreatedAt {  get; set; }
    }
}
