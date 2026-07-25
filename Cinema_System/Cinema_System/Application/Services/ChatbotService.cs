using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Cinema_System.Application.DTOs;
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
    private readonly IMovieService _movieService;

    public ChatbotService(IUnitOfWork unitOfWork, IHttpClientFactory httpFactory, IConfiguration config, ILogger<ChatbotService> logger, IMovieService movieService)
    {
        _unitOfWork = unitOfWork;
        _httpFactory = httpFactory;
        _config = config;
        _logger = logger;
        _movieService = movieService;
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
                var knowledge = await BuildKnowledgeContextAsync();
                response = await CallGoogleAiAsync(cleanedMessage, googleKey, knowledge);
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
    /// Dựng ngữ cảnh dữ liệu thật của CineStar (phim, đồ ăn/nước, khuyến mãi, rạp) để AI trả lời chính xác.
    /// </summary>
    private async Task<string> BuildKnowledgeContextAsync()
    {
        var sections = new List<string>();

        var movies = await SafeBuildAsync("phim", BuildMovieCatalogAsync);
        if (!string.IsNullOrWhiteSpace(movies))
            sections.Add("PHIM (mỗi dòng kèm slug để tạo liên kết):\n" + movies);

        var foods = await SafeBuildAsync("đồ ăn/nước", BuildConcessionCatalogAsync);
        if (!string.IsNullOrWhiteSpace(foods))
            sections.Add("ĐỒ ĂN & NƯỚC UỐNG (bắp, nước, combo... kèm giá VND):\n" + foods);

        var promos = await SafeBuildAsync("khuyến mãi", BuildPromotionCatalogAsync);
        if (!string.IsNullOrWhiteSpace(promos))
            sections.Add("KHUYẾN MÃI ĐANG ÁP DỤNG:\n" + promos);

        var cinemas = await SafeBuildAsync("rạp", BuildCinemaCatalogAsync);
        if (!string.IsNullOrWhiteSpace(cinemas))
            sections.Add("HỆ THỐNG RẠP:\n" + cinemas);

        return string.Join("\n\n", sections);
    }

    /// <summary>Chạy một hàm dựng ngữ cảnh và nuốt lỗi để một phần dữ liệu hỏng không làm chết cả chatbot.</summary>
    private async Task<string> SafeBuildAsync(string label, Func<Task<string>> builder)
    {
        try { return await builder(); }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không dựng được ngữ cảnh {Label} cho chatbot.", label);
            return string.Empty;
        }
    }

    /// <summary>Danh mục phim đang chiếu và sắp chiếu, kèm slug để tạo liên kết chi tiết.</summary>
    private async Task<string> BuildMovieCatalogAsync()
    {
        var nowShowing = await _movieService.GetNowShowingMoviesAsync();
        var comingSoon = await _movieService.GetComingSoonMoviesAsync();

        var lines = new List<string>();

        static void Append(List<string> acc, string status, IEnumerable<MovieDTO> movies)
        {
            foreach (var m in movies)
            {
                if (string.IsNullOrWhiteSpace(m.Slug)) continue;
                var genres = m.GenreNames.Count > 0 ? string.Join(", ", m.GenreNames) : "chưa cập nhật";
                var release = m.ReleaseDate?.ToString("dd/MM/yyyy") ?? "chưa rõ";
                acc.Add($"- {m.Title} | {status} | thể loại: {genres} | phân loại: {m.AgeRating ?? "N/A"} | khởi chiếu: {release} | slug: {m.Slug}");
            }
        }

        Append(lines, "Đang chiếu", nowShowing);
        Append(lines, "Sắp chiếu", comingSoon);

        return string.Join("\n", lines);
    }

    /// <summary>Danh mục đồ ăn & nước uống kèm giá và tình trạng còn hàng.</summary>
    private async Task<string> BuildConcessionCatalogAsync()
    {
        var items = await _unitOfWork.FoodBeverages.GetAllAsync(orderBy: q => q.OrderBy(f => f.Name));
        var lines = items
            .Select(f =>
            {
                var stock = string.Equals(f.StockStatus, "OutOfStock", StringComparison.OrdinalIgnoreCase) ? "tạm hết hàng" : "còn hàng";
                return $"- {f.Name} | giá: {f.Price:#,##0} đ | {stock}";
            })
            .ToList();
        return string.Join("\n", lines);
    }

    /// <summary>Danh mục khuyến mãi còn hiệu lực (đang active và trong thời gian áp dụng).</summary>
    private async Task<string> BuildPromotionCatalogAsync()
    {
        var now = DateTime.Now;
        var promos = await _unitOfWork.Promotions.GetAllAsync(
            predicate: p => p.ValidFrom <= now && p.ValidTo >= now,
            orderBy: q => q.OrderBy(p => p.ValidTo));

        var lines = promos
            .Where(p => string.IsNullOrEmpty(p.Status) || string.Equals(p.Status, "Active", StringComparison.OrdinalIgnoreCase))
            .Select(p =>
            {
                var discount = string.Equals(p.DiscountType, "Percentage", StringComparison.OrdinalIgnoreCase)
                    ? $"giảm {p.DiscountAmount:#,##0}%"
                    : $"giảm {p.DiscountAmount:#,##0} đ";
                var cond = p.MinOrderValue.HasValue && p.MinOrderValue.Value > 0 ? $" (đơn từ {p.MinOrderValue.Value:#,##0} đ)" : "";
                return $"- Mã {p.Code} | {discount}{cond} | HSD tới {p.ValidTo:dd/MM/yyyy}";
            })
            .ToList();
        return string.Join("\n", lines);
    }

    /// <summary>Danh mục rạp đang hoạt động kèm địa chỉ và hotline.</summary>
    private async Task<string> BuildCinemaCatalogAsync()
    {
        var cinemas = await _unitOfWork.Cinemas.GetAllAsync(orderBy: q => q.OrderBy(c => c.Name));
        var lines = cinemas
            .Where(c => string.IsNullOrEmpty(c.Status) || !string.Equals(c.Status, "Inactive", StringComparison.OrdinalIgnoreCase))
            .Select(c => $"- {c.Name} | {c.Address ?? "chưa cập nhật địa chỉ"} | hotline: {c.Hotline ?? "chưa có"}")
            .ToList();
        return string.Join("\n", lines);
    }

    /// <summary>
    /// Gửi prompt người dùng tới Google Gemini dùng model và URL API cấu hình.
    /// </summary>
    private async Task<string> CallGoogleAiAsync(string message, string apiKey, string knowledge)
    {
        var model = _config["AI:Google:Model"]?.Trim() ?? "gemini-2.5-flash";
        var apiUrl = _config["AI:Google:ApiUrl"]?.Trim() ?? $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";

        var client = _httpFactory.CreateClient();
        apiUrl = AppendApiKeyQuery(apiUrl, apiKey);
        client.DefaultRequestHeaders.Add("x-goog-api-key", apiKey);

        var systemInstruction =
            "Bạn là trợ lý ảo của CineStar - một hệ thống đặt vé xem phim trực tuyến. Vai trò của bạn là hỗ trợ khách hàng và người dùng về MỌI khía cạnh của hệ thống: tìm và giới thiệu phim, lịch chiếu, đặt vé (online và tại quầy), chọn ghế, giá vé, đồ ăn & nước uống (bắp rang, nước ngọt, combo...), khuyến mãi và mã giảm giá, điểm thưởng thành viên, thông tin rạp, tài khoản, thanh toán, check-in vé và các quy định xem phim.\n" +
            "Phong cách: trả lời bằng tiếng Việt, ngắn gọn, thân thiện, dễ hiểu như đang hướng dẫn một khách hàng. Ưu tiên trả lời thẳng vào câu hỏi trước, rồi mới bổ sung nếu cần.\n" +
            "QUAN TRỌNG - dùng dữ liệu thật: Bên dưới là dữ liệu thực tế hiện có của hệ thống. Khi khách hỏi về giá đồ ăn/nước, khuyến mãi, phim, hay rạp, hãy TRẢ LỜI DỰA TRÊN dữ liệu này, nêu con số/thông tin cụ thể (ví dụ giá bắp rang bơ bao nhiêu tiền). Nếu thông tin khách hỏi không có trong dữ liệu, hãy nói thật là hiện chưa có/chưa cập nhật thay vì bịa ra.\n" +
            "Nếu câu hỏi hoàn toàn không liên quan tới CineStar hay việc xem phim (ví dụ toán học, chính trị, code...), hãy lịch sự từ chối và nói rằng bạn chỉ hỗ trợ về hệ thống CineStar.";

        if (!string.IsNullOrWhiteSpace(knowledge))
        {
            systemInstruction +=
                "\n\n===== DỮ LIỆU HỆ THỐNG CINESTAR =====\n" + knowledge +
                "\n===== HẾT DỮ LIỆU =====\n" +
                "\nQUY TẮC VỀ LIÊN KẾT PHIM: Khi bạn giới thiệu, gợi ý hoặc nhắc tới một phim CÓ trong dữ liệu trên, hãy luôn viết tên phim dưới dạng liên kết Markdown đúng định dạng [Tên phim](/Movies/Details/slug) với slug lấy chính xác từ dòng tương ứng. Tuyệt đối không tự bịa slug hay tạo liên kết cho phim không có trong dữ liệu. Khi khách muốn tìm phim theo thể loại/độ tuổi/thời điểm, hãy lọc từ dữ liệu và liệt kê mỗi phim một dòng gạch đầu dòng kèm liên kết. Chỉ tạo liên kết cho phim, không tạo liên kết cho đồ ăn hay khuyến mãi.";
        }

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
                maxOutputTokens = 800
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
