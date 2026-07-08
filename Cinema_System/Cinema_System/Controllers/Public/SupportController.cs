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

        Guid? currentUserId = null;
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdClaim, out var parsedUserId))
        {
            currentUserId = parsedUserId;
        }

        var botReply = await _chatbotService.HandleMessageAsync(request.Message.Trim(), currentUserId, HttpContext.Session.Id);

        return Ok(new
        {
            success = true,
            bot = new { role = botReply.Role, content = botReply.Content, createdAt = botReply.CreatedAt }
        });
    }
}
