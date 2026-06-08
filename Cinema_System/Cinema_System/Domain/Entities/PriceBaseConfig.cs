using System;
using System.Collections.Generic;

namespace Cinema_System.Domain.Entities;

public partial class PriceBaseConfig
{
    public Guid Id { get; set; }

    public Guid? MovieId { get; set; }

    public decimal BasePrice { get; set; }

    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public string? Status { get; set; }

    public virtual Movie? Movie { get; set; }
}
