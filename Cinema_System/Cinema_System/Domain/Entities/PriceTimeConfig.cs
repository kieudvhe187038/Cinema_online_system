using System;
using System.Collections.Generic;

namespace Cinema_System.Domain.Entities;

public partial class PriceTimeConfig
{
    public Guid Id { get; set; }

    public string TimeCondition { get; set; } = null!;

    public int? DayOfWeek { get; set; }

    public DateOnly? SpecificDate { get; set; }

    public TimeOnly? StartTime { get; set; }

    public TimeOnly? EndTime { get; set; }

    public decimal TimeSurcharge { get; set; }

    public string? RuleGroup { get; set; }

    public int Priority { get; set; }

    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public string? Status { get; set; }
}
