using System;
using System.Collections.Generic;

namespace Cinema_System.Domain.Entities;

public partial class RoomType
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<PriceRoomTypeConfig> PriceRoomTypeConfigs { get; set; } = new List<PriceRoomTypeConfig>();

    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
}
