using System;
using System.Collections.Generic;

namespace Cinema_System.Domain.Entities;

public partial class SystemConfig
{
    public string ConfigKey { get; set; } = null!;

    public string? ConfigValue { get; set; }

    public string? Description { get; set; }

    public Guid? UpdatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
