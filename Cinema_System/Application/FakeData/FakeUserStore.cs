using Cinema_System.Application.ViewModels;

namespace Cinema_System.Application.FakeData
{
    // Đây là "DATABASE GIẢ" tạm thời, chỉ để chạy demo khi chưa nối SQL Server.
    // static = chỉ có 1 bản duy nhất, dữ liệu được giữ lại trong suốt thời gian app chạy.
    public static class FakeUserStore
    {
        // 1 user nhân viên mẫu. Sau này dữ liệu này sẽ lấy từ bảng Users trong SQL Server.
        public static ProfileViewModel CurrentUser = new ProfileViewModel
        {
            Id = 1,
            FullName = "Nguyễn Tiến Dũng",
            Email = "dungnt@cinema.com",
            Phone = "0901234567",
            AvatarUrl = null, //null => View sẽ hiển thị ảnh mặc định
            RoleName = "Customer",
            RewardPoints = 1250,
            Status = "Active",
            CreatedAt = new DateTime(2025, 1, 25)
        };

        // Mật khẩu giả để demo chức năng đổi mật khẩu (DB thật sẽ lưu password_hash đã mã hóa).
        public static string CurrentPassword = "123456";
    }
}
