using System;
using System.Collections.Generic;

namespace Cinema_System.Domain.Entities;

public partial class Review
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid MovieId { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Movie Movie { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
