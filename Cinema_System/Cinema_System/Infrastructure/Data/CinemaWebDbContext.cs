using System;
using System.Collections.Generic;
using Cinema_System.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cinema_System.Infrastructure.Data;

public partial class CinemaWebDbContext : DbContext
{
    public CinemaWebDbContext()
    {
    }

    public CinemaWebDbContext(DbContextOptions<CinemaWebDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Booking> Bookings { get; set; }

    public virtual DbSet<BookingFood> BookingFoods { get; set; }

    public virtual DbSet<ChatbotLog> ChatbotLogs { get; set; }

    public virtual DbSet<Cinema> Cinemas { get; set; }

    public virtual DbSet<EmailLog> EmailLogs { get; set; }

    public virtual DbSet<FoodBeverage> FoodBeverages { get; set; }

    public virtual DbSet<Genre> Genres { get; set; }

    public virtual DbSet<Movie> Movies { get; set; }

    public virtual DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<PriceBaseConfig> PriceBaseConfigs { get; set; }

    public virtual DbSet<PriceRoomTypeConfig> PriceRoomTypeConfigs { get; set; }

    public virtual DbSet<PriceSeatConfig> PriceSeatConfigs { get; set; }

    public virtual DbSet<PriceTimeConfig> PriceTimeConfigs { get; set; }

    public virtual DbSet<Promotion> Promotions { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<RewardPointHistory> RewardPointHistories { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Room> Rooms { get; set; }

    public virtual DbSet<RoomType> RoomTypes { get; set; }

    public virtual DbSet<Seat> Seats { get; set; }

    public virtual DbSet<SeatHold> SeatHolds { get; set; }

    public virtual DbSet<SeatType> SeatTypes { get; set; }

    public virtual DbSet<Showtime> Showtimes { get; set; }

    public virtual DbSet<ShowtimeIncident> ShowtimeIncidents { get; set; }

    public virtual DbSet<SystemConfig> SystemConfigs { get; set; }

    public virtual DbSet<Ticket> Tickets { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Vat> Vats { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("Audit_Logs");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.Action)
                .HasMaxLength(100)
                .HasColumnName("action");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.IpAddress)
                .HasMaxLength(45)
                .IsUnicode(false)
                .HasColumnName("ip_address");
            entity.Property(e => e.NewValue).HasColumnName("new_value");
            entity.Property(e => e.OldValue).HasColumnName("old_value");
            entity.Property(e => e.RecordId).HasColumnName("record_id");
            entity.Property(e => e.TableName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("table_name");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AuditLogs_Users");
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasIndex(e => e.QrCode, "UX_Bookings_qrcode")
                .IsUnique()
                .HasFilter("([qr_code] IS NOT NULL)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.BookingType)
                .HasMaxLength(50)
                .HasDefaultValue("Online")
                .HasColumnName("booking_type");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.DiscountAmount)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("discount_amount");
            entity.Property(e => e.FinalAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("final_amount");
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(50)
                .HasDefaultValue("Pending")
                .HasColumnName("payment_status");
            entity.Property(e => e.PromotionId).HasColumnName("promotion_id");
            entity.Property(e => e.QrCode)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("qr_code");
            entity.Property(e => e.ShowtimeId).HasColumnName("showtime_id");
            entity.Property(e => e.StaffId).HasColumnName("staff_id");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("total_amount");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.VatAmount)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("vat_amount");
            entity.Property(e => e.VatId).HasColumnName("vat_id");

            entity.HasOne(d => d.Promotion).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.PromotionId)
                .HasConstraintName("FK_Bookings_Promotions");

            entity.HasOne(d => d.Showtime).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.ShowtimeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bookings_Showtimes");

            entity.HasOne(d => d.Staff).WithMany(p => p.BookingStaffs)
                .HasForeignKey(d => d.StaffId)
                .HasConstraintName("FK_Bookings_Staff");

            entity.HasOne(d => d.User).WithMany(p => p.BookingUsers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Bookings_Users");

            entity.HasOne(d => d.Vat).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.VatId)
                .HasConstraintName("FK_Bookings_VAT");
        });

        modelBuilder.Entity<BookingFood>(entity =>
        {
            entity.ToTable("Booking_Foods");

            entity.HasIndex(e => new { e.BookingId, e.FbId }, "UK_BookingFoods_Order").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.FbId).HasColumnName("fb_id");
            entity.Property(e => e.PriceAtBooking)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("price_at_booking");
            entity.Property(e => e.Quantity).HasColumnName("quantity");

            entity.HasOne(d => d.Booking).WithMany(p => p.BookingFoods)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BookingFoods_Bookings");

            entity.HasOne(d => d.Fb).WithMany(p => p.BookingFoods)
                .HasForeignKey(d => d.FbId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BookingFoods_FB");
        });

        modelBuilder.Entity<ChatbotLog>(entity =>
        {
            entity.ToTable("Chatbot_Logs");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.BotResponse).HasColumnName("bot_response");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.IntentDetected)
                .HasMaxLength(100)
                .HasColumnName("intent_detected");
            entity.Property(e => e.SessionId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("session_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.UserMessage).HasColumnName("user_message");

            entity.HasOne(d => d.User).WithMany(p => p.ChatbotLogs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_ChatbotLogs_Users");
        });

        modelBuilder.Entity<Cinema>(entity =>
        {
            entity.HasIndex(e => e.Name, "UK_Cinemas_name").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.Address)
                .HasMaxLength(500)
                .HasColumnName("address");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Hotline)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("hotline");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Active")
                .HasColumnName("status");
        });

