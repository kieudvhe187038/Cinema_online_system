using System;
using System.Collections.Generic;

namespace Cinema_System.Domain.Entities;

public partial class ChatbotLog
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public string SessionId { get; set; } = null!;

    public string UserMessage { get; set; } = null!;

    public string BotResponse { get; set; } = null!;

    public string? IntentDetected { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? User { get; set; }
}
