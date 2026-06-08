using System;
using System.Collections.Generic;

namespace Cinema_System.Domain.Entities;

public partial class Genre
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Movie> Movies { get; set; } = new List<Movie>();
}
