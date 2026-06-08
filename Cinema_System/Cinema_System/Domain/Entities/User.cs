using System;
using System.Collections.Generic;

namespace Cinema_System.Domain.Entities;

public partial class User
{
    public Guid Id { get; set; }

    public Guid RoleId { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }

    public DateOnly DateOfBirth { get; set; }

    public string? PasswordHash { get; set; }

    public string? AvatarUrl { get; set; }

    public int? RewardPoints { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual ICollection<Booking> BookingStaffs { get; set; } = new List<Booking>();

    public virtual ICollection<Booking> BookingUsers { get; set; } = new List<Booking>();

    public virtual ICollection<ChatbotLog> ChatbotLogs { get; set; } = new List<ChatbotLog>();

    public virtual ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual ICollection<RewardPointHistory> RewardPointHistories { get; set; } = new List<RewardPointHistory>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<SeatHold> SeatHolds { get; set; } = new List<SeatHold>();

    public virtual ICollection<ShowtimeIncident> ShowtimeIncidents { get; set; } = new List<ShowtimeIncident>();

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
