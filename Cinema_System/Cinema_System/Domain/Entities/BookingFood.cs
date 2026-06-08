using System;
using System.Collections.Generic;

namespace Cinema_System.Domain.Entities;

public partial class BookingFood
{
    public Guid Id { get; set; }

    public Guid BookingId { get; set; }

    public Guid FbId { get; set; }

    public int Quantity { get; set; }

    public decimal PriceAtBooking { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual FoodBeverage Fb { get; set; } = null!;
}
