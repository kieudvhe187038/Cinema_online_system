using System;
using System.Collections.Generic;

namespace Cinema_System.Domain.Entities;

public partial class Room
{
    public Guid Id { get; set; }

    public Guid CinemaId { get; set; }

    public string Name { get; set; } = null!;

    public Guid RoomTypeId { get; set; }

    public int? TotalSeats { get; set; }

    public int? TotalColumns { get; set; }

    public int? TotalRow { get; set; }

    public string? Status { get; set; }

    public virtual Cinema Cinema { get; set; } = null!;

    public virtual RoomType RoomType { get; set; } = null!;

    public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();

    public virtual ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();
}
