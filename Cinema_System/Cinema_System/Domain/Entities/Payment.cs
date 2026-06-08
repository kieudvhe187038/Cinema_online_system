using System;
using System.Collections.Generic;

namespace Cinema_System.Domain.Entities;

public partial class Payment
{
    public Guid Id { get; set; }

    public Guid BookingId { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public string PaymentSource { get; set; } = null!;

    public string? TransactionRef { get; set; }

    public string? Status { get; set; }

    public DateTime? PaidAt { get; set; }

    public decimal Amount { get; set; }

    public decimal? CashReceived { get; set; }

    public decimal? ChangeAmount { get; set; }

    public virtual Booking Booking { get; set; } = null!;
}
