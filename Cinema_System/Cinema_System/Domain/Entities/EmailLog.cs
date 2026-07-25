using System;
using System.Collections.Generic;

namespace Cinema_System.Domain.Entities;

public partial class EmailLog
{
    public Guid Id { get; set; }

    public Guid? BookingId { get; set; }

    public string RecipientEmail { get; set; } = null!;

    public string Subject { get; set; } = null!;

    public string? BodyContent { get; set; }

    public string? Status { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime? SentAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Booking? Booking { get; set; }
}
