using System;
using System.Collections.Generic;

namespace Cinema_System.Domain.Entities;

public partial class SeatHold
{
    public Guid Id { get; set; }

    public Guid ShowtimeId { get; set; }

    public Guid SeatId { get; set; }

    public Guid? UserId { get; set; }

    public DateTime? HeldAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public string? Status { get; set; }

    public virtual Seat Seat { get; set; } = null!;

    public virtual Showtime Showtime { get; set; } = null!;

    public virtual User? User { get; set; }
}
