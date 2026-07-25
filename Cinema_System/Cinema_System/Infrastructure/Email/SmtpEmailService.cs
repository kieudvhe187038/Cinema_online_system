using System.Net;
using System.Net.Mail;
using Cinema_System.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cinema_System.Infrastructure.Email;

/// <summary>
/// Gửi email qua SMTP (mặc định cấu hình cho Gmail).
/// Khi chưa cấu hình User/Password, sẽ ghi nội dung email (kèm OTP) ra log để dev test.
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<EmailSettings> settings, ILogger<SmtpEmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        // Chưa cấu hình SMTP -> dev fallback: log nội dung để test luồng mà không cần email thật.
        if (string.IsNullOrWhiteSpace(_settings.Host)
            || string.IsNullOrWhiteSpace(_settings.User)
            || string.IsNullOrWhiteSpace(_settings.Password))
        {
            _logger.LogWarning(
                "[EMAIL DEV] SMTP chưa cấu hình. Email gửi tới {To} | Tiêu đề: {Subject}\n{Body}",
                to, subject, htmlBody);
            return;
        }

        var fromEmail = string.IsNullOrWhiteSpace(_settings.FromEmail) ? _settings.User : _settings.FromEmail;

        using var message = new MailMessage
        {
            From = new MailAddress(fromEmail, _settings.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(to);

        using var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = _settings.EnableSsl,
            Credentials = new NetworkCredential(_settings.User, _settings.Password)
        };

        await client.SendMailAsync(message);
    }
}
