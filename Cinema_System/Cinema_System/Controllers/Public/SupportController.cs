using System.Security.Claims;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers.Public;

/// <summary>
/// Controller xử lý endpoint chat hỗ trợ công khai.
/// </summary>
public class SupportController : Controller
{
    // Giới hạn độ dài tin nhắn gửi tới chatbot (tránh spam payload khổng lồ gây tốn phí API / nghẽn xử lý).
    private const int MaxMessageLength = 2000;

    private readonly IChatbotService _chatbotService;

    public SupportController(IChatbotService chatbotService)
    {
        _chatbotService = chatbotService;
    }

    // Payload gửi từ widget chatbot AJAX.
    public class MessageRequest
    {
        public string? Message { get; set; }
    }

    /// <summary>
    /// Nhận tin nhắn người dùng từ widget, chuyển tới dịch vụ chatbot và trả về phản hồi AI.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApiSendMessage([FromBody] MessageRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { success = false, error = "Message is required" });

        var message = request.Message.Trim();
        if (message.Length > MaxMessageLength)
            return BadRequest(new { success = false, error = $"Tin nhắn tối đa {MaxMessageLength} ký tự." });

        Guid? currentUserId = null;
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdClaim, out var parsedUserId))
        {
            currentUserId = parsedUserId;
        }

        var botReply = await _chatbotService.HandleMessageAsync(message, currentUserId, HttpContext.Session.Id);

        return Ok(new
        {
            success = true,
            bot = new { role = botReply.Role, content = botReply.Content, createdAt = botReply.CreatedAt }
        });
    }
}
