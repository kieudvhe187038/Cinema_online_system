using System;
using System.Collections.Generic;

namespace Cinema_System.Domain.Entities;

public partial class Vat
{
    public Guid Id { get; set; }

    public decimal VatRate { get; set; }

    public string? Description { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
