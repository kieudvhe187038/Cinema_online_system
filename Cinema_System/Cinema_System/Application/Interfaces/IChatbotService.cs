using Cinema_System.Application.ViewModels;

namespace Cinema_System.Application.Interfaces;

/// <summary>
/// Giao diện dịch vụ chatbot AI hỗ trợ khách hàng.
/// </summary>
public interface IChatbotService
{
    /// <summary>
    /// Xử lý tin nhắn người dùng bằng cách gửi tới nhà cung cấp AI cấu hình sẵn và trả về phản hồi của bot.
    /// </summary>
    Task<SupportChatMessage> HandleMessageAsync(string? userMessage, Guid? userId, string sessionId);
}
