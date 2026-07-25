using System;
using System.Collections.Generic;

namespace Cinema_System.Domain.Entities;

public partial class Seat
{
    public Guid Id { get; set; }

    public Guid RoomId { get; set; }

    public Guid SeatTypeId { get; set; }

    public int RowNumber { get; set; }

    public int SeatNumber { get; set; }

    public string? Status { get; set; }

    public virtual Room Room { get; set; } = null!;

    public virtual ICollection<SeatHold> SeatHolds { get; set; } = new List<SeatHold>();

    public virtual SeatType SeatType { get; set; } = null!;

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
