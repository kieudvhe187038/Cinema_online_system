using System;
using System.Collections.Generic;

namespace Cinema_System.Domain.Entities;

public partial class Ticket
{
    public Guid Id { get; set; }

    public Guid BookingId { get; set; }

    public Guid ShowtimeId { get; set; }

    public Guid SeatId { get; set; }

    public decimal PriceAtBooking { get; set; }

    public string? QrCode { get; set; }

    public string? Status { get; set; }

    public Guid? ScanBy { get; set; }

    public DateTime? ScannedAt { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual User? ScanByNavigation { get; set; }

    public virtual Seat Seat { get; set; } = null!;

    public virtual Showtime Showtime { get; set; } = null!;
}
