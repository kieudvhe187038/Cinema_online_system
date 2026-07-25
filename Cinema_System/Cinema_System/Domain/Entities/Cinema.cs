using System;
using System.Collections.Generic;

namespace Cinema_System.Domain.Entities;

public partial class Cinema
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Address { get; set; }

    public string? Hotline { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
}
