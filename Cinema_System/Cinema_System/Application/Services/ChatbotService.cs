using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Cinema_System.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Cinema_System.Application.Services;

/// <summary>
/// Dịch vụ chatbot AI chuyển tiếp tin nhắn người dùng tới Google Gemini và lưu nhật ký hội thoại.
/// </summary>
public class ChatbotService : IChatbotService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<ChatbotService> _logger;

    public ChatbotService(IUnitOfWork unitOfWork, IHttpClientFactory httpFactory, IConfiguration config, ILogger<ChatbotService> logger)
    {
        _unitOfWork = unitOfWork;
        _httpFactory = httpFactory;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Xử lý tin nhắn hỗ trợ, gọi Google Gemini và lưu nhật ký hội thoại.
    /// </summary>
    public async Task<SupportChatMessage> HandleMessageAsync(string? userMessage, Guid? userId, string sessionId)
    {
        var cleanedMessage = userMessage?.Trim() ?? string.Empty;
        var googleKey = _config["AI:Google:ApiKey"];

        string response;

        if (!string.IsNullOrWhiteSpace(googleKey))
        {
            try
            {
                response = await CallGoogleAiAsync(cleanedMessage, googleKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Google AI request failed.");
                response = "Rất tiếc, dịch vụ trợ lý đang tạm thời bận. Vui lòng thử lại trong vài phút.";
            }
        }
        else
        {
            _logger.LogWarning("Google AI key is not configured.");
            response = "Xin lỗi, chatbot chưa được cấu hình đầy đủ. Vui lòng kiểm tra lại.";
        }

        var log = new ChatbotLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SessionId = string.IsNullOrWhiteSpace(sessionId) ? Guid.NewGuid().ToString() : sessionId,
            UserMessage = cleanedMessage,
            BotResponse = response,
            IntentDetected = null,
            CreatedAt = DateTime.Now
        };

        await _unitOfWork.ChatbotLogs.AddAsync(log);
        await _unitOfWork.SaveChangesAsync();

        return new SupportChatMessage
        {
            Role = "assistant",
            Content = response,
            CreatedAt = DateTime.Now
        };
    }

    /// <summary>
    /// Gửi prompt người dùng tới Google Gemini dùng model và URL API cấu hình.
    /// </summary>
    private async Task<string> CallGoogleAiAsync(string message, string apiKey)
    {
        var model = _config["AI:Google:Model"]?.Trim() ?? "gemini-2.5-flash";
        var apiUrl = _config["AI:Google:ApiUrl"]?.Trim() ?? $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";

        var client = _httpFactory.CreateClient();
        apiUrl = AppendApiKeyQuery(apiUrl, apiKey);
        client.DefaultRequestHeaders.Add("x-goog-api-key", apiKey);

        var systemInstruction = "Bạn là trợ lý hỗ trợ người dùng của dự án CineStar. Hãy trả lời đơn giản, rõ ràng và thân thiện như đang hướng dẫn một khách hàng hoặc người dùng cuối. Chỉ trả lời các câu hỏi liên quan tới dự án này, bao gồm chức năng web, cách sử dụng, các tính năng, cấu hình, và hướng dẫn hỗ trợ người dùng. Nếu câu hỏi không liên quan, hãy trả lời ngắn gọn rằng bạn chỉ hỗ trợ dự án CineStar và không trả lời ngoài phạm vi.";

        var body = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = systemInstruction }
                    }
                },
                new
                {
                    parts = new[]
                    {
                        new { text = message }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.3,
                maxOutputTokens = 500
            }
        };

        const int maxAttempts = 3;
        var delayMillis = 500;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var response = await client.PostAsJsonAsync(apiUrl, body);
            var text = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = ParseGoogleAiResponse(text);
                if (string.IsNullOrWhiteSpace(result))
                {
                    _logger.LogWarning("Google AI returned an unrecognized response format. Response body: {Body}", text);
                    return string.Empty;
                }

                return result.Trim();
            }

            if (attempt == maxAttempts || !ShouldRetry(response.StatusCode))
            {
                throw new Exception($"Google AI request failed: {response.StatusCode} {text}");
            }

            _logger.LogWarning("Google AI request returned {StatusCode}, retry {Attempt}/{MaxAttempts} after {Delay}ms.", response.StatusCode, attempt, maxAttempts, delayMillis);
            await Task.Delay(delayMillis);
            delayMillis *= 2;
        }

        throw new Exception("Google AI request failed after retries.");
    }

    private static bool ShouldRetry(System.Net.HttpStatusCode statusCode)
    {
        return statusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
               statusCode == System.Net.HttpStatusCode.TooManyRequests ||
               statusCode == System.Net.HttpStatusCode.InternalServerError ||
               statusCode == System.Net.HttpStatusCode.GatewayTimeout;
    }

    /// <summary>
    /// Nối khóa Google API vào URL endpoint nếu chưa có.
    /// </summary>
    private static string AppendApiKeyQuery(string apiUrl, string apiKey)
    {
        if (apiUrl.Contains("key=", StringComparison.OrdinalIgnoreCase))
            return apiUrl;

        return apiUrl + (apiUrl.Contains("?", StringComparison.OrdinalIgnoreCase) ? "&" : "?") + $"key={Uri.EscapeDataString(apiKey)}";
    }

    /// <summary>
    /// Phân tích JSON phản hồi của Gemini và lấy phần văn bản sinh ra đầu tiên.
    /// </summary>
    private static string ParseGoogleAiResponse(string rawJson)
    {
        try
        {
            using var document = JsonDocument.Parse(rawJson);
            var root = document.RootElement;

            if (root.TryGetProperty("candidates", out var candidates) && candidates.ValueKind == JsonValueKind.Array && candidates.GetArrayLength() > 0)
            {
                var candidate = candidates[0];
                if (candidate.TryGetProperty("content", out var content) && content.TryGetProperty("parts", out var parts) && parts.ValueKind == JsonValueKind.Array && parts.GetArrayLength() > 0)
                {
                    var firstPart = parts[0];
                    if (firstPart.TryGetProperty("text", out var textPart))
                        return textPart.GetString() ?? string.Empty;
                }
            }

            if (root.TryGetProperty("output", out var output) && output.ValueKind == JsonValueKind.String)
            {
                return output.GetString() ?? string.Empty;
            }

            return string.Empty;
        }
        catch (JsonException)
        {
            return string.Empty;
        }
    }


}
