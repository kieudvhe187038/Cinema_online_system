namespace Cinema_System.Infrastructure.Email;

/// <summary>
/// Cấu hình SMTP đọc từ section "EmailSettings" trong appsettings.
/// Thông tin nhạy cảm (User/Password) nên đặt ở appsettings.Development.json (đã gitignore).
/// </summary>
public class EmailSettings
{
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public bool EnableSsl { get; set; } = true;

    public string User { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FromEmail { get; set; } = string.Empty;

    public string FromName { get; set; } = "CineStar";
}
