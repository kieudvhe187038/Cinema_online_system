using System;
using System.Collections.Generic;

namespace Cinema_System.Domain.Entities;

public partial class SeatType
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public int Capacity { get; set; }

    public int ColumnSpan { get; set; }

    public virtual ICollection<PriceSeatConfig> PriceSeatConfigs { get; set; } = new List<PriceSeatConfig>();

    public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();
}
