namespace Cinema_System.Application.ViewModels;

/// <summary>
/// Đại diện cho một tin nhắn chatbot trả về cho client.
/// </summary>
public class SupportChatMessage
{
    /// <summary>
    /// Vai trò bot, thường là "assistant" cho phản hồi chatbot.
    /// </summary>
    public string Role { get; set; } = "assistant";

    /// <summary>
    /// Nội dung trả lời do mô hình AI tạo ra.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Thời điểm phản hồi được tạo.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
