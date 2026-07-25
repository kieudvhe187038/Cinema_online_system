using System;
using System.Collections.Generic;

namespace Cinema_System.Domain.Entities;

public partial class Showtime
{
    public Guid Id { get; set; }

    public Guid MovieId { get; set; }

    public Guid RoomId { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual Movie Movie { get; set; } = null!;

    public virtual Room Room { get; set; } = null!;

    public virtual ICollection<SeatHold> SeatHolds { get; set; } = new List<SeatHold>();

    public virtual ICollection<ShowtimeIncident> ShowtimeIncidents { get; set; } = new List<ShowtimeIncident>();

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