        modelBuilder.Entity<EmailLog>(entity =>
        {
            entity.ToTable("Email_Logs");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.BodyContent).HasColumnName("body_content");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message");
            entity.Property(e => e.RecipientEmail)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("recipient_email");
            entity.Property(e => e.SentAt)
                .HasColumnType("datetime")
                .HasColumnName("sent_at");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Pending")
                .HasColumnName("status");
            entity.Property(e => e.Subject)
                .HasMaxLength(255)
                .HasColumnName("subject");

            entity.HasOne(d => d.Booking).WithMany(p => p.EmailLogs)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("FK_EmailLogs_Bookings");
        });

        modelBuilder.Entity<FoodBeverage>(entity =>
        {
            entity.ToTable("Food_Beverages");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("image_url");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("price");
            entity.Property(e => e.StockStatus)
                .HasMaxLength(50)
                .HasDefaultValue("In Stock")
                .HasColumnName("stock_status");
        });

        modelBuilder.Entity<Genre>(entity =>
        {
            entity.HasIndex(e => e.Name, "UK_Genres_name").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Movie>(entity =>
        {
            entity.HasIndex(e => e.Slug, "UK_Movies_slug").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.AgeRating)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("age_rating");
            entity.Property(e => e.BannerUrl)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("banner_url");
            entity.Property(e => e.CastMembers).HasColumnName("cast_members");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Director)
                .HasMaxLength(255)
                .HasColumnName("director");
            entity.Property(e => e.DurationMinutes).HasColumnName("duration_minutes");
            entity.Property(e => e.Language)
                .HasMaxLength(100)
                .HasColumnName("language");
            entity.Property(e => e.PosterUrl)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("poster_url");
            entity.Property(e => e.ReleaseDate).HasColumnName("release_date");
            entity.Property(e => e.Slug)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("slug");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.Subtitle)
                .HasMaxLength(100)
                .HasColumnName("subtitle");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.TrailerUrl)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("trailer_url");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasMany(d => d.Genres).WithMany(p => p.Movies)
                .UsingEntity<Dictionary<string, object>>(
                    "MovieGenre",
                    r => r.HasOne<Genre>().WithMany()
                        .HasForeignKey("GenreId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_MovieGenres_Genres"),
                    l => l.HasOne<Movie>().WithMany()
                        .HasForeignKey("MovieId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_MovieGenres_Movies"),
                    j =>
                    {
                        j.HasKey("MovieId", "GenreId");
                        j.ToTable("Movie_Genres");
                        j.IndexerProperty<Guid>("MovieId").HasColumnName("movie_id");
                        j.IndexerProperty<Guid>("GenreId").HasColumnName("genre_id");
                    });
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.ToTable("Password_Reset_Tokens");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiryAt)
                .HasColumnType("datetime")
                .HasColumnName("expiry_at");
            entity.Property(e => e.IsUsed)
                .HasDefaultValue(false)
                .HasColumnName("is_used");
            entity.Property(e => e.TokenHash)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("token_hash");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.PasswordResetTokens)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PasswordReset_Users");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasIndex(e => e.TransactionRef, "UX_Payments_ref")
                .IsUnique()
                .HasFilter("([transaction_ref] IS NOT NULL)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.CashReceived)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("cash_received");
            entity.Property(e => e.ChangeAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("change_amount");
            entity.Property(e => e.PaidAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("paid_at");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(100)
                .HasColumnName("payment_method");
            entity.Property(e => e.PaymentSource)
                .HasMaxLength(100)
                .HasColumnName("payment_source");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Success")
                .HasColumnName("status");
            entity.Property(e => e.TransactionRef)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("transaction_ref");

            entity.HasOne(d => d.Booking).WithMany(p => p.Payments)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payments_Bookings");
        });

        modelBuilder.Entity<PriceBaseConfig>(entity =>
        {
            entity.ToTable("Price_Base_Configs");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.BasePrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("base_price");
            entity.Property(e => e.EffectiveFrom)
                .HasColumnType("datetime")
                .HasColumnName("effective_from");
            entity.Property(e => e.EffectiveTo)
                .HasColumnType("datetime")
                .HasColumnName("effective_to");
            entity.Property(e => e.MovieId).HasColumnName("movie_id");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Active")
                .HasColumnName("status");

            entity.HasOne(d => d.Movie).WithMany(p => p.PriceBaseConfigs)
                .HasForeignKey(d => d.MovieId)
                .HasConstraintName("FK_PriceBase_Movies");
        });

        modelBuilder.Entity<PriceRoomTypeConfig>(entity =>
        {
            entity.ToTable("Price_Room_Type_Configs");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.EffectiveFrom)
                .HasColumnType("datetime")
                .HasColumnName("effective_from");
            entity.Property(e => e.EffectiveTo)
                .HasColumnType("datetime")
                .HasColumnName("effective_to");
            entity.Property(e => e.RoomTypeId).HasColumnName("room_type_id");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Active")
                .HasColumnName("status");
            entity.Property(e => e.TypeSurcharge)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("type_surcharge");

            entity.HasOne(d => d.RoomType).WithMany(p => p.PriceRoomTypeConfigs)
                .HasForeignKey(d => d.RoomTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PriceRoom_RoomTypes");
        });

        modelBuilder.Entity<PriceSeatConfig>(entity =>
        {
            entity.ToTable("Price_Seat_Configs");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.EffectiveFrom)
                .HasColumnType("datetime")
                .HasColumnName("effective_from");
            entity.Property(e => e.EffectiveTo)
                .HasColumnType("datetime")
                .HasColumnName("effective_to");
            entity.Property(e => e.SeatSurcharge)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("seat_surcharge");
            entity.Property(e => e.SeatTypeId).HasColumnName("seat_type_id");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Active")
                .HasColumnName("status");

            entity.HasOne(d => d.SeatType).WithMany(p => p.PriceSeatConfigs)
                .HasForeignKey(d => d.SeatTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PriceSeat_SeatTypes");
        });

        modelBuilder.Entity<PriceTimeConfig>(entity =>
        {
            entity.ToTable("Price_Time_Configs");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.DayOfWeek).HasColumnName("day_of_week");
            entity.Property(e => e.EffectiveFrom)
                .HasColumnType("datetime")
                .HasColumnName("effective_from");
            entity.Property(e => e.EffectiveTo)
                .HasColumnType("datetime")
                .HasColumnName("effective_to");
            entity.Property(e => e.EndTime).HasColumnName("end_time");
            entity.Property(e => e.Priority).HasColumnName("priority");
            entity.Property(e => e.RuleGroup)
                .HasMaxLength(50)
                .HasColumnName("rule_group");
            entity.Property(e => e.SpecificDate).HasColumnName("specific_date");
            entity.Property(e => e.StartTime).HasColumnName("start_time");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Active")
                .HasColumnName("status");
            entity.Property(e => e.TimeCondition)
                .HasMaxLength(100)
                .HasColumnName("time_condition");
            entity.Property(e => e.TimeSurcharge)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("time_surcharge");
        });

        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.HasIndex(e => e.Code, "UK_Promotions_code").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.ApplicableTarget)
                .HasMaxLength(50)
                .HasDefaultValue("All")
                .HasColumnName("applicable_target");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("code");
            entity.Property(e => e.DiscountAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("discount_amount");
            entity.Property(e => e.DiscountType)
                .HasMaxLength(50)
                .HasColumnName("discount_type");
            entity.Property(e => e.MaxDiscountAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("max_discount_amount");
            entity.Property(e => e.MinOrderValue)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("min_order_value");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Active")
                .HasColumnName("status");
            entity.Property(e => e.UsageLimit).HasColumnName("usage_limit");
            entity.Property(e => e.ValidFrom)
                .HasColumnType("datetime")
                .HasColumnName("valid_from");
            entity.Property(e => e.ValidTo)
                .HasColumnType("datetime")
                .HasColumnName("valid_to");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.MovieId }, "UK_Reviews_User_Movie").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.Comment).HasColumnName("comment");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.MovieId).HasColumnName("movie_id");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Approved")
                .HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Movie).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.MovieId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reviews_Movies");

            entity.HasOne(d => d.User).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reviews_Users");
        });

        modelBuilder.Entity<RewardPointHistory>(entity =>
        {
            entity.ToTable("Reward_Point_History");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.ActionType)
                .HasMaxLength(100)
                .HasColumnName("action_type");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.PointsChanged).HasColumnName("points_changed");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Booking).WithMany(p => p.RewardPointHistories)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("FK_RewardPoint_Bookings");

            entity.HasOne(d => d.User).WithMany(p => p.RewardPointHistories)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RewardPoint_Users");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(e => e.Name, "UK_Roles_name").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasIndex(e => new { e.CinemaId, e.Name }, "UK_Rooms_Cinema_Name").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.CinemaId).HasColumnName("cinema_id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.RoomTypeId).HasColumnName("room_type_id");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Active")
                .HasColumnName("status");
            entity.Property(e => e.TotalColumns).HasColumnName("total_columns");
            entity.Property(e => e.TotalRow).HasColumnName("total_row");
            entity.Property(e => e.TotalSeats).HasColumnName("total_seats");

            entity.HasOne(d => d.Cinema).WithMany(p => p.Rooms)
                .HasForeignKey(d => d.CinemaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Rooms_Cinemas");

            entity.HasOne(d => d.RoomType).WithMany(p => p.Rooms)
                .HasForeignKey(d => d.RoomTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Rooms_RoomTypes");
        });

        modelBuilder.Entity<RoomType>(entity =>
        {
            entity.ToTable("Room_Types");

            entity.HasIndex(e => e.Name, "UK_RoomTypes_name").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Seat>(entity =>
        {
            entity.HasIndex(e => new { e.RoomId, e.RowNumber, e.SeatNumber }, "UK_Seats_Room_Row_Number").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.RoomId).HasColumnName("room_id");
            entity.Property(e => e.RowNumber).HasColumnName("row_number");
            entity.Property(e => e.SeatNumber).HasColumnName("seat_number");
            entity.Property(e => e.SeatTypeId).HasColumnName("seat_type_id");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Available")
                .HasColumnName("status");

            entity.HasOne(d => d.Room).WithMany(p => p.Seats)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Seats_Rooms");

            entity.HasOne(d => d.SeatType).WithMany(p => p.Seats)
                .HasForeignKey(d => d.SeatTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Seats_SeatTypes");
        });

        modelBuilder.Entity<SeatHold>(entity =>
        {
            entity.ToTable("Seat_Holds");

            entity.HasIndex(e => new { e.ShowtimeId, e.SeatId }, "UX_SeatHolds_Active")
                .IsUnique()
                .HasFilter("([status]='Holding')");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.ExpiresAt)
                .HasColumnType("datetime")
                .HasColumnName("expires_at");
            entity.Property(e => e.HeldAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("held_at");
            entity.Property(e => e.SeatId).HasColumnName("seat_id");
            entity.Property(e => e.ShowtimeId).HasColumnName("showtime_id");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Holding")
                .HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Seat).WithMany(p => p.SeatHolds)
                .HasForeignKey(d => d.SeatId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SeatHolds_Seats");

            entity.HasOne(d => d.Showtime).WithMany(p => p.SeatHolds)
                .HasForeignKey(d => d.ShowtimeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SeatHolds_Showtimes");

            entity.HasOne(d => d.User).WithMany(p => p.SeatHolds)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_SeatHolds_Users");
        });

        modelBuilder.Entity<SeatType>(entity =>
        {
            entity.ToTable("Seat_Types");

            entity.HasIndex(e => e.Name, "UK_SeatTypes_name").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.Capacity)
                .HasDefaultValue(1)
                .HasColumnName("capacity");
            entity.Property(e => e.ColumnSpan)
                .HasDefaultValue(1)
                .HasColumnName("column_span");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Showtime>(entity =>
        {
            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.EndTime)
                .HasColumnType("datetime")
                .HasColumnName("end_time");
            entity.Property(e => e.MovieId).HasColumnName("movie_id");
            entity.Property(e => e.RoomId).HasColumnName("room_id");
            entity.Property(e => e.StartTime)
                .HasColumnType("datetime")
                .HasColumnName("start_time");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Scheduled")
                .HasColumnName("status");

            entity.HasOne(d => d.Movie).WithMany(p => p.Showtimes)
                .HasForeignKey(d => d.MovieId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Showtimes_Movies");

            entity.HasOne(d => d.Room).WithMany(p => p.Showtimes)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Showtimes_Rooms");
        });

        modelBuilder.Entity<ShowtimeIncident>(entity =>
        {
            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.CompensationPromo).HasColumnName("compensation_promo");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.RefundPointsRate)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("refund_points_rate");
            entity.Property(e => e.ShowtimeId).HasColumnName("showtime_id");

            entity.HasOne(d => d.CompensationPromoNavigation).WithMany(p => p.ShowtimeIncidents)
                .HasForeignKey(d => d.CompensationPromo)
                .HasConstraintName("FK_Incidents_Promo");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.ShowtimeIncidents)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_Incidents_CreatedBy");

            entity.HasOne(d => d.Showtime).WithMany(p => p.ShowtimeIncidents)
                .HasForeignKey(d => d.ShowtimeId)
                .HasConstraintName("FK_Incidents_Showtimes");
        });

        modelBuilder.Entity<SystemConfig>(entity =>
        {
            entity.HasKey(e => e.ConfigKey);

            entity.ToTable("SystemConfig");

            entity.Property(e => e.ConfigKey)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("config_key");
            entity.Property(e => e.ConfigValue).HasColumnName("config_value");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.HasIndex(e => new { e.BookingId, e.SeatId }, "UK_Tickets_Booking_Seat").IsUnique();

            entity.HasIndex(e => new { e.ShowtimeId, e.SeatId }, "UX_Tickets_Showtime_Seat")
                .IsUnique()
                .HasFilter("([status]<>'Cancelled')");

            entity.HasIndex(e => e.QrCode, "UX_Tickets_qrcode")
                .IsUnique()
                .HasFilter("([qr_code] IS NOT NULL)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.PriceAtBooking)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("price_at_booking");
            entity.Property(e => e.QrCode)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("qr_code");
            entity.Property(e => e.ScanBy).HasColumnName("scan_by");
            entity.Property(e => e.ScannedAt)
                .HasColumnType("datetime")
                .HasColumnName("scanned_at");
            entity.Property(e => e.SeatId).HasColumnName("seat_id");
            entity.Property(e => e.ShowtimeId).HasColumnName("showtime_id");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Booked")
                .HasColumnName("status");

            entity.HasOne(d => d.Booking).WithMany(p => p.Tickets)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tickets_Bookings");

            entity.HasOne(d => d.ScanByNavigation).WithMany(p => p.Tickets)
                .HasForeignKey(d => d.ScanBy)
                .HasConstraintName("FK_Tickets_ScanBy");

            entity.HasOne(d => d.Seat).WithMany(p => p.Tickets)
                .HasForeignKey(d => d.SeatId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tickets_Seats");

            entity.HasOne(d => d.Showtime).WithMany(p => p.Tickets)
                .HasForeignKey(d => d.ShowtimeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tickets_Showtimes");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email, "UK_Users_email").IsUnique();

            entity.HasIndex(e => e.Phone, "UX_Users_phone")
                .IsUnique()
                .HasFilter("[phone] IS NOT NULL");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("avatar_url");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .HasColumnName("full_name");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("password_hash");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("phone");
            entity.Property(e => e.RewardPoints)
                .HasDefaultValue(0)
                .HasColumnName("reward_points");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Active")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Roles");
        });

        modelBuilder.Entity<Vat>(entity =>
        {
            entity.ToTable("VAT");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Active")
                .HasColumnName("status");
            entity.Property(e => e.VatRate)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("vat_rate");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
