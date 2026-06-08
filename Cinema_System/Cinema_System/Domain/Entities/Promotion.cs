using System;
using System.Collections.Generic;

namespace Cinema_System.Domain.Entities;

public partial class Promotion
{
    public Guid Id { get; set; }

    public string Code { get; set; } = null!;

    public decimal DiscountAmount { get; set; }

    public string DiscountType { get; set; } = null!;

    public decimal? MinOrderValue { get; set; }

    public decimal? MaxDiscountAmount { get; set; }

    public string? ApplicableTarget { get; set; }

    public DateTime ValidFrom { get; set; }

    public DateTime ValidTo { get; set; }

    public int? UsageLimit { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual ICollection<ShowtimeIncident> ShowtimeIncidents { get; set; } = new List<ShowtimeIncident>();
}
