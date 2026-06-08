using System;
using System.Collections.Generic;

namespace Cinema_System.Domain.Entities;

public partial class Booking
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public Guid? StaffId { get; set; }

    public Guid ShowtimeId { get; set; }

    public Guid? PromotionId { get; set; }

    public Guid? VatId { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal? DiscountAmount { get; set; }

    public decimal? VatAmount { get; set; }

    public decimal FinalAmount { get; set; }

    public string? PaymentStatus { get; set; }

    public string? BookingType { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? QrCode { get; set; }

    public virtual ICollection<BookingFood> BookingFoods { get; set; } = new List<BookingFood>();

    public virtual ICollection<EmailLog> EmailLogs { get; set; } = new List<EmailLog>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Promotion? Promotion { get; set; }

    public virtual ICollection<RewardPointHistory> RewardPointHistories { get; set; } = new List<RewardPointHistory>();

    public virtual Showtime Showtime { get; set; } = null!;

    public virtual User? Staff { get; set; }

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    public virtual User? User { get; set; }

    public virtual Vat? Vat { get; set; }
}
