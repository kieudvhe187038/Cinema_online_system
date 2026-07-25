using System;
using System.Collections.Generic;

namespace Cinema_System.Domain.Entities;

public partial class ShowtimeIncident
{
    public Guid Id { get; set; }

    public Guid? ShowtimeId { get; set; }

    public string? Description { get; set; }

    public decimal? RefundPointsRate { get; set; }

    public Guid? CompensationPromo { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Promotion? CompensationPromoNavigation { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual Showtime? Showtime { get; set; }
}
