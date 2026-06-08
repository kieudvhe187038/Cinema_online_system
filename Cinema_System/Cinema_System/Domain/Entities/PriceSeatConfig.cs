using System;
using System.Collections.Generic;

namespace Cinema_System.Domain.Entities;

public partial class PriceSeatConfig
{
    public Guid Id { get; set; }

    public Guid SeatTypeId { get; set; }

    public decimal SeatSurcharge { get; set; }

    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public string? Status { get; set; }

    public virtual SeatType SeatType { get; set; } = null!;
}
