using System;
using System.Collections.Generic;

namespace Cinema_System.Domain.Entities;

public partial class PriceRoomTypeConfig
{
    public Guid Id { get; set; }

    public Guid RoomTypeId { get; set; }

    public decimal TypeSurcharge { get; set; }

    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public string? Status { get; set; }

    public virtual RoomType RoomType { get; set; } = null!;
}
