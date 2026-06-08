using System;
using System.Collections.Generic;

namespace Cinema_System.Domain.Entities;

public partial class FoodBeverage
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public decimal Price { get; set; }

    public string? StockStatus { get; set; }

    public virtual ICollection<BookingFood> BookingFoods { get; set; } = new List<BookingFood>();
}
