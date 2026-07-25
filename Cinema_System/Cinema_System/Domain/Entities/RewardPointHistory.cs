using System;
using System.Collections.Generic;

namespace Cinema_System.Domain.Entities;

public partial class RewardPointHistory
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid? BookingId { get; set; }

    public int PointsChanged { get; set; }

    public string ActionType { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Booking? Booking { get; set; }

    public virtual User User { get; set; } = null!;
}
