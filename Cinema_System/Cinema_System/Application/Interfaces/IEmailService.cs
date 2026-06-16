namespace Cinema_System.Application.Interfaces;

/// <summary>
/// Dịch vụ gửi email (OTP, thông báo...).
/// </summary>
public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody);
}
